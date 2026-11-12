using System.Net;
using System.Net.Http.Json;
using Arquitectos.Application.Usuarios;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Arquitectos.Tests;

public sealed class SesionHost : IDisposable
{
    private readonly SqliteConnection _conexion = new("Data Source=sesion-segura;Mode=Memory;Cache=Shared");

    public SesionHost()
    {
        _conexion.Open();
        Environment.SetEnvironmentVariable("Jwt__Key", new string('k', 40));
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Arquitectos",
            "Server=127.0.0.1;Database=Arquitectos;User ID=ci;Password=ci;TrustServerCertificate=True");
        Fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuracion) =>
            {
                configuracion.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = new string('k', 40),
                    ["Jwt:Issuer"] = "Arquitectos",
                    ["Jwt:Audience"] = "Arquitectos",
                    ["ConnectionStrings:Arquitectos"] = "Server=127.0.0.1;Database=Arquitectos;Trusted_Connection=True;TrustServerCertificate=True"
                });
            });
            builder.ConfigureServices(servicios =>
            {
                servicios.RemoveAll(typeof(DbContextOptions<ArquitectosDbContext>));
                servicios.RemoveAll(typeof(DbContextOptions));
                servicios.RemoveAll(typeof(ArquitectosDbContext));
                servicios.RemoveAll(typeof(IDbContextOptionsConfiguration<ArquitectosDbContext>));
                servicios.AddDbContext<ArquitectosDbContext>(opciones => opciones.UseSqlite(_conexion));
            });
        });
        using var alcance = Fabrica.Services.CreateScope();
        alcance.ServiceProvider.GetRequiredService<ArquitectosDbContext>().Database.EnsureCreated();
        Cliente = Fabrica.CreateClient();
    }

    public WebApplicationFactory<Program> Fabrica { get; }

    public HttpClient Cliente { get; }

    public void Dispose()
    {
        Cliente.Dispose();
        Fabrica.Dispose();
        _conexion.Dispose();
    }
}

public sealed class SesionSeguraTests : IClassFixture<SesionHost>
{
    private const string Clave = "Clave12345";

    private readonly SesionHost _host;

    public SesionSeguraTests(SesionHost host)
    {
        _host = host;
    }

    [Fact]
    public async Task Renovar_rota_el_token_y_reutilizarlo_revoca_todas_las_sesiones()
    {
        await CrearUsuario("ana");
        var ingreso = await Login("ana");
        var primero = Cookie(ingreso, "arquitectos.renovacion");
        Assert.DoesNotContain("tokenAcceso", await ingreso.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        var renovado = await Enviar(HttpMethod.Post, "/api/auth/renovar", null, ("arquitectos.renovacion", primero));
        var segundo = Cookie(renovado, "arquitectos.renovacion");
        var reutilizado = await Enviar(HttpMethod.Post, "/api/auth/renovar", null, ("arquitectos.renovacion", primero));
        var despues = await Enviar(HttpMethod.Post, "/api/auth/renovar", null, ("arquitectos.renovacion", segundo));

        Assert.Equal(HttpStatusCode.NoContent, renovado.StatusCode);
        Assert.NotEqual(primero, segundo);
        Assert.Equal(HttpStatusCode.Unauthorized, reutilizado.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, despues.StatusCode);
    }

    [Fact]
    public async Task Un_usuario_desactivado_recibe_401_en_la_siguiente_peticion()
    {
        var id = await CrearUsuario("bruno");
        var ingreso = await Login("bruno");
        var acceso = Cookie(ingreso, "arquitectos.acceso");
        var menuAntes = await Enviar(HttpMethod.Get, "/api/menu", null, ("arquitectos.acceso", acceso));
        using (var alcance = _host.Fabrica.Services.CreateScope())
        {
            var usuarios = alcance.ServiceProvider.GetRequiredService<IUsuarioServicio>();
            var eliminado = await usuarios.EliminarAsync(id, SemillaInicial.UsuarioAdminId, CancellationToken.None);
            Assert.Equal(EstadoUsuario.Ok, eliminado.Estado);
        }

        var menu = await Enviar(HttpMethod.Get, "/api/menu", null, ("arquitectos.acceso", acceso));

        Assert.Equal(HttpStatusCode.OK, menuAntes.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, menu.StatusCode);
    }

    [Fact]
    public async Task Salir_revoca_la_sesion()
    {
        await CrearUsuario("carla");
        var ingreso = await Login("carla");
        var acceso = Cookie(ingreso, "arquitectos.acceso");
        var renovacion = Cookie(ingreso, "arquitectos.renovacion");

        var salida = await Enviar(HttpMethod.Post, "/api/auth/salir", null, ("arquitectos.acceso", acceso));
        var menu = await Enviar(HttpMethod.Get, "/api/menu", null, ("arquitectos.acceso", acceso));
        var renovar = await Enviar(HttpMethod.Post, "/api/auth/renovar", null, ("arquitectos.renovacion", renovacion));

        Assert.Equal(HttpStatusCode.NoContent, salida.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, menu.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, renovar.StatusCode);
    }

    private async Task<int> CrearUsuario(string usuario)
    {
        using var alcance = _host.Fabrica.Services.CreateScope();
        var servicio = alcance.ServiceProvider.GetRequiredService<IUsuarioServicio>();
        var creado = await servicio.CrearAsync(new CrearUsuarioSolicitud
        {
            Usuario = usuario,
            Nombre = usuario,
            Password = Clave,
            TipoUsuarioId = SemillaInicial.TipoAdministradorId
        }, CancellationToken.None);
        return creado.Valor!.Id;
    }

    private async Task<HttpResponseMessage> Login(string usuario)
    {
        var respuesta = await Enviar(HttpMethod.Post, "/api/auth/login", new { usuario, password = Clave });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        return respuesta;
    }

    private async Task<HttpResponseMessage> Enviar(
        HttpMethod metodo,
        string ruta,
        object? cuerpo,
        params (string Nombre, string Valor)[] cookies)
    {
        var peticion = new HttpRequestMessage(metodo, ruta);
        if (cuerpo is not null)
        {
            peticion.Content = JsonContent.Create(cuerpo);
        }

        if (metodo != HttpMethod.Get)
        {
            peticion.Headers.TryAddWithoutValidation("X-Requested-With", "arquitectos");
        }

        if (cookies.Length > 0)
        {
            peticion.Headers.TryAddWithoutValidation(
                "Cookie",
                string.Join("; ", cookies.Select(cookie => $"{cookie.Nombre}={cookie.Valor}")));
        }

        return await _host.Cliente.SendAsync(peticion);
    }

    private static string Cookie(HttpResponseMessage respuesta, string nombre)
    {
        var encabezado = respuesta.Headers.GetValues("Set-Cookie")
            .Single(valor => valor.StartsWith(nombre + "=", StringComparison.Ordinal));
        return encabezado.Split(';', 2)[0][(nombre.Length + 1)..];
    }
}
