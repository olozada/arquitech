using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Arquitectos.Infrastructure.Configuration;

public static class ArchivoEnv
{
    public const string ClaveConexion = "ConnectionStrings:Arquitectos";

    public static void AplicarConexion(IConfiguration configuration, string directorioInicial)
    {
        var valores = CrearValores(directorioInicial);
        if (valores.Count == 0)
        {
            return;
        }

        foreach (var valor in valores)
        {
            configuration[valor.Key] = valor.Value;
        }
    }

    public static Dictionary<string, string?> CrearValores(string directorioInicial)
    {
        var ruta = Buscar(directorioInicial);
        if (ruta is null)
        {
            return [];
        }

        var variables = Leer(ruta);
        if (!variables.TryGetValue("INTEGRACION_VENTAS_HOST", out var host) ||
            !variables.TryGetValue("INTEGRACION_VENTAS_DATABASE", out var baseDatos) ||
            !variables.TryGetValue("INTEGRACION_VENTAS_USER", out var usuario) ||
            !variables.TryGetValue("INTEGRACION_VENTAS_PASSWORD", out var clave) ||
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
            TrustServerCertificate = true,
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

            directorio = directorio.Parent;
        }

        return null;
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
