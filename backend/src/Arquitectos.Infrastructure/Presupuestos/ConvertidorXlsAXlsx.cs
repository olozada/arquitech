using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace Arquitectos.Infrastructure.Presupuestos;

public static class ConvertidorXlsAXlsx
{
    public static bool RequiereConversion(string ruta) =>
        string.Equals(Path.GetExtension(ruta), ".xls", StringComparison.OrdinalIgnoreCase);

    public static string AsegurarXlsx(string ruta)
    {
        if (!RequiereConversion(ruta))
        {
            return ruta;
        }

        using var entrada = File.OpenRead(ruta);
        using var hssf = new HSSFWorkbook(entrada);
        var xssf = new XSSFWorkbook();
        Copiar(hssf, xssf);
        var destino = Path.Combine(Path.GetTempPath(), $"arquitectos-{Guid.NewGuid():N}.xlsx");
        using (var salida = File.Create(destino))
        {
            xssf.Write(salida, leaveOpen: false);
        }

        return destino;
    }

    private static void Copiar(IWorkbook origen, IWorkbook destino)
    {
        for (var indice = 0; indice < origen.NumberOfSheets; indice++)
        {
            var hojaOrigen = origen.GetSheetAt(indice);
            var hojaDestino = destino.CreateSheet(hojaOrigen.SheetName);
            for (var fila = hojaOrigen.FirstRowNum; fila <= hojaOrigen.LastRowNum; fila++)
            {
                var filaOrigen = hojaOrigen.GetRow(fila);
                if (filaOrigen is null)
                {
                    continue;
                }

                var filaDestino = hojaDestino.CreateRow(fila);
                for (var columna = filaOrigen.FirstCellNum; columna < filaOrigen.LastCellNum; columna++)
                {
                    if (columna < 0)
                    {
                        continue;
                    }

                    var celdaOrigen = filaOrigen.GetCell(columna);
                    if (celdaOrigen is null)
                    {
                        continue;
                    }

                    var celdaDestino = filaDestino.CreateCell(columna);
                    switch (celdaOrigen.CellType)
                    {
                        case CellType.Numeric:
                            celdaDestino.SetCellValue(celdaOrigen.NumericCellValue);
                            break;
                        case CellType.String:
                            celdaDestino.SetCellValue(celdaOrigen.StringCellValue);
                            break;
                        case CellType.Boolean:
                            celdaDestino.SetCellValue(celdaOrigen.BooleanCellValue);
                            break;
                        case CellType.Formula:
                            VolcarFormulaComoValor(celdaOrigen, celdaDestino);
                            break;
                        case CellType.Blank:
                            break;
                        default:
                            celdaDestino.SetCellValue(celdaOrigen.ToString());
                            break;
                    }
                }
            }
        }
    }

    private static void VolcarFormulaComoValor(ICell origen, ICell destino)
    {
        try
        {
            switch (origen.CachedFormulaResultType)
            {
                case CellType.Numeric:
                    destino.SetCellValue(origen.NumericCellValue);
                    return;
                case CellType.String:
                    destino.SetCellValue(origen.StringCellValue);
                    return;
                case CellType.Boolean:
                    destino.SetCellValue(origen.BooleanCellValue);
                    return;
                default:
                    break;
            }
        }
        catch
        {
            // usar texto o cero
        }

        var texto = origen.ToString();
        if (double.TryParse(texto, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var numero))
        {
            destino.SetCellValue(numero);
        }
        else if (!string.IsNullOrWhiteSpace(texto))
        {
            destino.SetCellValue(texto);
        }
    }
}