using Arquitectos.Application.Facturas;
using ClosedXML.Excel;

namespace Arquitectos.Infrastructure.Facturas;

public static class GeneradorExcelFacturas
{
    public static byte[] Generar(InformeFacturasDto informe)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add(informe.Agrupado ? "Acumulado" : "Detallado");
        var fila = 1;
        foreach (var linea in informe.Encabezado)
        {
            hoja.Cell(fila, 1).Value = linea;
            fila++;
        }

        if (fila > 1)
        {
            fila++;
        }

        var titulos = informe.Agrupado
            ? new[] { "Construcción", "Código", "Insumo", "UM", "Cantidad", "Precio", "Subtotal" }
            : new[] { "Factura", "Fecha", "Construcción", "Código", "Insumo", "Descripción", "UM", "Cantidad", "Precio", "Subtotal" };
        for (var columna = 0; columna < titulos.Length; columna++)
        {
            hoja.Cell(fila, columna + 1).Value = titulos[columna];
            hoja.Cell(fila, columna + 1).Style.Font.Bold = true;
        }

        fila++;
        foreach (var item in informe.Items)
        {
            var columna = 1;
            if (!informe.Agrupado)
            {
                hoja.Cell(fila, columna++).Value = item.Numero ?? "";
                hoja.Cell(fila, columna++).Value = item.Fecha?.ToString("dd/MM/yyyy") ?? "";
            }

            hoja.Cell(fila, columna++).Value = item.Construccion;
            hoja.Cell(fila, columna++).Value = item.Codigo;
            hoja.Cell(fila, columna++).Value = item.Insumo;
            if (!informe.Agrupado)
            {
                hoja.Cell(fila, columna++).Value = item.Descripcion;
            }

            hoja.Cell(fila, columna++).Value = item.Unidad;
            hoja.Cell(fila, columna++).Value = item.Cantidad;
            hoja.Cell(fila, columna++).Value = item.Precio;
            hoja.Cell(fila, columna).Value = item.Subtotal;
            fila++;
        }

        hoja.Cell(fila, titulos.Length).Value = informe.Total;
        hoja.Cell(fila, titulos.Length).Style.Font.Bold = true;
        hoja.Columns().AdjustToContents();
        using var flujo = new MemoryStream();
        libro.SaveAs(flujo);
        return flujo.ToArray();
    }
}
