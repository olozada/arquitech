using Arquitectos.Application.Actas;
using ClosedXML.Excel;

namespace Arquitectos.Infrastructure.Actas;

public static class GeneradorExcelMateriales
{
    public static byte[] Generar(InformeMaterialesDto informe)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Materiales");
        var fila = 1;
        foreach (var linea in informe.Encabezado ?? [])
        {
            hoja.Cell(fila, 1).Value = linea;
            fila++;
        }

        if (fila > 1)
        {
            fila++;
        }

        var detallado = !informe.Agrupado;
        var titulos = detallado
            ? new[] { "Acta", "Código", "Material", "UM", "Cantidad", "Precio", "Estado", "Subtotal" }
            : new[] { "Código", "Material", "UM", "Cantidad", "Precio", "Estado", "Subtotal" };
        for (var columna = 0; columna < titulos.Length; columna++)
        {
            hoja.Cell(fila, columna + 1).Value = titulos[columna];
            hoja.Cell(fila, columna + 1).Style.Font.Bold = true;
        }

        fila++;
        var inicio = detallado ? 2 : 1;
        foreach (var item in informe.Items)
        {
            if (detallado && item.NumeroActa is int numeroActa)
            {
                hoja.Cell(fila, 1).Value = numeroActa;
            }

            hoja.Cell(fila, inicio).Value = item.Codigo;
            hoja.Cell(fila, inicio + 1).Value = item.Material;
            hoja.Cell(fila, inicio + 2).Value = item.Unidad;
            hoja.Cell(fila, inicio + 3).Value = (double)item.Cantidad;
            hoja.Cell(fila, inicio + 3).Style.NumberFormat.Format = "#,##0.00";
            hoja.Cell(fila, inicio + 4).Value = (double)item.Precio;
            hoja.Cell(fila, inicio + 4).Style.NumberFormat.Format = "$ #,##0";
            hoja.Cell(fila, inicio + 5).Value = item.Aprobado ? "Aprobado" : "Rechazado";
            hoja.Cell(fila, inicio + 6).Value = (double)item.Subtotal;
            hoja.Cell(fila, inicio + 6).Style.NumberFormat.Format = "$ #,##0";
            fila++;
        }

        hoja.Cell(fila, inicio + 5).Value = "TOTAL";
        hoja.Cell(fila, inicio + 5).Style.Font.Bold = true;
        hoja.Cell(fila, inicio + 6).Value = (double)informe.Total;
        hoja.Cell(fila, inicio + 6).Style.NumberFormat.Format = "$ #,##0";
        hoja.Cell(fila, inicio + 6).Style.Font.Bold = true;
        hoja.Columns().AdjustToContents();

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }
}
