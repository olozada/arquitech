using Arquitectos.Application.Actas;
using Arquitectos.Infrastructure.Informes;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Arquitectos.Infrastructure.Actas;

public static class GeneradorPdfActa
{
    public static byte[] Generar(ActaDto acta)
    {
        var grupos = acta.Detalles.GroupBy(detalle => detalle.Construccion).ToList();
        return Document.Create(documento =>
        {
            documento.Page(pagina =>
            {
                FormatoDocumentoObra.Preparar(pagina);
                pagina.Content().Column(columna =>
                {
                    columna.Spacing(4);
                    FormatoDocumentoObra.MembreteYTitulo(columna, $"Acta de liquidación No. {acta.NumeroActa}");
                    columna.Item().Row(fila =>
                    {
                        fila.RelativeItem().Text($"Fecha inicial: {FormatoDocumentoObra.Fecha(acta.FechaInicial)}");
                        fila.RelativeItem().Text($"Fecha final: {FormatoDocumentoObra.Fecha(acta.FechaFinal)}");
                    });
                    columna.Item().Text("Mano de obra construcción viviendas");
                    if (acta.Cerrada)
                    {
                        columna.Item().Text("Acta cerrada.").SemiBold();
                    }

                    foreach (var grupo in grupos)
                    {
                        FormatoDocumentoObra.Casa(columna, grupo.Key);
                        Tabla(columna, grupo);
                        if (grupos.Count > 1)
                        {
                            FormatoDocumentoObra.ValorTotal(columna, grupo.Sum(LineaTotal));
                        }
                    }

                    FormatoDocumentoObra.ValorTotal(columna, acta.Total);
                    if (!string.IsNullOrWhiteSpace(acta.Observacion))
                    {
                        columna.Item().PaddingTop(8).Text($"Comentarios: {acta.Observacion}");
                    }

                    FormatoDocumentoObra.CierreArmenia(columna, acta.FechaFinal);
                    columna.Item().PaddingTop(28).Row(fila =>
                    {
                        Firma(fila, "Arquitecto", acta.Arquitecto);
                        Firma(fila, "Contratista", acta.Contratista);
                        Firma(fila, "Revisor fiscal", acta.Revisor);
                    });
                });
                FormatoDocumentoObra.Pie(pagina);
            });
        }).GeneratePdf();
    }

    private static void Tabla(ColumnDescriptor columna, IEnumerable<LineaActaDto> lineas)
    {
        columna.Item().Table(tabla =>
        {
            tabla.ColumnsDefinition(columnas =>
            {
                columnas.RelativeColumn(2.4f);
                columnas.RelativeColumn(2.6f);
                columnas.RelativeColumn(1.1f);
                columnas.RelativeColumn(1.3f);
                columnas.RelativeColumn(1.4f);
            });
            tabla.Header(encabezado =>
            {
                FormatoDocumentoObra.Encabezado(encabezado.Cell(), "Concepto");
                FormatoDocumentoObra.Encabezado(encabezado.Cell(), "Descripción");
                FormatoDocumentoObra.Encabezado(encabezado.Cell(), "Cantidad");
                FormatoDocumentoObra.Encabezado(encabezado.Cell(), "Precio");
                FormatoDocumentoObra.Encabezado(encabezado.Cell(), "Total");
            });

            foreach (var linea in lineas)
            {
                FormatoDocumentoObra.Celda(tabla.Cell()).Text(linea.Concepto);
                FormatoDocumentoObra.Celda(tabla.Cell()).Text(TextoLinea(linea));
                FormatoDocumentoObra.Celda(tabla.Cell()).AlignRight().Text(TextoCantidad(linea.Cantidad, linea.UnidadMedida));
                FormatoDocumentoObra.Celda(tabla.Cell()).AlignRight().Text(FormatoDocumentoObra.Moneda(linea.Precio));
                FormatoDocumentoObra.Celda(tabla.Cell()).AlignRight().Text(FormatoDocumentoObra.Moneda(LineaTotal(linea)));
            }
        });
    }

    private static string TextoLinea(LineaActaDto linea)
    {
        var texto = linea.Descripcion;
        if (!string.IsNullOrWhiteSpace(linea.Insumo))
        {
            texto = string.IsNullOrWhiteSpace(texto) ? linea.Insumo : $"{texto}\n{linea.Insumo}";
        }

        if (!linea.Aprobado)
        {
            texto = string.IsNullOrWhiteSpace(texto) ? "(rechazado)" : $"{texto}\n(rechazado)";
        }

        return texto;
    }

    private static string TextoCantidad(decimal cantidad, string unidad)
    {
        var texto = FormatoDocumentoObra.Cantidad(cantidad);
        return string.IsNullOrWhiteSpace(unidad) ? texto : $"{texto}\n{unidad}";
    }

    private static decimal LineaTotal(LineaActaDto linea) => linea.Aprobado ? linea.Subtotal : 0;

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
}
