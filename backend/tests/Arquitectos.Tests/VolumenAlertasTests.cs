using System.Diagnostics;
using Arquitectos.Application.Dashboard;

namespace Arquitectos.Tests;

public sealed class VolumenAlertasTests
{
    [Fact]
    public void A1_y_A3_con_el_volumen_de_la_semilla_por_diez_caben_en_un_segundo_y_medio()
    {
        var lineas = Lineas(actas: 1_520, lineasPorActa: 5);
        var opciones = new OpcionesDashboard();
        var reloj = Stopwatch.StartNew();
        var precios = ReglasAlertas.PreciosSobreReferencia(lineas, lineas, opciones);
        var dobles = ReglasAlertas.DoblesCobros(lineas);
        reloj.Stop();

        Assert.NotNull(precios);
        Assert.NotNull(dobles);
        Assert.True(reloj.Elapsed < TimeSpan.FromMilliseconds(1_500), $"Tardó {reloj.Elapsed.TotalMilliseconds:0} ms");
    }

    private static List<LineaParaAlerta> Lineas(int actas, int lineasPorActa)
    {
        var lineas = new List<LineaParaAlerta>(actas * lineasPorActa);
        var inicio = new DateOnly(2025, 1, 1);
        var id = 1;
        for (var acta = 1; acta <= actas; acta++)
        {
            var fin = inicio.AddDays((acta % 700) * 1);
            var desde = fin.AddDays(-13);
            for (var linea = 0; linea < lineasPorActa; linea++)
            {
                var concepto = (acta + linea) % 20 + 1;
                var obra = acta % 10 + 1;
                var contratista = acta % 5 + 1;
                lineas.Add(new LineaParaAlerta(
                    id++,
                    acta,
                    desde,
                    fin,
                    contratista,
                    "Contratista",
                    obra,
                    "Obra",
                    concepto,
                    "Concepto",
                    1,
                    linea % 3 == 0 ? linea % 30 + 1 : null,
                    linea % 3 == 0 ? "Material" : null,
                    2,
                    100 + (acta % 17),
                    200));
            }
        }

        return lineas;
    }
}
