using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Arquitectos.Application.Usuarios;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Arquitectos.Tests;

[Collection("AnfitrionApi")]
public sealed class AuthMovilTests : IAsyncLifetime
{
    private const string Clave = "Clave12345";

    private readonly SqliteConnection _conexion = new("Data Source=auth-movil;Mode=Memory;Cache=Shared");
    private WebApplicationFactory<Program> _fabrica = null!;
    private HttpClient _cliente = null!;

    public async Task InitializeAsync()
    {
        await _conexion.OpenAsync();
        Environment.SetEnvironmentVariable("Jwt__Key", new string('k', 40));
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Arquitectos",
            "Server=127.0.0.1;Database=Arquitectos;User ID=ci;Password=ci;TrustServerCertificate=True");
        _fabrica = Fabrica(_conexion, limiteLogin: 100);
        using var alcance = _fabrica.Services.CreateScope();
        await alcance.ServiceProvider.GetRequiredService<ArquitectosDbContext>().Database.EnsureCreatedAsync();
        _cliente = _fabrica.CreateClient();
    }

    [Fact]
    public async Task El_administrador_recibe_403_y_el_inactivo_401()
    {
        await CrearUsuarioAsync("adminmovil", SemillaInicial.TipoAdministradorId, null);
        var administrador = await LoginMovil("adminmovil", Guid.NewGuid());
        var arquitectoId = await CrearOperativoAsync("arquitecto.movil", SemillaInicial.TipoArquitectoId);
        using (var alcance = _fabrica.Services.CreateScope())
        {
            var contexto = alcance.ServiceProvider.GetRequiredService<ArquitectosDbContext>();
            var usuario = await contexto.Usuarios.SingleAsync(item => item.Id == arquitectoId);
            usuario.Activo = false;
            await contexto.SaveChangesAsync();
        }

        var inactivo = await LoginMovil("arquitecto.movil", Guid.NewGuid());

        Assert.Equal(HttpStatusCode.Forbidden, administrador.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, inactivo.StatusCode);
    }

    [Fact]
    public async Task Renovar_rota_el_token_y_el_anterior_deja_de_servir()
    {
        await CrearOperativoAsync("rota.movil", SemillaInicial.TipoArquitectoId);
        var dispositivo = Guid.NewGuid();
        var ingreso = await LoginMovil("rota.movil", dispositivo);
        var primero = await JsonDocument.ParseAsync(await ingreso.Content.ReadAsStreamAsync());
        var tokenInicial = primero.RootElement.GetProperty("refreshToken").GetString();

        var renovado = await EnviarMovil(HttpMethod.Post, "/api/movil/v1/auth/renovar", new { refreshToken = tokenInicial, dispositivoId = dispositivo });
        var segundo = await JsonDocument.ParseAsync(await renovado.Content.ReadAsStreamAsync());
        var tokenNuevo = segundo.RootElement.GetProperty("refreshToken").GetString();
        var reutilizado = await EnviarMovil(HttpMethod.Post, "/api/movil/v1/auth/renovar", new { refreshToken = tokenInicial, dispositivoId = dispositivo });
        var conElNuevo = await EnviarMovil(HttpMethod.Post, "/api/movil/v1/auth/renovar", new { refreshToken = tokenNuevo, dispositivoId = dispositivo });

        Assert.Equal(HttpStatusCode.OK, ingreso.StatusCode);
        Assert.Equal(HttpStatusCode.OK, renovado.StatusCode);
        Assert.NotEqual(tokenInicial, tokenNuevo);
        Assert.False(string.IsNullOrWhiteSpace(segundo.RootElement.GetProperty("accessToken").GetString()));
        Assert.Equal(HttpStatusCode.Unauthorized, reutilizado.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, conElNuevo.StatusCode);
    }

    [Fact]
    public async Task Un_dispositivo_revocado_responde_401_con_codigo_y_queda_auditado()
    {
        var usuarioId = await CrearOperativoAsync("revocado.movil", SemillaInicial.TipoContratistaId);
        var dispositivo = Guid.NewGuid();
        var ingreso = await LoginMovil("revocado.movil", dispositivo);
        Assert.Equal(HttpStatusCode.OK, ingreso.StatusCode);
        var sesion = await JsonDocument.ParseAsync(await ingreso.Content.ReadAsStreamAsync());
        var token = sesion.RootElement.GetProperty("refreshToken").GetString();
        await CrearUsuarioAsync("admin.revoca", SemillaInicial.TipoAdministradorId, null);
        var web = await EnviarWeb(HttpMethod.Post, "/api/auth/login", new { usuario = "admin.revoca", password = Clave });
        var acceso = Cookie(web, "arquitectos.acceso");

        var revocado = await EnviarWeb(HttpMethod.Post, $"/api/usuarios/{usuarioId}/dispositivos/{dispositivo}/revocar", null, ("arquitectos.acceso", acceso));
        var renovar = await EnviarMovil(HttpMethod.Post, "/api/movil/v1/auth/renovar", new { refreshToken = token, dispositivoId = dispositivo });
        var cuerpo = await JsonDocument.ParseAsync(await renovar.Content.ReadAsStreamAsync());
        using var alcance = _fabrica.Services.CreateScope();
        var auditoria = await alcance.ServiceProvider.GetRequiredService<ArquitectosDbContext>().Auditorias
            .SingleAsync(item => item.Entidad == "Dispositivo" && item.EntidadId == dispositivo.ToString());

        Assert.Equal(HttpStatusCode.NoContent, revocado.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, renovar.StatusCode);
        Assert.Equal("dispositivo_revocado", cuerpo.RootElement.GetProperty("codigo").GetString());
        Assert.Equal(AccionAuditoria.Modificar, auditoria.Accion);
        Assert.Equal("web", auditoria.Origen);
    }

    [Fact]
    public async Task Una_version_vieja_responde_426()
    {
        await CrearOperativoAsync("version.movil", SemillaInicial.TipoRevisorId);
        var vieja = await LoginMovil("version.movil", Guid.NewGuid(), "0.9.0");
        var actual = await LoginMovil("version.movil", Guid.NewGuid(), "1.0.0");

        Assert.Equal(HttpStatusCode.UpgradeRequired, vieja.StatusCode);
        Assert.Contains("Actualice la aplicación.", await vieja.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, actual.StatusCode);
    }

    public async Task DisposeAsync()
    {
        _cliente.Dispose();
        await _fabrica.DisposeAsync();
        await _conexion.DisposeAsync();
    }

    private async Task<int> CrearOperativoAsync(string usuario, int tipo)
    {
        using var alcance = _fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ArquitectosDbContext>();
        int perfilId;
        if (tipo == SemillaInicial.TipoArquitectoId)
        {
            var persona = new Arquitecto { Nombre = usuario };
            contexto.Arquitectos.Add(persona);
            await contexto.SaveChangesAsync();
            perfilId = persona.Id;
        }
        else if (tipo == SemillaInicial.TipoContratistaId)
        {
            var persona = new Contratista { Nombre = usuario };
            contexto.Contratistas.Add(persona);
            await contexto.SaveChangesAsync();
            perfilId = persona.Id;
        }
        else
        {
            var persona = new Revisor { Nombre = usuario };
            contexto.Revisores.Add(persona);
            await contexto.SaveChangesAsync();
            perfilId = persona.Id;
        }

        return await CrearUsuarioAsync(usuario, tipo, perfilId);
    }

    private async Task<int> CrearUsuarioAsync(string usuario, int tipo, int? perfilId)
    {
        using var alcance = _fabrica.Services.CreateScope();
        var servicio = alcance.ServiceProvider.GetRequiredService<IUsuarioServicio>();
        var creado = await servicio.CrearAsync(new CrearUsuarioSolicitud
        {
            Usuario = usuario,
            Nombre = usuario,
            Password = Clave,
            TipoUsuarioId = tipo,
            PerfilId = perfilId
        }, CancellationToken.None);
        Assert.Equal(EstadoUsuario.Ok, creado.Estado);
        return creado.Valor!.Id;
    }

    private Task<HttpResponseMessage> LoginMovil(string usuario, Guid dispositivo, string version = "1.0.0") =>
        EnviarMovil(HttpMethod.Post, "/api/movil/v1/auth/login", new
        {
            usuario,
            contrasena = Clave,
            dispositivo = new { id = dispositivo, nombre = "Teléfono de obra", plataforma = "android", versionApp = version }
        }, version);

    private Task<HttpResponseMessage> EnviarMovil(HttpMethod metodo, string ruta, object? cuerpo, string version = "1.0.0")
    {
        var peticion = new HttpRequestMessage(metodo, ruta);
        if (cuerpo is not null)
        {
            peticion.Content = JsonContent.Create(cuerpo);
        }

        peticion.Headers.TryAddWithoutValidation("X-Requested-With", "arquitectos");
        peticion.Headers.TryAddWithoutValidation("X-Version-App", version);
        return _cliente.SendAsync(peticion);
    }

    private Task<HttpResponseMessage> EnviarWeb(HttpMethod metodo, string ruta, object? cuerpo, params (string Nombre, string Valor)[] cookies)
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
            peticion.Headers.TryAddWithoutValidation("Cookie", string.Join("; ", cookies.Select(cookie => $"{cookie.Nombre}={cookie.Valor}")));
        }

        return _cliente.SendAsync(peticion);
    }

    private static string Cookie(HttpResponseMessage respuesta, string nombre)
    {
        var encabezado = respuesta.Headers.GetValues("Set-Cookie")
            .Single(valor => valor.StartsWith(nombre + "=", StringComparison.Ordinal));
        return encabezado.Split(';', 2)[0][(nombre.Length + 1)..];
    }

    internal static WebApplicationFactory<Program> Fabrica(SqliteConnection conexion, int? limiteLogin)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuracion) =>
            {
                var valores = new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = new string('k', 40),
                    ["Jwt:Issuer"] = "Arquitectos",
                    ["Jwt:Audience"] = "Arquitectos",
                    ["ConnectionStrings:Arquitectos"] = "Server=127.0.0.1;Database=Arquitectos;Trusted_Connection=True;TrustServerCertificate=True",
                    ["Movil:VersionMinimaApp"] = "1.0.0"
                };
                if (limiteLogin is int limite)
                {
                    valores["Login:Limite"] = limite.ToString();
                }

                configuracion.AddInMemoryCollection(valores);
            });
            builder.ConfigureServices(servicios =>
            {
                servicios.RemoveAll(typeof(DbContextOptions<ArquitectosDbContext>));
                servicios.RemoveAll(typeof(DbContextOptions));
                servicios.RemoveAll(typeof(ArquitectosDbContext));
                servicios.RemoveAll(typeof(IDbContextOptionsConfiguration<ArquitectosDbContext>));
                servicios.AddDbContext<ArquitectosDbContext>(opciones => opciones.UseSqlite(conexion));
            });
        });
    }
}

[Collection("AnfitrionApi")]
public sealed class AuthMovilLimiteTests : IAsyncLifetime
{
    private readonly SqliteConnection _conexion = new("Data Source=auth-movil-limite;Mode=Memory;Cache=Shared");
    private WebApplicationFactory<Program> _fabrica = null!;

    public async Task InitializeAsync()
    {
        await _conexion.OpenAsync();
        Environment.SetEnvironmentVariable("Jwt__Key", new string('k', 40));
        _fabrica = AuthMovilTests.Fabrica(_conexion, limiteLogin: null);
        using var alcance = _fabrica.Services.CreateScope();
        await alcance.ServiceProvider.GetRequiredService<ArquitectosDbContext>().Database.EnsureCreatedAsync();
    }

    [Fact]
    public async Task La_sexta_peticion_de_login_movil_en_un_minuto_responde_429()
    {
        using var cliente = _fabrica.CreateClient();
        HttpResponseMessage? ultima = null;
        for (var intento = 0; intento < 6; intento++)
        {
            var peticion = new HttpRequestMessage(HttpMethod.Post, "/api/movil/v1/auth/login")
            {
                Content = JsonContent.Create(new { usuario = "nadie", contrasena = "Clave12345" })
            };
            peticion.Headers.TryAddWithoutValidation("X-Requested-With", "arquitectos");
            ultima = await cliente.SendAsync(peticion);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, ultima!.StatusCode);
    }

    public async Task DisposeAsync()
    {
        await _fabrica.DisposeAsync();
        await _conexion.DisposeAsync();
    }
}
