using Arquitectos.Application.Dashboard;
using ClosedXML.Excel;

namespace Arquitectos.Infrastructure.Dashboard;

public static class GeneradorExcelDashboard
{
    public static byte[] Generar(DashboardDto tablero, IReadOnlyList<AlertaDto> alertas)
    {
        using var libro = new XLWorkbook();
        HojaKpis(libro, tablero);
        HojaTendencia(libro, tablero);
        HojaRanking(libro, "Construcciones", tablero.TopConstrucciones);
        HojaRanking(libro, "Contratistas", tablero.Contratistas);
        HojaRanking(libro, "Conceptos", tablero.Conceptos);
        HojaPersonas(libro, tablero);
        HojaAlertas(libro, alertas);
        HojaCalidad(libro, tablero.Calidad);

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    private static void HojaKpis(XLWorkbook libro, DashboardDto tablero)
    {
        var hoja = libro.Worksheets.Add("KPIs");
        EscribirTitulos(hoja, ["Código", "Título", "Valor", "Valor anterior", "Variación %", "Complemento"]);
        var fila = 2;
        foreach (var kpi in tablero.Kpis)
        {
            hoja.Cell(fila, 1).Value = kpi.Codigo;
            hoja.Cell(fila, 2).Value = kpi.Titulo;
            hoja.Cell(fila, 3).Value = (double)kpi.Valor;
            hoja.Cell(fila, 4).Value = (double)(kpi.ValorAnterior ?? 0);
            if (kpi.VariacionPct is decimal variacion)
            {
                hoja.Cell(fila, 5).Value = (double)variacion;
            }

            if (kpi.Complemento is decimal complemento)
            {
                hoja.Cell(fila, 6).Value = (double)complemento;
            }

            fila++;
        }

        hoja.Columns().AdjustToContents();
    }

    private static void HojaTendencia(XLWorkbook libro, DashboardDto tablero)
    {
        var hoja = libro.Worksheets.Add("Tendencia");
        EscribirTitulos(hoja, ["Año", "Mes", "Valor", "Actas", "Valor año anterior"]);
        var fila = 2;
        foreach (var punto in tablero.Tendencia)
        {
            hoja.Cell(fila, 1).Value = punto.Anio;
            hoja.Cell(fila, 2).Value = punto.Mes;
            hoja.Cell(fila, 3).Value = (double)punto.Valor;
            hoja.Cell(fila, 3).Style.NumberFormat.Format = "$ #,##0";
            hoja.Cell(fila, 4).Value = punto.Actas;
            hoja.Cell(fila, 5).Value = (double)(punto.ValorAnioAnterior ?? 0);
            hoja.Cell(fila, 5).Style.NumberFormat.Format = "$ #,##0";
            fila++;
        }

        hoja.Columns().AdjustToContents();
    }

    private static void HojaRanking(XLWorkbook libro, string nombre, IReadOnlyList<RankingDto> filas)
    {
        var hoja = libro.Worksheets.Add(nombre);
        EscribirTitulos(hoja, ["Id", "Nombre", "Valor", "Actas", "Participación %", "Acumulada %"]);
        var fila = 2;
        foreach (var item in filas)
        {
            hoja.Cell(fila, 1).Value = item.Id;
            hoja.Cell(fila, 2).Value = item.Nombre;
            hoja.Cell(fila, 3).Value = (double)item.Valor;
            hoja.Cell(fila, 3).Style.NumberFormat.Format = "$ #,##0";
            hoja.Cell(fila, 4).Value = item.Actas;
            hoja.Cell(fila, 5).Value = (double)item.Participacion;
            hoja.Cell(fila, 6).Value = (double)item.ParticipacionAcumulada;
            fila++;
        }

        hoja.Columns().AdjustToContents();
    }

    private static void HojaPersonas(XLWorkbook libro, DashboardDto tablero)
    {
        var hoja = libro.Worksheets.Add("Personas");
        EscribirTitulos(hoja, ["Rol", "Id", "Nombre", "Valor", "Actas", "Participación %"]);
        var fila = 2;
        fila = EscribirPersonas(hoja, fila, "Arquitecto", tablero.Arquitectos);
        EscribirPersonas(hoja, fila, "Revisor", tablero.Revisores);
        hoja.Columns().AdjustToContents();
    }

    private static int EscribirPersonas(IXLWorksheet hoja, int fila, string rol, IReadOnlyList<RankingDto> personas)
    {
        foreach (var persona in personas)
        {
            hoja.Cell(fila, 1).Value = rol;
            hoja.Cell(fila, 2).Value = persona.Id;
            hoja.Cell(fila, 3).Value = persona.Nombre;
            hoja.Cell(fila, 4).Value = (double)persona.Valor;
            hoja.Cell(fila, 4).Style.NumberFormat.Format = "$ #,##0";
            hoja.Cell(fila, 5).Value = persona.Actas;
            hoja.Cell(fila, 6).Value = (double)persona.Participacion;
            fila++;
        }

        return fila;
    }

    private static void HojaAlertas(XLWorkbook libro, IReadOnlyList<AlertaDto> alertas)
    {
        var hoja = libro.Worksheets.Add("Alertas");
        EscribirTitulos(hoja, ["Tipo", "Severidad", "Título", "Detalle", "N° Acta", "Construcción", "Impacto"]);
        var fila = 2;
        foreach (var alerta in alertas)
        {
            hoja.Cell(fila, 1).Value = alerta.Tipo;
            hoja.Cell(fila, 2).Value = alerta.Severidad;
            hoja.Cell(fila, 3).Value = alerta.Titulo;
            hoja.Cell(fila, 4).Value = alerta.Detalle;
            if (alerta.NumeroActa is int numero)
            {
                hoja.Cell(fila, 5).Value = numero;
            }

            hoja.Cell(fila, 6).Value = alerta.Construccion ?? "";
            if (alerta.Impacto is decimal impacto)
            {
                hoja.Cell(fila, 7).Value = (double)impacto;
                hoja.Cell(fila, 7).Style.NumberFormat.Format = "$ #,##0";
            }

            fila++;
        }

        hoja.Columns().AdjustToContents();
    }

    private static void HojaCalidad(XLWorkbook libro, CalidadDatosDto calidad)
    {
        var hoja = libro.Worksheets.Add("Calidad");
        EscribirTitulos(hoja, ["Indicador", "Valor"]);
        hoja.Cell(2, 1).Value = "% materiales con precio de proveedor";
        hoja.Cell(2, 2).Value = (double)calidad.PctMaterialesConPrecio;
        hoja.Cell(3, 1).Value = "Materiales con un solo proveedor";
        hoja.Cell(3, 2).Value = calidad.MaterialesUnProveedor;
        hoja.Cell(4, 1).Value = "Líneas sin material";
        hoja.Cell(4, 2).Value = calidad.LineasSinMaterial;
        hoja.Cell(5, 1).Value = "Actas sin observación";
        hoja.Cell(5, 2).Value = calidad.ActasSinObservacion;
        hoja.Columns().AdjustToContents();
    }

    private static void EscribirTitulos(IXLWorksheet hoja, IReadOnlyList<string> titulos)
    {
        for (var columna = 0; columna < titulos.Count; columna++)
        {
            hoja.Cell(1, columna + 1).Value = titulos[columna];
            hoja.Cell(1, columna + 1).Style.Font.Bold = true;
        }
    }
}
