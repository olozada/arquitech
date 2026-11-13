using System.IO.Compression;

namespace Arquitectos.Infrastructure.Presupuestos;

public static class ValidadorLibroExcel
{
    public const int HojasMaximas = 50;
    public const int EntradasMaximas = 2_000;
    public const long DescomprimidoMaximo = 100L * 1024 * 1024;
    public static readonly TimeSpan TiempoMaximo = TimeSpan.FromSeconds(60);

    public static string? Revisar(string? nombre, Stream archivo)
    {
        if (!string.Equals(Path.GetExtension(nombre), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return "El archivo debe ser un libro .xlsx.";
        }

        return RevisarContenido(archivo);
    }

    public static string? RevisarContenido(Stream archivo)
    {
        if (!archivo.CanSeek)
        {
            return "No se pudo leer el archivo.";
        }

        try
        {
            var firma = new byte[4];
            var leidos = archivo.Read(firma, 0, firma.Length);
            if (leidos < 4 || firma[0] != 0x50 || firma[1] != 0x4B || firma[2] != 0x03 || firma[3] != 0x04)
            {
                return "El archivo no es un libro de Excel válido.";
            }

            archivo.Position = 0;
            using var zip = new ZipArchive(archivo, ZipArchiveMode.Read, leaveOpen: true);
            if (zip.Entries.Count > EntradasMaximas)
            {
                return $"El libro contiene más de {EntradasMaximas} archivos internos.";
            }

            long total = 0;
            foreach (var entrada in zip.Entries)
            {
                if (entrada.Length > DescomprimidoMaximo || total > DescomprimidoMaximo - entrada.Length)
                {
                    return "El libro descomprimido supera 100 MB.";
                }

                total += entrada.Length;
            }

            return null;
        }
        catch (InvalidDataException)
        {
            return "El archivo no es un libro de Excel válido.";
        }
        finally
        {
            if (archivo.CanSeek)
            {
                archivo.Position = 0;
            }
        }
    }
}
