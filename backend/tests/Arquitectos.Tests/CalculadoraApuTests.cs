using Arquitectos.Application.Presupuestos;
using Arquitectos.Infrastructure.Presupuestos;

namespace Arquitectos.Tests;

public class CalculadoraApuTests
{
    [Fact]
    public void Los_cuatro_modos_de_linea_aplican_la_formula()
    {
        var apu = Unico("Material", 1, [
            Linea(ModoCalculo.Desperdicio, 2, 0.05m, 100),
            Linea(ModoCalculo.Rendimiento, 1, 0.5m, 80),
            Linea(ModoCalculo.Divisor, 60, 6, 12)
        ]);

        var total = CalculadoraApu.Calcular(1, apu).Total;

        Assert.Equal(2 * 100 * 1.05m + 1 * 80 * 0.5m + 60m * 12 / 6, total);
    }

    [Fact]
    public void El_divisor_del_grupo_y_el_general_dividen_el_costo()
    {
        var mapa = new Dictionary<int, ApuCalculo>
        {
            [1] = new(1, 6, [new GrupoCalculo("Material", 2, [Linea(ModoCalculo.Desperdicio, 1, 0, 120)])])
        };

        Assert.Equal(10m, CalculadoraApu.Calcular(1, mapa).Total);
    }

    [Fact]
    public void La_herramienta_menor_es_un_porcentaje_de_la_mano_de_obra()
    {
        var mapa = new Dictionary<int, ApuCalculo>
        {
            [1] = new(1, 1, [new GrupoCalculo("ManoObra", 1, [
                Linea(ModoCalculo.Desperdicio, 1, 0, 1000),
                new LineaCalculo(ModoCalculo.Rendimiento, 1, 0.10m, 0, null, true)
            ])])
        };

        var resultado = CalculadoraApu.Calcular(1, mapa);

        Assert.Equal(1100m, resultado.Total);
        Assert.Equal(1100m, resultado.Grupos["ManoObra"]);
    }

    [Fact]
    public void Un_subanalisis_de_dos_niveles_usa_el_costo_del_interno()
    {
        var mapa = new Dictionary<int, ApuCalculo>
        {
            [2] = new(2, 1, [new GrupoCalculo("Material", 1, [Linea(ModoCalculo.Desperdicio, 1, 0, 50)])]),
            [1] = new(1, 1, [new GrupoCalculo("Material", 1, [
                new LineaCalculo(ModoCalculo.Desperdicio, 2, 0, 0, 2, false)
            ])])
        };

        Assert.Equal(100m, CalculadoraApu.Calcular(1, mapa).Total);
    }

    [Fact]
    public void Un_ciclo_entre_analisis_se_rechaza()
    {
        var mapa = new Dictionary<int, ApuCalculo>
        {
            [1] = new(1, 1, [new GrupoCalculo("Material", 1, [new LineaCalculo(ModoCalculo.Desperdicio, 1, 0, 0, 2, false)])]),
            [2] = new(2, 1, [new GrupoCalculo("Material", 1, [new LineaCalculo(ModoCalculo.Desperdicio, 1, 0, 0, 1, false)])])
        };

        Assert.Throws<CicloApuException>(() => CalculadoraApu.Calcular(1, mapa));
    }

    [Fact]
    public void El_apu_concreto_zapatas_reproduce_el_excel()
    {
        var ruta = Path.Combine(AppContext.BaseDirectory, "Fixtures", "presupuesto_casa_murillo.json");
        var modelo = LectorModelo.LeerJson(File.ReadAllText(ruta));
        var total = CalculadoraApu.Calcular(17, LectorModelo.Mapa(modelo)).Total;

        Assert.InRange(Math.Abs(total - 803590.4927212801m), 0, 1);
    }

    [Fact]
    public void La_cuadrilla_suma_jornales_con_el_factor_prestacional()
    {
        var factor = CalculadoraApu.FactorPrestacional([0.72m], false, 296);
        var costo = CalculadoraApu.CostoDiaCuadrilla([(1_200_000, 1), (900_000, 2)], 30, factor);

        Assert.Equal(0.72m, factor);
        Assert.Equal((1_200_000m / 30 * 1.72m) + (900_000m / 30 * 1.72m * 2), costo);
    }

    private static Dictionary<int, ApuCalculo> Unico(string tipo, decimal divisor, IReadOnlyList<LineaCalculo> lineas) =>
        new() { [1] = new ApuCalculo(1, 1, [new GrupoCalculo(tipo, divisor, lineas)]) };

    private static LineaCalculo Linea(ModoCalculo modo, decimal cantidad, decimal factor, decimal precio) =>
        new(modo, cantidad, factor, precio, null, false);
}
