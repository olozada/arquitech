using Arquitectos.Infrastructure.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Arquitectos.Tests;

[CollectionDefinition("AnfitrionApi", DisableParallelization = true)]
public sealed class AnfitrionApiColeccion;

[Collection("AnfitrionApi")]
public sealed class ConfiguracionConexionTests : IDisposable
{
    private readonly string _base;
    private readonly string _raiz;
    private readonly string _api;
    private readonly string? _cadenaAnterior;
    private readonly string? _jwtAnterior;

    public ConfiguracionConexionTests()
    {
        _base = Path.Combine(Path.GetTempPath(), "arquitectos-conexion-" + Guid.NewGuid().ToString("N"));
        _raiz = Path.Combine(_base, "repo");
        _api = Path.Combine(_raiz, "backend", "src", "Arquitectos.Api");
        Directory.CreateDirectory(_api);
        File.WriteAllText(Path.Combine(_raiz, ".git"), "gitdir: test");
        File.WriteAllText(Path.Combine(_raiz, "backend", "Arquitectos.sln"), "");
        File.WriteAllText(Path.Combine(_raiz, ".env"), """
            INTEGRACION_VENTAS_HOST=archivo.local
            INTEGRACION_VENTAS_DATABASE=DESDE_ARCHIVO
            INTEGRACION_VENTAS_USER=archivo
            INTEGRACION_VENTAS_PASSWORD=archivo-secreto
            """);
        _cadenaAnterior = Environment.GetEnvironmentVariable("ConnectionStrings__Arquitectos");
        _jwtAnterior = Environment.GetEnvironmentVariable("Jwt__Key");
    }

    [Fact]
    public void En_produccion_la_variable_gana_al_archivo_env()
    {
        Environment.SetEnvironmentVariable("Jwt__Key", new string('k', 40));
        Environment.SetEnvironmentVariable("ConnectionStrings__Arquitectos", Cadena("DESDE_VARIABLE"));

        using var fabrica = Fabrica(Environments.Production);
        using var cliente = fabrica.CreateClient();
        var cadena = fabrica.Services.GetRequiredService<IConfiguration>().GetConnectionString("Arquitectos");

        Assert.Equal("DESDE_VARIABLE", Catalogo(cadena));
    }

    [Fact]
    public void En_produccion_sin_variable_no_arranca_y_no_usa_el_archivo_env()
    {
        Environment.SetEnvironmentVariable("Jwt__Key", new string('k', 40));
        Environment.SetEnvironmentVariable("ConnectionStrings__Arquitectos", null);

        var excepcion = Assert.ThrowsAny<Exception>(() =>
        {
            using var fabrica = Fabrica(Environments.Production);
            using var cliente = fabrica.CreateClient();
        });

        var mensaje = Texto(excepcion);
        Assert.Contains("ConnectionStrings:Arquitectos", mensaje, StringComparison.Ordinal);
        Assert.Contains("ConnectionStrings__Arquitectos", mensaje, StringComparison.Ordinal);
        Assert.DoesNotContain("DESDE_ARCHIVO", mensaje, StringComparison.Ordinal);
        Assert.DoesNotContain(".env", mensaje, StringComparison.Ordinal);
    }

    [Fact]
    public void En_desarrollo_el_archivo_env_se_usa_y_la_variable_lo_gana()
    {
        Environment.SetEnvironmentVariable("Jwt__Key", new string('k', 40));
        Environment.SetEnvironmentVariable("ConnectionStrings__Arquitectos", null);
        using (var fabrica = Fabrica(Environments.Development))
        {
            using var cliente = fabrica.CreateClient();
            Assert.Equal(
                "DESDE_ARCHIVO",
                Catalogo(fabrica.Services.GetRequiredService<IConfiguration>().GetConnectionString("Arquitectos")));
        }

        Environment.SetEnvironmentVariable("ConnectionStrings__Arquitectos", Cadena("DESDE_VARIABLE"));
        using var conVariable = Fabrica(Environments.Development);
        using var clienteVariable = conVariable.CreateClient();
        Assert.Equal(
            "DESDE_VARIABLE",
            Catalogo(conVariable.Services.GetRequiredService<IConfiguration>().GetConnectionString("Arquitectos")));
    }

    [Fact]
    public void La_busqueda_no_pasa_de_la_raiz_del_repositorio()
    {
        File.WriteAllText(Path.Combine(_base, ".env"), """
            INTEGRACION_VENTAS_HOST=fuera.local
            INTEGRACION_VENTAS_DATABASE=FUERA_DEL_REPO
            INTEGRACION_VENTAS_USER=fuera
            INTEGRACION_VENTAS_PASSWORD=fuera-secreto
            """);
        File.Delete(Path.Combine(_raiz, ".env"));

        Assert.Null(ArchivoEnv.Buscar(_api));
        Assert.Empty(ArchivoEnv.CrearValores(_api));
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Arquitectos", _cadenaAnterior);
        Environment.SetEnvironmentVariable("Jwt__Key", _jwtAnterior);
        if (Directory.Exists(_base))
        {
            Directory.Delete(_base, recursive: true);
        }
    }

    private WebApplicationFactory<Program> Fabrica(string entorno) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting(WebHostDefaults.EnvironmentKey, entorno);
            builder.UseContentRoot(_api);
        });

    private static string Cadena(string catalogo) =>
        new SqlConnectionStringBuilder
        {
            DataSource = "variable.local",
            InitialCatalog = catalogo,
            UserID = "variable",
            Password = "variable-secreto",
            TrustServerCertificate = true
        }.ConnectionString;

    private static string? Catalogo(string? cadena) =>
        string.IsNullOrWhiteSpace(cadena) ? null : new SqlConnectionStringBuilder(cadena).InitialCatalog;

    private static string Texto(Exception excepcion)
    {
        var partes = new List<string>();
        for (Exception? actual = excepcion; actual is not null; actual = actual.InnerException)
        {
            partes.Add(actual.Message);
        }

        return string.Join(" ", partes);
    }
}
