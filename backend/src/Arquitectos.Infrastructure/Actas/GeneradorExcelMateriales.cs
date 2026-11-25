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
        const int codigo = 1;
        const int material = 2;
        const int unidad = 3;
        var acta = detallado ? 4 : 0;
        var cantidad = detallado ? 5 : 4;
        var precio = detallado ? 6 : 5;
        var subtotal = detallado ? 7 : 6;
        var titulos = detallado
            ? new[] { "Código", rotuloColumna, "UM", "Acta", "Cantidad", "Precio", "Subtotal" }
            : new[] { "Código", rotuloColumna, "UM", "Cantidad", "Precio", "Subtotal" };
        var casas = CortesMaterial.Casas(informe.Items);
        var varias = casas.Exists(casa => !string.IsNullOrWhiteSpace(casa.Nombre));
        if (!varias)
        {
            fila = EscribirTitulos(hoja, fila, titulos);
        }

        for (var indice = 0; indice < casas.Count; indice++)
        {
            var casa = casas[indice];
            if (varias && indice > 0)
            {
                hoja.PageSetup.AddHorizontalPageBreak(fila - 1);
            }

            if (varias)
            {
                hoja.Cell(fila, 1).Value = $"Construcción: {casa.Nombre}";
                hoja.Range(fila, 1, fila, subtotal).Merge().Style.Font.Bold = true;
                fila++;
                fila = EscribirTitulos(hoja, fila, titulos);
            }

            foreach (var corte in CortesMaterial.Armar(casa.Items, subtotales && detallado))
            {
                if (corte.Encabezado)
                {
                    hoja.Cell(fila, codigo).Value = corte.Codigo;
                    hoja.Cell(fila, material).Value = corte.Material;
                    hoja.Cell(fila, unidad).Value = corte.Unidad;
                    hoja.Range(fila, 1, fila, subtotal).Style.Font.Bold = true;
                }
                else if (corte.Linea is ItemMaterialInformeDto item)
                {
                    if (!corte.OcultarIdentidad)
                    {
                        hoja.Cell(fila, codigo).Value = item.Codigo;
                        hoja.Cell(fila, material).Value = item.Material;
                        hoja.Cell(fila, unidad).Value = item.Unidad;
                    }

                    if (detallado && item.NumeroActa is int numeroActa)
                    {
                        hoja.Cell(fila, acta).Value = numeroActa;
                    }

                    hoja.Cell(fila, cantidad).Value = (double)item.Cantidad;
                    hoja.Cell(fila, cantidad).Style.NumberFormat.Format = "#,##0.00";
                    hoja.Cell(fila, precio).Value = (double)item.Precio;
                    hoja.Cell(fila, precio).Style.NumberFormat.Format = "$ #,##0";
                    hoja.Cell(fila, subtotal).Value = (double)item.Subtotal;
                    hoja.Cell(fila, subtotal).Style.NumberFormat.Format = "$ #,##0";
                }
                else
                {
                    hoja.Cell(fila, material).Value = "Subtotal";
                    hoja.Cell(fila, cantidad).Value = (double)corte.Cantidad;
                    hoja.Cell(fila, cantidad).Style.NumberFormat.Format = "#,##0.00";
                    hoja.Cell(fila, subtotal).Value = (double)corte.Valor;
                    hoja.Cell(fila, subtotal).Style.NumberFormat.Format = "$ #,##0";
                    hoja.Range(fila, 1, fila, subtotal).Style.Font.Bold = true;
                }

                fila++;
            }

            if (varias)
            {
                hoja.Cell(fila, precio).Value = "Total construcción";
                hoja.Cell(fila, precio).Style.Font.Bold = true;
                hoja.Cell(fila, subtotal).Value = (double)casa.Items.Sum(item => item.Subtotal);
                hoja.Cell(fila, subtotal).Style.NumberFormat.Format = "$ #,##0";
                hoja.Cell(fila, subtotal).Style.Font.Bold = true;
                fila++;
            }
        }

        hoja.Cell(fila, precio).Value = "TOTAL";
        hoja.Cell(fila, precio).Style.Font.Bold = true;
        hoja.Cell(fila, subtotal).Value = (double)informe.Total;
        hoja.Cell(fila, subtotal).Style.NumberFormat.Format = "$ #,##0";
        hoja.Cell(fila, subtotal).Style.Font.Bold = true;
        hoja.Columns().AdjustToContents();
        hoja.PageSetup.PageOrientation = XLPageOrientation.Landscape;

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    private static int EscribirTitulos(IXLWorksheet hoja, int fila, string[] titulos)
    {
        for (var columna = 0; columna < titulos.Length; columna++)
        {
            hoja.Cell(fila, columna + 1).Value = titulos[columna];
            hoja.Cell(fila, columna + 1).Style.Font.Bold = true;
        }

        return fila + 1;
    }
}
