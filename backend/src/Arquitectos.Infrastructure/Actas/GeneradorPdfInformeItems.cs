using Arquitectos.Application.Actas;
using Arquitectos.Infrastructure.Informes;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Arquitectos.Infrastructure.Actas;

public static class GeneradorPdfInformeItems
{
    public static byte[] Generar(InformeItemsDto informe)
    {
        var titulo = informe.Agrupado && !informe.RomperPorActa
            ? "Valor de obra acumulado"
            : "Valor de obra detallado";
        var mostrarActa = !informe.Agrupado || informe.RomperPorActa;
        var mostrarDescripcion = !informe.Agrupado;
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
                    foreach (var linea in informe.Encabezado ?? [])
                    {
                        columna.Item().Text(linea);
                    }

                    foreach (var grupo in grupos)
                    {
                        FormatoDocumentoObra.Casa(columna, grupo.Key);
                        var filas = informe.RomperPorActa
                            ? grupo.OrderBy(item => item.NumeroActa).ToList()
                            : grupo.ToList();
                        Tabla(columna, filas, mostrarActa, mostrarDescripcion, informe.RomperPorActa);
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

    private static void Tabla(
        ColumnDescriptor columna,
        IReadOnlyList<ItemInformeDto> items,
        bool mostrarActa,
        bool mostrarDescripcion,
        bool romperPorActa)
    {
        columna.Item().Table(tabla =>
        {
            tabla.ColumnsDefinition(columnas =>
            {
                if (mostrarActa)
                {
                    columnas.RelativeColumn(0.8f);
                }

                columnas.RelativeColumn(2.4f);
                if (mostrarDescripcion)
                {
                    columnas.RelativeColumn(2.2f);
                }

                columnas.RelativeColumn(1.1f);
                columnas.RelativeColumn(1.2f);
                columnas.RelativeColumn(1.3f);
            });
            tabla.Header(encabezado =>
            {
                if (mostrarActa)
                {
                    FormatoDocumentoObra.Encabezado(encabezado.Cell(), "Acta");
                }

                FormatoDocumentoObra.Encabezado(encabezado.Cell(), "Concepto");
                if (mostrarDescripcion)
                {
                    FormatoDocumentoObra.Encabezado(encabezado.Cell(), "Descripción");
                }

                FormatoDocumentoObra.Encabezado(encabezado.Cell(), "Cantidad");
                FormatoDocumentoObra.Encabezado(encabezado.Cell(), "Precio");
                FormatoDocumentoObra.Encabezado(encabezado.Cell(), "Total");
            });

            for (var indice = 0; indice < items.Count; indice++)
            {
                var item = items[indice];
                if (mostrarActa)
                {
                    FormatoDocumentoObra.Celda(tabla.Cell()).Text(item.NumeroActa?.ToString(FormatoDocumentoObra.Cultura) ?? "");
                }

                FormatoDocumentoObra.Celda(tabla.Cell()).Text(TextoConcepto(item));
                if (mostrarDescripcion)
                {
                    FormatoDocumentoObra.Celda(tabla.Cell()).Text(item.Descripcion);
                }

                FormatoDocumentoObra.Celda(tabla.Cell()).AlignRight().Text(TextoCantidad(item));
                FormatoDocumentoObra.Celda(tabla.Cell()).AlignRight().Text(FormatoDocumentoObra.Moneda(item.Precio));
                FormatoDocumentoObra.Celda(tabla.Cell()).AlignRight().Text(FormatoDocumentoObra.Moneda(item.Subtotal));

                var siguiente = indice + 1 < items.Count ? items[indice + 1] : null;
                if (romperPorActa && siguiente?.NumeroActa != item.NumeroActa)
                {
                    var subtotal = items.Where(fila => fila.NumeroActa == item.NumeroActa).Sum(fila => fila.Subtotal);
                    Corte(tabla, mostrarDescripcion, item.NumeroActa, subtotal);
                }
            }
        });
    }

    private static void Corte(TableDescriptor tabla, bool mostrarDescripcion, int? numeroActa, decimal subtotal)
    {
        var titulo = numeroActa is int numero
            ? $"Total acta {numero.ToString(FormatoDocumentoObra.Cultura)}"
            : "Total acta";
        var previas = (uint)(1 + 1 + (mostrarDescripcion ? 1 : 0) + 2);
        tabla.Cell().ColumnSpan(previas).Border(0.5f).Padding(3).Text(titulo).SemiBold();
        tabla.Cell().Border(0.5f).Padding(3).AlignRight().Text(FormatoDocumentoObra.Moneda(subtotal)).SemiBold();
    }

    private static string TextoConcepto(ItemInformeDto item)
    {
        var texto = string.IsNullOrWhiteSpace(item.Material) ? item.Concepto : $"{item.Concepto} ({item.Material})";
        return item.Aprobado ? texto : $"{texto} (rechazado)";
    }

    private static string TextoCantidad(ItemInformeDto item)
    {
        var texto = FormatoDocumentoObra.Cantidad(item.Cantidad);
        return string.IsNullOrWhiteSpace(item.Unidad) ? texto : $"{texto}\n{item.Unidad}";
    }
}
