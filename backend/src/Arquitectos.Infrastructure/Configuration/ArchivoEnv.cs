using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.Hosting;

namespace Arquitectos.Infrastructure.Configuration;

public static class ArchivoEnv
{
    public const string ClaveConexion = "ConnectionStrings:Arquitectos";
    public const string ClaveConfianza = "Sql:ConfiarCertificado";

    private const string AvisoVariablesAntiguas =
        "El archivo .env todavía usa INTEGRACION_VENTAS_HOST, INTEGRACION_VENTAS_DATABASE, INTEGRACION_VENTAS_USER o INTEGRACION_VENTAS_PASSWORD. " +
        "Renómbralas a ARQUITECTOS_DB_HOST, ARQUITECTOS_DB_NAME, ARQUITECTOS_DB_USER y ARQUITECTOS_DB_PASSWORD. Esta lectura se quitará en la próxima versión.";

    private static readonly (string Nueva, string Antigua)[] Claves =
    [
        ("ARQUITECTOS_DB_HOST", "INTEGRACION_VENTAS_HOST"),
        ("ARQUITECTOS_DB_NAME", "INTEGRACION_VENTAS_DATABASE"),
        ("ARQUITECTOS_DB_USER", "INTEGRACION_VENTAS_USER"),
        ("ARQUITECTOS_DB_PASSWORD", "INTEGRACION_VENTAS_PASSWORD")
    ];

    public static bool EsDesarrollo(string? entorno) =>
        string.IsNullOrEmpty(entorno)
        || string.Equals(entorno, Environments.Development, StringComparison.OrdinalIgnoreCase);

    public static bool ResolverConfianza(IConfiguration configuration, bool esDesarrollo)
    {
        if (bool.TryParse(configuration[ClaveConfianza], out var valor))
        {
            return valor;
        }

        return esDesarrollo;
    }

    public static string CadenaConConfianza(string cadena, bool confiar) =>
        new SqlConnectionStringBuilder(cadena) { TrustServerCertificate = confiar }.ConnectionString;

    public static void FijarConfianza(ConfigurationManager configuration)
    {
        var cadena = configuration.GetConnectionString("Arquitectos");
        if (string.IsNullOrWhiteSpace(cadena) || !bool.TryParse(configuration[ClaveConfianza], out var confiar))
        {
            return;
        }

        configuration[ClaveConexion] = CadenaConConfianza(cadena, confiar);
    }

    public static void AgregarDesarrollo(
        ConfigurationManager configuration,
        IHostEnvironment environment,
        string directorioInicial,
        Action<string>? advertir = null)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        var valores = CrearValores(directorioInicial, ResolverConfianza(configuration, esDesarrollo: true), advertir);
        if (valores.Count == 0)
        {
            return;
        }

        configuration.Sources.Insert(IndiceInsercion(configuration.Sources), new MemoryConfigurationSource { InitialData = valores });
    }

    public static Dictionary<string, string?> CrearValores(
        string directorioInicial,
        bool confiarCertificado = true,
        Action<string>? advertir = null)
    {
        var ruta = Buscar(directorioInicial);
        if (ruta is null)
        {
            return [];
        }

        var variables = Leer(ruta);
        var usoAntigua = false;
        var host = LeerVariable(variables, Claves[0].Nueva, Claves[0].Antigua, ref usoAntigua);
        var baseDatos = LeerVariable(variables, Claves[1].Nueva, Claves[1].Antigua, ref usoAntigua);
        var usuario = LeerVariable(variables, Claves[2].Nueva, Claves[2].Antigua, ref usoAntigua);
        var clave = LeerVariable(variables, Claves[3].Nueva, Claves[3].Antigua, ref usoAntigua);
        if (host is null || baseDatos is null || usuario is null || clave is null)
        {
            return [];
        }

        if (usoAntigua)
        {
            advertir?.Invoke(AvisoVariablesAntiguas);
        }

        var cadena = new SqlConnectionStringBuilder
        {
            DataSource = host,
            InitialCatalog = baseDatos,
            UserID = usuario,
            Password = clave,
            TrustServerCertificate = confiarCertificado,
            Encrypt = true
        };

        return new Dictionary<string, string?>
        {
            [ClaveConexion] = cadena.ConnectionString
        };
    }

    public static string? Buscar(string directorioInicial)
    {
        var directorio = new DirectoryInfo(directorioInicial);
        while (directorio is not null)
        {
            var candidato = Path.Combine(directorio.FullName, ".env");
            if (File.Exists(candidato))
            {
                return candidato;
            }

            if (EsRaizRepositorio(directorio))
            {
                return null;
            }

            directorio = directorio.Parent;
        }

        return null;
    }

    private static string? LeerVariable(
        Dictionary<string, string> variables,
        string nueva,
        string antigua,
        ref bool usoAntigua)
    {
        if (variables.TryGetValue(nueva, out var valor) && !string.IsNullOrWhiteSpace(valor))
        {
            return valor;
        }

        if (variables.TryGetValue(antigua, out var legado) && !string.IsNullOrWhiteSpace(legado))
        {
            usoAntigua = true;
            return legado;
        }

        return null;
    }

    private static bool EsRaizRepositorio(DirectoryInfo directorio)
    {
        var ruta = directorio.FullName;
        return File.Exists(Path.Combine(ruta, ".git"))
            || Directory.Exists(Path.Combine(ruta, ".git"))
            || (Directory.Exists(Path.Combine(ruta, "backend"))
                && File.Exists(Path.Combine(ruta, "backend", "Arquitectos.sln")));
    }

    private static int IndiceInsercion(IList<IConfigurationSource> fuentes)
    {
        for (var i = 0; i < fuentes.Count; i++)
        {
            if (fuentes[i] is JsonConfigurationSource json
                && json.Path?.EndsWith("secrets.json", StringComparison.OrdinalIgnoreCase) == true)
            {
                return i;
            }
        }

        for (var i = fuentes.Count - 1; i >= 0; i--)
        {
            if (fuentes[i] is EnvironmentVariablesConfigurationSource)
            {
                return i;
            }
        }

        return fuentes.Count;
    }

    private static Dictionary<string, string> Leer(string ruta)
    {
        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var linea in File.ReadAllLines(ruta))
        {
            var texto = linea.Trim();
            if (texto.Length == 0 || texto.StartsWith('#'))
            {
                continue;
            }

            var separador = texto.IndexOf('=');
            if (separador <= 0)
            {
                continue;
            }

            var clave = texto[..separador].Trim();
            var valor = texto[(separador + 1)..].Trim().Trim('"');
            variables[clave] = valor;
        }

        return variables;
    }
}
