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

    private const string Host = "ARQUITECTOS_DB_HOST";
    private const string BaseDatos = "ARQUITECTOS_DB_NAME";
    private const string Usuario = "ARQUITECTOS_DB_USER";
    private const string Clave = "ARQUITECTOS_DB_PASSWORD";

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
        string directorioInicial)
    {
        if (!environment.IsDevelopment())
        {
            return;
        }

        var valores = CrearValores(directorioInicial, ResolverConfianza(configuration, esDesarrollo: true));
        if (valores.Count == 0)
        {
            return;
        }

        configuration.Sources.Insert(IndiceInsercion(configuration.Sources), new MemoryConfigurationSource { InitialData = valores });
    }

    public static Dictionary<string, string?> CrearValores(string directorioInicial, bool confiarCertificado = true)
    {
        var ruta = Buscar(directorioInicial);
        if (ruta is null)
        {
            return [];
        }

        var variables = Leer(ruta);
        if (!variables.TryGetValue(Host, out var host) ||
            !variables.TryGetValue(BaseDatos, out var baseDatos) ||
            !variables.TryGetValue(Usuario, out var usuario) ||
            !variables.TryGetValue(Clave, out var clave) ||
            string.IsNullOrWhiteSpace(host) ||
            string.IsNullOrWhiteSpace(baseDatos) ||
            string.IsNullOrWhiteSpace(usuario) ||
            string.IsNullOrWhiteSpace(clave))
        {
            return [];
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
