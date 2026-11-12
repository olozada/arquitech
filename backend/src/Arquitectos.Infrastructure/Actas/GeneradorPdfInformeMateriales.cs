using System.Globalization;
using Arquitectos.Application.Actas;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Arquitectos.Infrastructure.Actas;

public static class GeneradorPdfInformeMateriales
{
    private static readonly CultureInfo Cultura = new("es-CO");

    public static byte[] Generar(InformeMaterialesDto informe)
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
                    columna.Spacing(8);
                    columna.Item().Text("Informe por materiales").FontSize(16).SemiBold();
                    foreach (var linea in informe.Encabezado ?? [])
                    {
                        columna.Item().Text(linea).FontSize(11).SemiBold();
                    }

                    var detallado = !informe.Agrupado;
                    columna.Item().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(columnas =>
                        {
                            if (detallado)
                            {
                                columnas.ConstantColumn(48);
                            }

                            columnas.ConstantColumn(70);
                            columnas.RelativeColumn(3);
                            columnas.ConstantColumn(40);
                            columnas.ConstantColumn(70);
                            columnas.ConstantColumn(80);
                            columnas.ConstantColumn(72);
                            columnas.ConstantColumn(100);
                        });

                        tabla.Header(encabezado =>
                        {
                            if (detallado)
                            {
                                Encabezado(encabezado.Cell(), "Acta");
                            }

                            Encabezado(encabezado.Cell(), "Código");
                            Encabezado(encabezado.Cell(), "Material");
                            Encabezado(encabezado.Cell(), "UM");
                            Encabezado(encabezado.Cell(), "Cantidad");
                            Encabezado(encabezado.Cell(), "Precio");
                            Encabezado(encabezado.Cell(), "Estado");
                            Encabezado(encabezado.Cell(), "Subtotal");
                        });

                        foreach (var item in informe.Items)
                        {
                            if (detallado)
                            {
                                Celda(tabla.Cell()).Text(item.NumeroActa?.ToString(Cultura) ?? "");
                            }

                            Celda(tabla.Cell()).Text(item.Codigo);
                            Celda(tabla.Cell()).Text(item.Material);
                            Celda(tabla.Cell()).Text(item.Unidad);
                            Celda(tabla.Cell()).AlignRight().Text(item.Cantidad.ToString("0.####", Cultura));
                            Celda(tabla.Cell()).AlignRight().Text(Moneda(item.Precio));
                            Celda(tabla.Cell()).Text(item.Aprobado ? "Aprobado" : "Rechazado");
                            Celda(tabla.Cell()).AlignRight().Text(Moneda(item.Subtotal));
                        }

                        tabla.Cell().ColumnSpan((uint)(detallado ? 7 : 6)).Background("#e8f8f5").Border(1).BorderColor("#1abb9c").Padding(4)
                            .AlignMiddle().Text("TOTAL").FontSize(12).FontColor("#0f766e").Bold();
                        tabla.Cell().Background("#e8f8f5").Border(1).BorderColor("#1abb9c").Padding(4)
                            .AlignRight().AlignMiddle().Text(Moneda(informe.Total)).FontSize(13).FontColor("#0f766e").Bold();
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

    private static void Encabezado(IContainer contenedor, string titulo) =>
        contenedor.Background(Colors.Grey.Lighten3).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3)
            .Text(titulo).SemiBold();

    private static IContainer Celda(IContainer contenedor) =>
        contenedor.Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3);

    private static string Moneda(decimal valor) => valor.ToString("$ #,##0", Cultura);
}
