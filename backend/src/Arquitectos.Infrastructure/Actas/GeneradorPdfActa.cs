using System.Globalization;
using Arquitectos.Application.Actas;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Arquitectos.Infrastructure.Actas;

public static class GeneradorPdfActa
{
    private static readonly CultureInfo Cultura = new("es-CO");

    public static byte[] Generar(ActaDto acta)
    {
        return Document.Create(documento =>
        {
            documento.Page(pagina =>
            {
                pagina.Size(PageSizes.Letter);
                pagina.Margin(36);
                pagina.DefaultTextStyle(estilo => estilo.FontSize(10));

                pagina.Content().Column(columna =>
                {
                    columna.Spacing(10);
                    columna.Item().Text($"Impresión de Acta No. {acta.NumeroActa}").FontSize(16).SemiBold();
                    columna.Item().Text($"Fecha inicial: {acta.FechaInicial:yyyy-MM-dd}");
                    columna.Item().Text($"Fecha final: {acta.FechaFinal:yyyy-MM-dd}");
                    columna.Item().Text($"Arquitecto: {acta.Arquitecto}");
                    columna.Item().Text($"Revisor: {acta.Revisor}");
                    columna.Item().Text($"Contratista: {acta.Contratista}");
                    columna.Item().Text($"Observación: {acta.Observacion}");
                    columna.Item().Text($"Fecha de registro: {acta.FechaRegistro.ToLocalTime():yyyy-MM-dd HH:mm}");

                    foreach (var grupo in acta.Detalles.GroupBy(detalle => detalle.Construccion))
                    {
                        columna.Item().PaddingTop(6).Text(grupo.Key).SemiBold();
                        columna.Item().Table(tabla =>
                        {
                            tabla.ColumnsDefinition(columnas =>
                            {
                                columnas.RelativeColumn(3);
                                columnas.RelativeColumn(3);
                                columnas.RelativeColumn(1);
                                columnas.RelativeColumn(1.2f);
                                columnas.RelativeColumn(1.4f);
                                columnas.RelativeColumn(1.4f);
                            });

                            tabla.Header(encabezado =>
                            {
                                Encabezado(encabezado.Cell(), "Concepto");
                                Encabezado(encabezado.Cell(), "Material");
                                Encabezado(encabezado.Cell(), "UM");
                                Encabezado(encabezado.Cell(), "Cantidad");
                                Encabezado(encabezado.Cell(), "Precio");
                                Encabezado(encabezado.Cell(), "Subtotal");
                            });

                            foreach (var linea in grupo)
                            {
                                Celda(tabla.Cell()).Text(linea.Concepto);
                                Celda(tabla.Cell()).Text(linea.Material ?? "");
                                Celda(tabla.Cell()).Text(linea.UnidadMedida);
                                Celda(tabla.Cell()).AlignRight().Text(linea.Cantidad.ToString("0.####", Cultura));
                                Celda(tabla.Cell()).AlignRight().Text(Moneda(linea.Precio));
                                Celda(tabla.Cell()).AlignRight().Text(Moneda(linea.Subtotal));
                            }
                        });
                    }

                    columna.Item().AlignRight().Text($"Total: {Moneda(acta.Total)}").SemiBold();

                    columna.Item().PaddingTop(36).Row(fila =>
                    {
                        Firma(fila, "Arquitecto", acta.Arquitecto);
                        Firma(fila, "Revisor", acta.Revisor);
                        Firma(fila, "Contratista", acta.Contratista);
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
        contenedor.Background(Colors.Grey.Lighten3).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(4)
            .Text(titulo).SemiBold();

    private static IContainer Celda(IContainer contenedor) =>
        contenedor.Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(4);

    private static void Firma(RowDescriptor fila, string rol, string nombre)
    {
        fila.RelativeItem().AlignCenter().Column(columna =>
        {
            columna.Item().Text("________________________");
            columna.Item().AlignCenter().Text(rol).SemiBold();
            columna.Item().AlignCenter().Text(nombre);
        });
    }

    private static string Moneda(decimal valor) => valor.ToString("$ #,##0", Cultura);
}
