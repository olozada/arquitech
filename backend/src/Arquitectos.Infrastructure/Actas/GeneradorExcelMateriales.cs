using Arquitectos.Application.Actas;
using ClosedXML.Excel;

namespace Arquitectos.Infrastructure.Actas;

public static class GeneradorExcelMateriales
{
    public static byte[] Generar(
        InformeMaterialesDto informe,
        bool subtotales = false,
        string hojaNombre = "Materiales",
        string rotuloColumna = "Material")
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add(hojaNombre);
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
            ? new[] { "Acta", "Código", rotuloColumna, "UM", "Cantidad", "Precio", "Estado", "Subtotal" }
            : new[] { "Código", rotuloColumna, "UM", "Cantidad", "Precio", "Estado", "Subtotal" };
        for (var columna = 0; columna < titulos.Length; columna++)
        {
            hoja.Cell(fila, columna + 1).Value = titulos[columna];
            hoja.Cell(fila, columna + 1).Style.Font.Bold = true;
        }

        fila++;
        var inicio = detallado ? 2 : 1;
        var casas = CortesMaterial.Casas(informe.Items);
        var varias = casas.Exists(casa => !string.IsNullOrWhiteSpace(casa.Nombre));
        foreach (var casa in casas)
        {
            if (varias)
            {
                hoja.Cell(fila, 1).Value = $"Construcción: {casa.Nombre}";
                hoja.Range(fila, 1, fila, inicio + 6).Merge().Style.Font.Bold = true;
                fila++;
            }

            foreach (var corte in CortesMaterial.Armar(casa.Items, subtotales && detallado))
            {
                if (corte.Linea is ItemMaterialInformeDto item)
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
                }
                else
                {
                    hoja.Cell(fila, inicio + 1).Value = $"Subtotal {corte.Material}";
                    hoja.Cell(fila, inicio + 3).Value = (double)corte.Cantidad;
                    hoja.Cell(fila, inicio + 3).Style.NumberFormat.Format = "#,##0.00";
                    hoja.Cell(fila, inicio + 6).Value = (double)corte.Valor;
                    hoja.Cell(fila, inicio + 6).Style.NumberFormat.Format = "$ #,##0";
                    hoja.Range(fila, 1, fila, inicio + 6).Style.Font.Bold = true;
                }

                fila++;
            }

            if (varias)
            {
                hoja.Cell(fila, inicio + 5).Value = "Total construcción";
                hoja.Cell(fila, inicio + 5).Style.Font.Bold = true;
                hoja.Cell(fila, inicio + 6).Value = (double)casa.Items.Sum(item => item.Subtotal);
                hoja.Cell(fila, inicio + 6).Style.NumberFormat.Format = "$ #,##0";
                hoja.Cell(fila, inicio + 6).Style.Font.Bold = true;
                fila++;
            }
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
