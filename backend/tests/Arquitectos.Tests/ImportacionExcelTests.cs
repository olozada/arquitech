using System.IO.Compression;
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

[Collection("AnfitrionApi")]
public sealed class ImportacionExcelTests : IAsyncLifetime
{
    private const string Clave = "Clave12345";

    private readonly SqliteConnection _conexion = new("Data Source=importacion-excel;Mode=Memory;Cache=Shared");
    private WebApplicationFactory<Program> _fabrica = null!;
    private HttpClient _cliente = null!;
    private string _acceso = "";

    public async Task InitializeAsync()
    {
        await _conexion.OpenAsync();
        Environment.SetEnvironmentVariable("Jwt__Key", new string('k', 40));
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Arquitectos",
            "Server=127.0.0.1;Database=Arquitectos;User ID=ci;Password=ci;TrustServerCertificate=True");
        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
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

        using var alcance = _fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ArquitectosDbContext>();
        await contexto.Database.EnsureCreatedAsync();
        var usuarios = alcance.ServiceProvider.GetRequiredService<IUsuarioServicio>();
        var creado = await usuarios.CrearAsync(new CrearUsuarioSolicitud
        {
            Usuario = "importador",
            Nombre = "Importador",
            Password = Clave,
            TipoUsuarioId = SemillaInicial.TipoAdministradorId
        }, CancellationToken.None);
        Assert.NotNull(creado.Valor);

        _cliente = _fabrica.CreateClient();
        using var ingreso = await Enviar(_cliente, HttpMethod.Post, "/api/auth/login", JsonContent.Create(new { usuario = "importador", password = Clave }));
        Assert.Equal(HttpStatusCode.OK, ingreso.StatusCode);
        _acceso = Cookie(ingreso, "arquitectos.acceso");
    }

    [Fact]
    public async Task Un_texto_renombrado_a_xlsx_responde_400()
    {
        var respuesta = await Importar("notas.xlsx", "esto no es un libro"u8.ToArray());

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("Excel", await respuesta.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Un_zip_con_entrada_de_200_mb_responde_400()
    {
        var bytes = LibroInflado(200L * 1024 * 1024);
        using (var zip = new ZipArchive(new MemoryStream(bytes), ZipArchiveMode.Read, leaveOpen: false))
        {
            Assert.True(zip.Entries.Single().Length > 100L * 1024 * 1024);
        }

        var respuesta = await Importar("trampa.xlsx", bytes);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.True(respuesta.StatusCode == HttpStatusCode.BadRequest, cuerpo);
        Assert.Contains("descomprimido", cuerpo, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Un_archivo_de_11_mb_responde_413()
    {
        var respuesta = await Importar("grande.xlsx", new byte[11_000_000]);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.True(respuesta.StatusCode == HttpStatusCode.RequestEntityTooLarge, cuerpo);
        Assert.Contains("superar", cuerpo, StringComparison.OrdinalIgnoreCase);
    }

    public async Task DisposeAsync()
    {
        _cliente.Dispose();
        await _fabrica.DisposeAsync();
        await _conexion.DisposeAsync();
    }

    private async Task<HttpResponseMessage> Importar(string nombre, byte[] bytes)
    {
        using var formulario = new MultipartFormDataContent();
        formulario.Add(new ByteArrayContent(bytes), "archivo", nombre);
        formulario.Add(new StringContent("1"), "construccionId");
        return await Enviar(_cliente, HttpMethod.Post, "/api/presupuestos/importar", formulario, ("arquitectos.acceso", _acceso));
    }

    private static async Task<HttpResponseMessage> Enviar(
        HttpClient cliente,
        HttpMethod metodo,
        string ruta,
        HttpContent contenido,
        params (string Nombre, string Valor)[] cookies)
    {
        var peticion = new HttpRequestMessage(metodo, ruta) { Content = contenido };
        peticion.Headers.TryAddWithoutValidation("X-Requested-With", "arquitectos");
        if (cookies.Length > 0)
        {
            peticion.Headers.TryAddWithoutValidation(
                "Cookie",
                string.Join("; ", cookies.Select(cookie => $"{cookie.Nombre}={cookie.Valor}")));
        }

        return await cliente.SendAsync(peticion);
    }

    private static string Cookie(HttpResponseMessage respuesta, string nombre)
    {
        var encabezado = respuesta.Headers.GetValues("Set-Cookie")
            .Single(valor => valor.StartsWith(nombre + "=", StringComparison.Ordinal));
        return encabezado.Split(';', 2)[0][(nombre.Length + 1)..];
    }

    private static byte[] LibroInflado(long descomprimido)
    {
        using var memoria = new MemoryStream();
        using (var zip = new ZipArchive(memoria, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entrada = zip.CreateEntry("xl/workbook.xml", CompressionLevel.Optimal);
            using var escritor = entrada.Open();
            escritor.Write("{}"u8);
        }

        var bytes = memoria.ToArray();
        var tamano = BitConverter.GetBytes((uint)descomprimido);
        Assert.Equal((byte)0x50, bytes[0]);
        Buffer.BlockCopy(tamano, 0, bytes, 22, 4);
        var central = Buscar(bytes, [0x50, 0x4B, 0x01, 0x02]);
        Assert.True(central > 0);
        Buffer.BlockCopy(tamano, 0, bytes, central + 24, 4);
        return bytes;
    }

    private static int Buscar(byte[] datos, byte[] marca)
    {
        for (var indice = 0; indice <= datos.Length - marca.Length; indice++)
        {
            var coincide = true;
            for (var byteIndice = 0; byteIndice < marca.Length; byteIndice++)
            {
                if (datos[indice + byteIndice] != marca[byteIndice])
                {
                    coincide = false;
                    break;
                }
            }

            if (coincide)
            {
                return indice;
            }
        }

        return -1;
    }
}
