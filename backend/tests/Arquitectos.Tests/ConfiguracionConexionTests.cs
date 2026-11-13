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
            ARQUITECTOS_DB_HOST=archivo.local
            ARQUITECTOS_DB_NAME=DESDE_ARCHIVO
            ARQUITECTOS_DB_USER=archivo
            ARQUITECTOS_DB_PASSWORD=archivo-secreto
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
    public void En_desarrollo_la_clave_Sql_ConfiarCertificado_pisa_el_predeterminado()
    {
        File.WriteAllText(Path.Combine(_api, "appsettings.Development.json"), """
            {
              "Sql": { "ConfiarCertificado": false }
            }
            """);
        Environment.SetEnvironmentVariable("Jwt__Key", new string('k', 40));
        Environment.SetEnvironmentVariable("ConnectionStrings__Arquitectos", null);

        using var fabrica = Fabrica(Environments.Development);
        using var cliente = fabrica.CreateClient();
        var cadena = fabrica.Services.GetRequiredService<IConfiguration>().GetConnectionString("Arquitectos");

        Assert.Equal("DESDE_ARCHIVO", Catalogo(cadena));
        Assert.False(new SqlConnectionStringBuilder(cadena!).TrustServerCertificate);
    }

    [Fact]
    public void La_busqueda_no_pasa_de_la_raiz_del_repositorio()
    {
        File.WriteAllText(Path.Combine(_base, ".env"), """
            ARQUITECTOS_DB_HOST=fuera.local
            ARQUITECTOS_DB_NAME=FUERA_DEL_REPO
            ARQUITECTOS_DB_USER=fuera
            ARQUITECTOS_DB_PASSWORD=fuera-secreto
            """);
        File.Delete(Path.Combine(_raiz, ".env"));

        Assert.Null(ArchivoEnv.Buscar(_api));
        Assert.Empty(ArchivoEnv.CrearValores(_api));
    }

    [Fact]
    public void Las_variables_nuevas_no_avisan_y_la_confianza_del_certificado_es_configurable()
    {
        var avisos = new List<string>();
        var conConfianza = ArchivoEnv.CrearValores(_api, confiarCertificado: true, avisos.Add);
        Assert.Empty(avisos);
        Assert.True(new SqlConnectionStringBuilder(conConfianza[ArchivoEnv.ClaveConexion]!).TrustServerCertificate);
        Assert.Equal("DESDE_ARCHIVO", Catalogo(conConfianza[ArchivoEnv.ClaveConexion]));

        var sinConfianza = ArchivoEnv.CrearValores(_api, confiarCertificado: false, avisos.Add);
        Assert.False(new SqlConnectionStringBuilder(sinConfianza[ArchivoEnv.ClaveConexion]!).TrustServerCertificate);
        Assert.Empty(avisos);
    }

    [Fact]
    public void Las_variables_antiguas_arman_la_cadena_y_avisan_sin_mostrar_la_clave()
    {
        File.WriteAllText(Path.Combine(_raiz, ".env"), """
            INTEGRACION_VENTAS_HOST=archivo.local
            INTEGRACION_VENTAS_DATABASE=DESDE_ARCHIVO
            INTEGRACION_VENTAS_USER=archivo
            INTEGRACION_VENTAS_PASSWORD=archivo-secreto
            """);
        var avisos = new List<string>();
        var valores = ArchivoEnv.CrearValores(_api, confiarCertificado: false, avisos.Add);

        Assert.Equal("DESDE_ARCHIVO", Catalogo(valores[ArchivoEnv.ClaveConexion]));
        Assert.False(new SqlConnectionStringBuilder(valores[ArchivoEnv.ClaveConexion]!).TrustServerCertificate);
        var aviso = Assert.Single(avisos);
        Assert.Contains("ARQUITECTOS_DB_HOST", aviso, StringComparison.Ordinal);
        Assert.DoesNotContain("archivo-secreto", aviso, StringComparison.Ordinal);
    }

    [Fact]
    public void Si_hay_variables_nuevas_no_se_leen_las_antiguas()
    {
        File.WriteAllText(Path.Combine(_raiz, ".env"), """
            ARQUITECTOS_DB_HOST=archivo.local
            ARQUITECTOS_DB_NAME=DESDE_ARCHIVO
            ARQUITECTOS_DB_USER=archivo
            ARQUITECTOS_DB_PASSWORD=archivo-secreto
            INTEGRACION_VENTAS_HOST=viejo.local
            INTEGRACION_VENTAS_DATABASE=DESDE_VIEJO
            INTEGRACION_VENTAS_USER=viejo
            INTEGRACION_VENTAS_PASSWORD=viejo-secreto
            """);
        var avisos = new List<string>();
        var valores = ArchivoEnv.CrearValores(_api, advertir: avisos.Add);

        Assert.Equal("DESDE_ARCHIVO", Catalogo(valores[ArchivoEnv.ClaveConexion]));
        Assert.Empty(avisos);
    }

    [Fact]
    public void Fuera_de_development_la_confianza_del_certificado_no_es_el_predeterminado()
    {
        var vacia = new ConfigurationBuilder().Build();
        Assert.True(ArchivoEnv.ResolverConfianza(vacia, esDesarrollo: true));
        Assert.False(ArchivoEnv.ResolverConfianza(vacia, esDesarrollo: false));

        var explicita = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [ArchivoEnv.ClaveConfianza] = "false" })
            .Build();
        Assert.False(ArchivoEnv.ResolverConfianza(explicita, esDesarrollo: true));
        Assert.False(ArchivoEnv.EsDesarrollo("Production"));
        Assert.True(ArchivoEnv.EsDesarrollo(null));
        var ajustada = ArchivoEnv.CadenaConConfianza(Cadena("CATALOGO"), confiar: false);
        Assert.False(new SqlConnectionStringBuilder(ajustada).TrustServerCertificate);
        Assert.Equal("CATALOGO", Catalogo(ajustada));
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
