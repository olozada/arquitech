using System.Globalization;
using Arquitectos.Application.Actas;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Arquitectos.Infrastructure.Actas;

public static class GeneradorPdfInformeItems
{
    private static readonly CultureInfo Cultura = new("es-CO");

    public static byte[] Generar(InformeItemsDto informe)
    {
        return Document.Create(documento =>
        {
            documento.Page(pagina =>
            {
                pagina.Size(PageSizes.Letter.Landscape());
                pagina.Margin(28);
                pagina.DefaultTextStyle(estilo => estilo.FontSize(9).LineHeight(1.15f));

                pagina.Content().Column(columna =>
                {
                    var titulo = informe.Agrupado ? "Informe agrupado de ítems" : "Informe detallado de ítems";
                    if (informe.RomperPorActa)
                    {
                        titulo += " por acta";
                    }

                    columna.Spacing(8);
                    columna.Item().Text(titulo)
                        .FontSize(16)
                        .SemiBold();
                    foreach (var linea in informe.Encabezado ?? [])
                    {
                        columna.Item().Text(linea).FontSize(11).SemiBold();
                    }

                    var mostrarActa = !informe.Agrupado || informe.RomperPorActa;
                    var mostrarConstruccion = !informe.OcultarConstruccion;
                    columna.Item().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(columnas =>
                        {
                            if (mostrarActa)
                            {
                                columnas.ConstantColumn(48);
                            }

                            if (mostrarConstruccion)
                            {
                                columnas.ConstantColumn(126);
                            }

                            columnas.ConstantColumn(108);
                            columnas.ConstantColumn(100);
                            columnas.ConstantColumn(36);
                            columnas.ConstantColumn(64);
                            columnas.ConstantColumn(66);
                            columnas.ConstantColumn(68);
                            columnas.ConstantColumn(120);
                        });

                        tabla.Header(encabezado =>
                        {
                            if (mostrarActa)
                            {
                                Encabezado(encabezado.Cell(), "Acta");
                            }

                            if (mostrarConstruccion)
                            {
                                Encabezado(encabezado.Cell(), "Construcción");
                            }

                            Encabezado(encabezado.Cell(), "Concepto");
                            Encabezado(encabezado.Cell(), "Material");
                            Encabezado(encabezado.Cell(), "UM");
                            Encabezado(encabezado.Cell(), "Cantidad");
                            Encabezado(encabezado.Cell(), "Precio");
                            Encabezado(encabezado.Cell(), "Estado");
                            Encabezado(encabezado.Cell(), "Subtotal");
                        });

                        for (var indice = 0; indice < informe.Items.Count; indice++)
                        {
                            var item = informe.Items[indice];
                            if (mostrarActa)
                            {
                                Celda(tabla.Cell()).Text(item.NumeroActa?.ToString(Cultura) ?? "");
                            }

                            if (mostrarConstruccion)
                            {
                                Celda(tabla.Cell()).Text(item.Construccion);
                            }

                            Celda(tabla.Cell()).Text(item.Concepto);
                            Celda(tabla.Cell()).Text(item.Material);
                            Celda(tabla.Cell()).Text(item.Unidad);
                            Celda(tabla.Cell()).AlignRight().Text(item.Cantidad.ToString("0.####", Cultura));
                            Celda(tabla.Cell()).AlignRight().Text(Moneda(item.Precio));
                            Celda(tabla.Cell()).Text(item.Aprobado ? "Aprobado" : "Rechazado");
                            Celda(tabla.Cell()).AlignRight().Text(Moneda(item.Subtotal));

                            var siguiente = indice + 1 < informe.Items.Count ? informe.Items[indice + 1] : null;
                            if (informe.RomperPorActa && siguiente?.NumeroActa != item.NumeroActa)
                            {
                                var subtotal = informe.Items
                                    .Where(fila => fila.NumeroActa == item.NumeroActa)
                                    .Sum(fila => fila.Subtotal);
                                Corte(tabla, mostrarActa, mostrarConstruccion, item.NumeroActa, subtotal);
                            }
                        }

                        Total(tabla, mostrarActa, mostrarConstruccion, informe.Total);
                    });
                });

                pagina.Footer().AlignRight().Text(texto =>
                {
                    texto.Span("Página ");
                    texto.CurrentPageNumber();
                    texto.Span(" de ");
                    texto.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static void Total(TableDescriptor tabla, bool mostrarActa, bool mostrarConstruccion, decimal total)
    {
        var columnas = (mostrarActa ? 1 : 0) + (mostrarConstruccion ? 1 : 0) + 6;
        tabla.Cell().ColumnSpan((uint)columnas).Background("#e8f8f5").Border(1).BorderColor("#1abb9c").Padding(4)
            .AlignMiddle().Text("TOTAL").FontSize(12).FontColor("#0f766e").Bold();
        tabla.Cell().Background("#e8f8f5").Border(1).BorderColor("#1abb9c").Padding(4)
            .AlignRight().AlignMiddle().Text(Moneda(total)).FontSize(13).FontColor("#0f766e").Bold();
    }

    private static void Corte(TableDescriptor tabla, bool mostrarActa, bool mostrarConstruccion, int? numeroActa, decimal subtotal)
    {
        var titulo = numeroActa is int numero ? $"Total acta {numero.ToString(Cultura)}" : "Total acta";
        var columnas = (mostrarActa ? 1 : 0) + (mostrarConstruccion ? 1 : 0) + 6;
        tabla.Cell().ColumnSpan((uint)columnas).Background("#eef6f4").Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3)
            .Text(titulo).SemiBold();
        tabla.Cell().Background("#eef6f4").Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3)
            .AlignRight().Text(Moneda(subtotal)).SemiBold();
    }

    private static void Encabezado(IContainer contenedor, string titulo) =>
        contenedor.Background(Colors.Grey.Lighten3).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3)
            .Text(titulo).SemiBold();

    private static IContainer Celda(IContainer contenedor) =>
        contenedor.Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3);

    private static string Moneda(decimal valor) => valor.ToString("$ #,##0", Cultura);
}
