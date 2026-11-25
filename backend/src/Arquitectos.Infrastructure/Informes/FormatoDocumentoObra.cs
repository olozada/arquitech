using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Arquitectos.Infrastructure.Informes;

public static class FormatoDocumentoObra
{
    public const string Membrete = "Rancho La Soledad";

    public static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-CO");

    public static void Preparar(PageDescriptor pagina)
    {
        pagina.Size(PageSizes.Letter);
        pagina.Margin(32);
        pagina.DefaultTextStyle(estilo => estilo.FontSize(10).LineHeight(1.15f));
    }

    public static void Pie(PageDescriptor pagina) =>
        pagina.Footer().AlignRight().Text(texto =>
        {
            texto.Span("Página: ");
            texto.CurrentPageNumber();
            texto.Span("/");
            texto.TotalPages();
        });

    public static void MembreteYTitulo(ColumnDescriptor columna, string titulo)
    {
        columna.Item().AlignCenter().Text(Membrete).FontSize(16).SemiBold();
        columna.Item().AlignCenter().Text(titulo).FontSize(13).SemiBold();
    }

    public static void Casa(ColumnDescriptor columna, string casa) =>
        columna.Item().PaddingTop(8).Text($"Casa: {casa}").SemiBold();

    public static void ValorTotal(ColumnDescriptor columna, decimal total) =>
        columna.Item().AlignRight().PaddingTop(4).Text($"Valor total: {Moneda(total)}").SemiBold();

    public static void CierreArmenia(ColumnDescriptor columna, DateOnly fecha) =>
        columna.Item().PaddingTop(14).Text($"Para constancia se firma en Armenia el {Fecha(fecha)}.");

    public static void Encabezado(IContainer contenedor, string titulo) =>
        contenedor.Background(Colors.Grey.Lighten3).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3)
            .Text(titulo).SemiBold();

    public static IContainer Celda(IContainer contenedor) =>
        contenedor.Border(0.5f).BorderColor(Colors.Grey.Lighten2).Padding(3);

    public static string Moneda(decimal valor) => valor.ToString("$ #,##0", Cultura);

    public static string MonedaConCentavos(decimal valor) => valor.ToString("$ #,##0.00", Cultura);

    public static string Cantidad(decimal valor) => valor.ToString("0.####", Cultura);

    public static string Fecha(DateOnly fecha) => fecha.ToString("dd/MM/yyyy", Cultura);
}
