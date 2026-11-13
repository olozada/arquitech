using Microsoft.Data.SqlClient;

namespace Arquitectos.Infrastructure.Configuration;

public static class GuardaDatosDemo
{
    public static readonly string[] SufijosPorDefecto = ["_DEV", "_E2E", "_TEST", "_QA"];

    public readonly record struct Resultado(bool Permitido, int CodigoSalida, string Mensaje, string Servidor, string BaseDatos);

    public static Resultado Evaluar(
        bool esDesarrollo,
        string? cadena,
        IEnumerable<string>? sufijos,
        bool reinicio,
        bool confirmar)
    {
        if (!esDesarrollo)
        {
            return Rechazo("Los comandos --reset, --seed-demo y --seed-plantilla solo están disponibles en Development.");
        }

        if (string.IsNullOrWhiteSpace(cadena))
        {
            return Rechazo("No hay cadena de conexión para el comando de demostración.");
        }

        SqlConnectionStringBuilder builder;
        try
        {
            builder = new SqlConnectionStringBuilder(cadena);
        }
        catch (ArgumentException)
        {
            return Rechazo("La cadena de conexión no es válida.");
        }

        var servidor = builder.DataSource;
        var baseDatos = builder.InitialCatalog.Trim();
        if (string.IsNullOrWhiteSpace(baseDatos))
        {
            return Rechazo($"La cadena del servidor {servidor} no indica una base de datos.");
        }

        var permitidos = Normalizar(sufijos);
        if (!permitidos.Any(sufijo => baseDatos.EndsWith(sufijo, StringComparison.OrdinalIgnoreCase)))
        {
            return new Resultado(
                false,
                1,
                $"La base {baseDatos} en {servidor} no admite --reset, --seed-demo ni --seed-plantilla. El nombre debe terminar en {string.Join(", ", permitidos)}.",
                servidor,
                baseDatos);
        }

        if (reinicio && !confirmar)
        {
            return new Resultado(
                false,
                1,
                $"--reset vacía la base {baseDatos} en {servidor}. Repite el comando con --confirmar.",
                servidor,
                baseDatos);
        }

        return new Resultado(true, 0, $"Servidor {servidor}, base {baseDatos}.", servidor, baseDatos);
    }

    private static Resultado Rechazo(string mensaje) => new(false, 1, mensaje, "", "");

    private static string[] Normalizar(IEnumerable<string>? sufijos)
    {
        var validos = (sufijos ?? [])
            .Select(sufijo => sufijo.Trim())
            .Where(sufijo => sufijo.Length >= 2 && sufijo.StartsWith('_'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return validos.Length == 0 ? SufijosPorDefecto : validos;
    }
}
