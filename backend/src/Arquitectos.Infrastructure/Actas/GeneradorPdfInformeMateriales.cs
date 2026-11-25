using System.Globalization;
using Arquitectos.Application.Actas;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Arquitectos.Infrastructure.Actas;

public static class GeneradorPdfInformeMateriales
{
    private static readonly CultureInfo Cultura = new("es-CO");

    public static byte[] Generar(
        InformeMaterialesDto informe,
        bool subtotales = false,
        string titulo = "Informe por materiales",
        string rotuloColumna = "Material")
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
                    columna.Item().Text(titulo).FontSize(16).SemiBold();
                    foreach (var linea in informe.Encabezado ?? [])
                    {
                        columna.Item().Text(linea).FontSize(11).SemiBold();
                    }

                    if (subtotales)
                    {
                        var nota = rotuloColumna == "Concepto" ? "Con subtotales por concepto" : "Con subtotales por material";
                        columna.Item().Text(nota).FontSize(11).SemiBold();
                    }

                    var detallado = !informe.Agrupado;
                    var casas = CortesMaterial.Casas(informe.Items);
                    var varias = casas.Exists(casa => !string.IsNullOrWhiteSpace(casa.Nombre));
                    var primera = true;
                    foreach (var casa in casas)
                    {
                        if (varias && !primera)
                        {
                            columna.Item().PageBreak();
                        }

                        primera = false;
                        var totalCasa = casa.Items.Sum(item => item.Subtotal);
                        columna.Item().Element(contenedor => Bloques(
                            contenedor,
                            casa.Items,
                            detallado,
                            subtotales,
                            varias ? totalCasa : informe.Total,
                            varias,
                            varias ? casa.Nombre : null,
                            rotuloColumna));
                    }

                    if (varias)
                    {
                        columna.Item().AlignRight().Text($"Total: {Moneda(informe.Total)}").FontSize(12).SemiBold();
                    }
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

    private const int MaximoFilasJuntas = 14;

    private static void Bloques(
        IContainer contenedor,
        IReadOnlyList<ItemMaterialInformeDto> items,
        bool detallado,
        bool subtotales,
        decimal total,
        bool porConstruccion,
        string? construccion,
        string rotulo)
    {
        var grupos = Partir(CortesMaterial.Armar(items, subtotales && detallado, incluirUnicos: true));
        contenedor.Column(columna =>
        {
            columna.Spacing(6);
            for (var indice = 0; indice < grupos.Count; indice++)
            {
                var primero = indice == 0;
                var ultimo = indice == grupos.Count - 1;
                columna.Item().Element(elemento => EmitirGrupo(
                    elemento,
                    grupos[indice],
                    detallado,
                    rotulo,
                    primero ? construccion : null,
                    ultimo,
                    total,
                    porConstruccion));
            }
        });
    }

    private static void EmitirGrupo(
        IContainer contenedor,
        IReadOnlyList<CortesMaterial.Corte> grupo,
        bool detallado,
        string rotulo,
        string? construccion,
        bool ultimo,
        decimal total,
        bool porConstruccion)
    {
        var identidad = grupo.FirstOrDefault(corte => corte.Encabezado);
        var lineas = grupo.Where(corte => corte.Linea is not null).ToList();
        CortesMaterial.Corte? subtotal = grupo.Any(corte => !corte.Encabezado && corte.Linea is null)
            ? grupo.Last(corte => !corte.Encabezado && corte.Linea is null)
            : null;
        var filasCierre = (subtotal is null ? 0 : 1) + (ultimo ? 1 : 0);
        var cabeJunto = lineas.Count + filasCierre <= MaximoFilasJuntas;

        contenedor.Column(columna =>
        {
            columna.Spacing(0);
            if (cabeJunto)
            {
                columna.Item().ShowEntire().Table(tabla => Pintar(
                    tabla, detallado, rotulo, identidad, construccion, lineas, subtotal, ultimo, total, porConstruccion));
                return;
            }

            var cuerpo = lineas.Count > 1 ? lineas.Take(lineas.Count - 1).ToList() : [];
            var cola = lineas.Count > 1 ? lineas.TakeLast(1).ToList() : lineas;
            if (cuerpo.Count > 0)
            {
                columna.Item().Table(tabla => Pintar(
                    tabla, detallado, rotulo, identidad, construccion, cuerpo, null, false, total, porConstruccion));
            }

            columna.Item().ShowEntire().Table(tabla => Pintar(
                tabla, detallado, rotulo, identidad, construccion, cola, subtotal, ultimo, total, porConstruccion));
        });
    }

    private static void Pintar(
        TableDescriptor tabla,
        bool detallado,
        string rotulo,
        CortesMaterial.Corte identidad,
        string? construccion,
        IReadOnlyList<CortesMaterial.Corte> lineas,
        CortesMaterial.Corte? subtotal,
        bool conTotal,
        decimal total,
        bool porConstruccion)
    {
        tabla.ColumnsDefinition(columnas => DefinirColumnas(columnas, detallado));
        tabla.Header(encabezado =>
        {
            if (!string.IsNullOrWhiteSpace(construccion))
            {
                encabezado.Cell().ColumnSpan(Columnas(detallado))
                    .Background("#eceae4").Border(0.5f).BorderColor("#e4e4e0").Padding(4)
                    .Text($"Construcción: {construccion}").FontSize(12).SemiBold();
            }

            if (identidad.Encabezado)
            {
                FilaIdentidad(encabezado, identidad, detallado);
            }
            else
            {
                FilaTitulos(encabezado, detallado, rotulo);
            }
        });

        foreach (var corte in lineas)
        {
            if (corte.Linea is not ItemMaterialInformeDto item)
            {
                continue;
            }

            Celda(tabla.Cell()).Text(corte.OcultarIdentidad ? "" : item.Codigo);
            Celda(tabla.Cell()).Text(corte.OcultarIdentidad ? "" : item.Material);
            Celda(tabla.Cell()).Text(corte.OcultarIdentidad ? "" : item.Unidad);
            if (detallado)
            {
                Celda(tabla.Cell()).AlignRight().Text(item.NumeroActa?.ToString(Cultura) ?? "");
            }

            Celda(tabla.Cell()).AlignRight().Text(item.Cantidad.ToString("0.####", Cultura));
            Celda(tabla.Cell()).AlignRight().Text(Moneda(item.Precio));
            Celda(tabla.Cell()).AlignRight().Text(Moneda(item.Subtotal));
        }

        if (subtotal is CortesMaterial.Corte cierre)
        {
            CeldaSubtotal(tabla.Cell()).Text("");
            CeldaSubtotal(tabla.Cell()).Text("Subtotal").SemiBold();
            CeldaSubtotal(tabla.Cell()).Text("");
            if (detallado)
            {
                CeldaSubtotal(tabla.Cell()).Text("");
            }

            CeldaSubtotal(tabla.Cell()).AlignRight().Text(cierre.Cantidad.ToString("0.####", Cultura)).SemiBold();
            CeldaSubtotal(tabla.Cell()).Text("");
            CeldaSubtotal(tabla.Cell()).AlignRight().Text(Moneda(cierre.Valor)).SemiBold();
        }

        if (!conTotal)
        {
            return;
        }

        var titulo = porConstruccion ? "Total construcción" : "TOTAL";
        tabla.Cell().ColumnSpan(Columnas(detallado) - 1).Background("#e8f8f5").Border(1).BorderColor("#1abb9c").Padding(4)
            .AlignMiddle().Text(titulo).FontSize(12).FontColor("#0f766e").Bold();
        tabla.Cell().Background("#e8f8f5").Border(1).BorderColor("#1abb9c").Padding(4)
            .AlignRight().AlignMiddle().Text(Moneda(total)).FontSize(13).FontColor("#0f766e").Bold();
    }

    private static List<List<CortesMaterial.Corte>> Partir(List<CortesMaterial.Corte> cortes)
    {
        var grupos = new List<List<CortesMaterial.Corte>>();
        List<CortesMaterial.Corte>? actual = null;
        foreach (var corte in cortes)
        {
            if (corte.Encabezado || actual is null)
            {
                actual = [];
                grupos.Add(actual);
            }

            actual.Add(corte);
            if (!corte.Encabezado && corte.Linea is null)
            {
                actual = null;
            }
        }

        return grupos;
    }

    private static void DefinirColumnas(TableColumnsDefinitionDescriptor columnas, bool detallado)
    {
        columnas.ConstantColumn(70);
        columnas.RelativeColumn(3);
        columnas.ConstantColumn(40);
        if (detallado)
        {
            columnas.ConstantColumn(48);
        }

        columnas.ConstantColumn(70);
        columnas.ConstantColumn(80);
        columnas.ConstantColumn(100);
    }

    private static void FilaTitulos(TableCellDescriptor encabezado, bool detallado, string rotulo)
    {
        Encabezado(encabezado.Cell(), "Código");
        Encabezado(encabezado.Cell(), rotulo);
        Encabezado(encabezado.Cell(), "UM");
        if (detallado)
        {
            Encabezado(encabezado.Cell(), "Acta");
        }

        Encabezado(encabezado.Cell(), "Cantidad");
        Encabezado(encabezado.Cell(), "Precio");
        Encabezado(encabezado.Cell(), "Subtotal");
    }

    private static void FilaIdentidad(TableCellDescriptor fila, CortesMaterial.Corte corte, bool detallado)
    {
        CeldaGrupo(fila.Cell()).Text(corte.Codigo).SemiBold();
        CeldaGrupo(fila.Cell()).Text(corte.Material).SemiBold();
        CeldaGrupo(fila.Cell()).Text(corte.Unidad).SemiBold();
        if (detallado)
        {
            Encabezado(fila.Cell(), "Acta");
        }

        Encabezado(fila.Cell(), "Cantidad");
        Encabezado(fila.Cell(), "Precio");
        Encabezado(fila.Cell(), "Subtotal");
    }

    private static uint Columnas(bool detallado) => detallado ? 7u : 6u;

    private static void Encabezado(IContainer contenedor, string titulo) =>
        contenedor.Background(Colors.Grey.Lighten3).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3)
            .Text(titulo).SemiBold();

    private static IContainer Celda(IContainer contenedor) =>
        contenedor.Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3);

    private static IContainer CeldaGrupo(IContainer contenedor) =>
        contenedor.Background("#f3f3f1").Border(0.5f).BorderColor("#e4e4e0").Padding(3);

    private static IContainer CeldaSubtotal(IContainer contenedor) =>
        contenedor.Background("#f6f1e4").Border(0.5f).BorderColor("#e4d7b8").Padding(3);

    private static string Moneda(decimal valor) => valor.ToString("$ #,##0", Cultura);
}
