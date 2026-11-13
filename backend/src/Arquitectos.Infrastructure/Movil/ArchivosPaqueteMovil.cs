namespace Arquitectos.Infrastructure.Movil;

internal static class ArchivosPaqueteMovil
{
    public const string Carpeta = "arquitectos-movil";

    public static string Directorio()
    {
        var carpeta = Path.Combine(Path.GetTempPath(), Carpeta);
        Directory.CreateDirectory(carpeta);
        return carpeta;
    }

    public static void Borrar(string? ruta)
    {
        if (string.IsNullOrEmpty(ruta))
        {
            return;
        }

        try
        {
            if (File.Exists(ruta))
            {
                File.Delete(ruta);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public static void LimpiarHuerfanos(TimeSpan antiguedad)
    {
        var carpeta = Path.Combine(Path.GetTempPath(), Carpeta);
        if (!Directory.Exists(carpeta))
        {
            return;
        }

        var limite = DateTime.UtcNow.Subtract(antiguedad);
        foreach (var archivo in Directory.EnumerateFiles(carpeta))
        {
            var nombre = Path.GetFileName(archivo);
            if (!EsTemporal(nombre) || File.GetLastWriteTimeUtc(archivo) >= limite)
            {
                continue;
            }

            Borrar(archivo);
        }
    }

    private static bool EsTemporal(string nombre) =>
        nombre.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase) ||
        nombre.EndsWith(".sqlite.gz", StringComparison.OrdinalIgnoreCase) ||
        nombre.EndsWith(".sqlite-journal", StringComparison.OrdinalIgnoreCase) ||
        nombre.EndsWith(".sqlite-wal", StringComparison.OrdinalIgnoreCase);
}
