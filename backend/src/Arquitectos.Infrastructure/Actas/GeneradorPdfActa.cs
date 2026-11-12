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
                pagina.Margin(32);
                pagina.DefaultTextStyle(estilo => estilo.FontSize(10).LineHeight(1.2f));

                pagina.Content().Column(columna =>
                {
                    columna.Spacing(6);
                    columna.Item().Text($"Impresión de Acta No. {acta.NumeroActa}").FontSize(16).SemiBold();
                    if (acta.Cerrada)
                    {
                        columna.Item().Text("Acta cerrada. No se pueden agregar, modificar ni eliminar ítems.").SemiBold();
                    }
                    columna.Item().Row(fila =>
                    {
                        fila.RelativeItem().Text($"Fecha inicial: {acta.FechaInicial:yyyy-MM-dd}");
                        fila.RelativeItem().Text($"Fecha final: {acta.FechaFinal:yyyy-MM-dd}");
                        fila.RelativeItem().Text($"Registro: {acta.FechaRegistro.ToLocalTime():yyyy-MM-dd HH:mm}");
                    });
                    columna.Item().Row(fila =>
                    {
                        fila.RelativeItem(2).Text($"Arquitecto: {acta.Arquitecto}");
                        fila.RelativeItem().Text($"Revisor: {acta.Revisor}");
                    });
                    columna.Item().Text($"Contratista: {acta.Contratista}");
                    if (!string.IsNullOrWhiteSpace(acta.Observacion))
                    {
                        columna.Item().Text($"Observación: {acta.Observacion}");
                    }

                    foreach (var grupo in acta.Detalles.GroupBy(detalle => detalle.Construccion))
                    {
                        columna.Item().PaddingTop(8).Text(grupo.Key).SemiBold();
                        columna.Item().Table(tabla =>
                        {
                            tabla.ColumnsDefinition(columnas =>
                            {
                                columnas.RelativeColumn(1.6f);
                                columnas.RelativeColumn(1.4f);
                                columnas.RelativeColumn(3);
                                columnas.RelativeColumn(3);
                                columnas.RelativeColumn(1);
                                columnas.RelativeColumn(1.2f);
                                columnas.RelativeColumn(1.4f);
                                columnas.RelativeColumn(1.4f);
                            });

                            tabla.Header(encabezado =>
                            {
                                Encabezado(encabezado.Cell(), "Perfil");
                                Encabezado(encabezado.Cell(), "Estado");
                                Encabezado(encabezado.Cell(), "Concepto");
                                Encabezado(encabezado.Cell(), "Material");
                                Encabezado(encabezado.Cell(), "UM");
                                Encabezado(encabezado.Cell(), "Cantidad");
                                Encabezado(encabezado.Cell(), "Precio");
                                Encabezado(encabezado.Cell(), "Subtotal");
                            });

                            foreach (var linea in grupo)
                            {
                                Celda(tabla.Cell()).Text(linea.TipoUsuario);
                                Celda(tabla.Cell()).Text(linea.Aprobado ? "Aprobado" : "Rechazado");
                                Celda(tabla.Cell()).Text(linea.Concepto);
                                Celda(tabla.Cell()).Text(linea.Insumo ?? "");
                                Celda(tabla.Cell()).Text(linea.UnidadMedida);
                                Celda(tabla.Cell()).AlignRight().Text(linea.Cantidad.ToString("0.####", Cultura));
                                Celda(tabla.Cell()).AlignRight().Text(Moneda(linea.Precio));
                                Celda(tabla.Cell()).AlignRight().Text(Moneda(linea.Subtotal));
                            }
                        });
                    }

                    columna.Item().AlignRight().Text($"Total: {Moneda(acta.Total)}").SemiBold();

                    columna.Item().PaddingTop(24).Row(fila =>
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
            columna.Spacing(2);
            columna.Item().Text("________________________");
            columna.Item().AlignCenter().Text(rol).SemiBold();
            columna.Item().AlignCenter().Text(nombre);
        });
    }

    private static string Moneda(decimal valor) => valor.ToString("$ #,##0", Cultura);
}
