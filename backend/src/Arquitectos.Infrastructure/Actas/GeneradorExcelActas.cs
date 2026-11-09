using Arquitectos.Application.Actas;
using ClosedXML.Excel;

namespace Arquitectos.Infrastructure.Actas;

public static class GeneradorExcelActas
{
    public static byte[] Generar(ConsultaActasDto consulta)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Actas");
        string[] titulos = ["N° Acta", "Fecha inicial", "Fecha final", "Contratista", "Observación", "Total"];
        for (var columna = 0; columna < titulos.Length; columna++)
        {
            hoja.Cell(1, columna + 1).Value = titulos[columna];
            hoja.Cell(1, columna + 1).Style.Font.Bold = true;
        }

        var fila = 2;
        foreach (var acta in consulta.Actas)
        {
            hoja.Cell(fila, 1).Value = acta.NumeroActa;
            hoja.Cell(fila, 2).Value = acta.FechaInicial.ToDateTime(TimeOnly.MinValue);
            hoja.Cell(fila, 2).Style.DateFormat.Format = "yyyy-mm-dd";
            hoja.Cell(fila, 3).Value = acta.FechaFinal.ToDateTime(TimeOnly.MinValue);
            hoja.Cell(fila, 3).Style.DateFormat.Format = "yyyy-mm-dd";
            hoja.Cell(fila, 4).Value = acta.Contratista;
            hoja.Cell(fila, 5).Value = acta.Observacion ?? "";
            hoja.Cell(fila, 6).Value = (double)acta.Total;
            hoja.Cell(fila, 6).Style.NumberFormat.Format = "$ #,##0";
            fila++;
        }

        hoja.Cell(fila, 5).Value = "TOTAL";
        hoja.Cell(fila, 5).Style.Font.Bold = true;
        hoja.Cell(fila, 6).Value = (double)consulta.TotalGeneral;
        hoja.Cell(fila, 6).Style.NumberFormat.Format = "$ #,##0";
        hoja.Cell(fila, 6).Style.Font.Bold = true;
        hoja.Columns().AdjustToContents();

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }
}
