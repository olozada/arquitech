using Arquitectos.Application.Facturas;
using Arquitectos.Infrastructure.Informes;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Arquitectos.Infrastructure.Facturas;

public static class GeneradorPdfFactura
{
    public static byte[] Generar(FacturaDto factura)
    {
        var grupos = factura.Detalles.GroupBy(detalle => detalle.Construccion).ToList();
        return Document.Create(documento =>
        {
            documento.Page(pagina =>
            {
                FormatoDocumentoObra.Preparar(pagina);
                pagina.Content().Column(columna =>
                {
                    columna.Spacing(4);
                    FormatoDocumentoObra.MembreteYTitulo(columna, $"Factura No. {factura.Numero}");
                    columna.Item().Text($"Fecha: {FormatoDocumentoObra.Fecha(factura.Fecha)}");
                    columna.Item().Text($"Proveedor: {factura.Proveedor}");
                    if (!string.IsNullOrWhiteSpace(factura.OrdenCompra))
                    {
                        columna.Item().Text($"Orden de compra: {factura.OrdenCompra}");
                    }

                    foreach (var grupo in grupos)
                    {
                        FormatoDocumentoObra.Casa(columna, grupo.Key);
                        columna.Item().Table(tabla =>
                        {
                            DefinirDetalle(tabla);
                            foreach (var linea in grupo)
                            {
                                FormatoDocumentoObra.Celda(tabla.Cell()).Text(linea.Insumo);
                                FormatoDocumentoObra.Celda(tabla.Cell()).Text(linea.Descripcion);
                                FormatoDocumentoObra.Celda(tabla.Cell()).AlignRight().Text(TextoCantidad(linea.Cantidad, linea.UnidadMedida));
                                FormatoDocumentoObra.Celda(tabla.Cell()).AlignRight().Text(FormatoDocumentoObra.Moneda(linea.Precio));
                                FormatoDocumentoObra.Celda(tabla.Cell()).AlignRight().Text(FormatoDocumentoObra.Moneda(linea.Subtotal));
                            }
                        });
                        if (grupos.Count > 1)
                        {
                            FormatoDocumentoObra.ValorTotal(columna, grupo.Sum(linea => linea.Subtotal));
                        }
                    }

                    FormatoDocumentoObra.ValorTotal(columna, factura.Total);
                    if (!string.IsNullOrWhiteSpace(factura.Observacion))
                    {
                        columna.Item().PaddingTop(8).Text($"Comentarios: {factura.Observacion}");
                    }

                    FormatoDocumentoObra.CierreArmenia(columna, factura.Fecha);
                });
                FormatoDocumentoObra.Pie(pagina);
            });
        }).GeneratePdf();
    }

    public static byte[] GenerarInforme(InformeFacturasDto informe)
    {
        var titulo = informe.Agrupado ? "Valor de factura acumulado" : "Valor de factura detallado";
        var grupos = informe.Items.GroupBy(item => item.Construccion).ToList();
        return Document.Create(documento =>
        {
            documento.Page(pagina =>
            {
                FormatoDocumentoObra.Preparar(pagina);
                pagina.Content().Column(columna =>
                {
                    columna.Spacing(4);
                    FormatoDocumentoObra.MembreteYTitulo(columna, titulo);
                    foreach (var linea in informe.Encabezado)
                    {
                        columna.Item().Text(linea);
                    }

                    foreach (var grupo in grupos)
                    {
                        FormatoDocumentoObra.Casa(columna, grupo.Key);
                        columna.Item().Table(tabla =>
                        {
                            if (informe.Agrupado)
                            {
                                DefinirAcumulado(tabla);
                            }
                            else
                            {
                                DefinirDetallado(tabla);
                            }

                            foreach (var item in grupo)
                            {
                                if (!informe.Agrupado)
                                {
                                    FormatoDocumentoObra.Celda(tabla.Cell()).Text(item.Numero ?? "");
                                }

                                FormatoDocumentoObra.Celda(tabla.Cell()).Text(item.Insumo);
                                if (!informe.Agrupado)
                                {
                                    FormatoDocumentoObra.Celda(tabla.Cell()).Text(item.Descripcion);
                                }

                                FormatoDocumentoObra.Celda(tabla.Cell()).AlignRight().Text(TextoCantidad(item.Cantidad, item.Unidad));
                                FormatoDocumentoObra.Celda(tabla.Cell()).AlignRight().Text(FormatoDocumentoObra.Moneda(item.Precio));
                                FormatoDocumentoObra.Celda(tabla.Cell()).AlignRight().Text(FormatoDocumentoObra.Moneda(item.Subtotal));
                            }
                        });
                        if (grupos.Count > 1)
                        {
                            FormatoDocumentoObra.ValorTotal(columna, grupo.Sum(item => item.Subtotal));
                        }
                    }

                    FormatoDocumentoObra.ValorTotal(columna, informe.Total);
                });
                FormatoDocumentoObra.Pie(pagina);
            });
        }).GeneratePdf();
    }

    private static void DefinirDetalle(TableDescriptor tabla)
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
    }

    private static void DefinirAcumulado(TableDescriptor tabla)
    {
        tabla.ColumnsDefinition(columnas =>
        {
            columnas.RelativeColumn(3.2f);
            columnas.RelativeColumn(1.1f);
            columnas.RelativeColumn(1.3f);
            columnas.RelativeColumn(1.4f);
        });
        tabla.Header(encabezado =>
        {
            FormatoDocumentoObra.Encabezado(encabezado.Cell(), "Concepto");
            FormatoDocumentoObra.Encabezado(encabezado.Cell(), "Cantidad");
            FormatoDocumentoObra.Encabezado(encabezado.Cell(), "Precio");
            FormatoDocumentoObra.Encabezado(encabezado.Cell(), "Total");
        });
    }

    private static void DefinirDetallado(TableDescriptor tabla)
    {
        tabla.ColumnsDefinition(columnas =>
        {
            columnas.RelativeColumn(0.9f);
            columnas.RelativeColumn(2.2f);
            columnas.RelativeColumn(2.2f);
            columnas.RelativeColumn(1.1f);
            columnas.RelativeColumn(1.2f);
            columnas.RelativeColumn(1.3f);
        });
        tabla.Header(encabezado =>
        {
            FormatoDocumentoObra.Encabezado(encabezado.Cell(), "Factura");
            FormatoDocumentoObra.Encabezado(encabezado.Cell(), "Concepto");
            FormatoDocumentoObra.Encabezado(encabezado.Cell(), "Descripción");
            FormatoDocumentoObra.Encabezado(encabezado.Cell(), "Cantidad");
            FormatoDocumentoObra.Encabezado(encabezado.Cell(), "Precio");
            FormatoDocumentoObra.Encabezado(encabezado.Cell(), "Total");
        });
    }

    private static string TextoCantidad(decimal cantidad, string unidad)
    {
        var texto = FormatoDocumentoObra.Cantidad(cantidad);
        return string.IsNullOrWhiteSpace(unidad) ? texto : $"{texto}\n{unidad}";
    }
}
