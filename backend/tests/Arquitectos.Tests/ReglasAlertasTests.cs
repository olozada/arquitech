using Arquitectos.Application.Dashboard;
using Arquitectos.Infrastructure.Dashboard;

namespace Arquitectos.Tests;

public sealed class ReglasAlertasTests
{
    private static readonly OpcionesDashboard Opciones = new();

    [Fact]
    public void A1_alerta_cuando_el_precio_supera_el_umbral()
    {
        var historico = Referencias(100m, 4, new DateOnly(2026, 1, 1));
        var linea = Linea(99, 10, new DateOnly(2026, 3, 1), 120m, 2);

        var alertas = ReglasAlertas.PreciosSobreReferencia([linea], historico, Opciones);

        var alerta = Assert.Single(alertas);
        Assert.Equal("A1", alerta.Tipo);
        Assert.Equal("Media", alerta.Severidad);
        Assert.Equal(40m, alerta.Impacto);
    }

    [Fact]
    public void A1_es_alta_cuando_duplica_el_umbral()
    {
        var historico = Referencias(100m, 3, new DateOnly(2026, 1, 1));
        var linea = Linea(99, 10, new DateOnly(2026, 3, 1), 140m, 1);

        var alerta = Assert.Single(ReglasAlertas.PreciosSobreReferencia([linea], historico, Opciones));

        Assert.Equal("Alta", alerta.Severidad);
    }

    [Fact]
    public void A1_no_alerta_en_el_umbral_exacto_ni_con_pocas_observaciones()
    {
        var suficientes = Referencias(100m, 3, new DateOnly(2026, 1, 1));
        var enElUmbral = Linea(99, 10, new DateOnly(2026, 3, 1), 115m, 1);
        var pocas = Referencias(100m, 2, new DateOnly(2026, 1, 1));
        var inflada = Linea(99, 10, new DateOnly(2026, 3, 1), 200m, 1);

        Assert.Empty(ReglasAlertas.PreciosSobreReferencia([enElUmbral], suficientes, Opciones));
        Assert.Empty(ReglasAlertas.PreciosSobreReferencia([inflada], pocas, Opciones));
    }

    [Fact]
    public void A2_alerta_solo_si_el_precio_supera_la_lista()
    {
        var lista = new PrecioProveedor(7, 1000m, "Proveedor barato");
        var cara = Linea(1, 10, new DateOnly(2026, 3, 1), 1500m, 2, materialId: 7);
        var igual = Linea(2, 10, new DateOnly(2026, 3, 1), 1000m, 2, materialId: 7);

        var alerta = Assert.Single(ReglasAlertas.PreciosSobreLista([cara, igual], [lista]));

        Assert.Equal("A2", alerta.Tipo);
        Assert.Equal("Media", alerta.Severidad);
        Assert.Equal(1000m, alerta.Impacto);
        Assert.Contains("Proveedor barato", alerta.Titulo);
    }

    [Fact]
    public void A3_alerta_cuando_los_periodos_se_tocan_un_dia()
    {
        var primera = Linea(1, 4, new DateOnly(2026, 3, 10), 100m, 2, inicio: new DateOnly(2026, 3, 1));
        var segunda = Linea(2, 4, new DateOnly(2026, 3, 20), 80m, 1, inicio: new DateOnly(2026, 3, 10));
        var ajena = Linea(3, 8, new DateOnly(2026, 3, 20), 80m, 1, inicio: new DateOnly(2026, 3, 11), contratistaId: 9);

        var alerta = Assert.Single(ReglasAlertas.DoblesCobros([primera, segunda, ajena]));

        Assert.Equal("A3", alerta.Tipo);
        Assert.Equal("Alta", alerta.Severidad);
        Assert.Equal(80m, alerta.Impacto);
    }

    [Fact]
    public void A3_no_alerta_si_los_periodos_no_se_cruzan()
    {
        var primera = Linea(1, 4, new DateOnly(2026, 3, 10), 100m, 1, inicio: new DateOnly(2026, 3, 1));
        var segunda = Linea(2, 4, new DateOnly(2026, 3, 20), 80m, 1, inicio: new DateOnly(2026, 3, 11));

        Assert.Empty(ReglasAlertas.DoblesCobros([primera, segunda]));
    }

    [Fact]
    public void A4_alerta_sin_actas_o_con_la_ultima_anterior_al_limite()
    {
        var hoy = new DateOnly(2026, 4, 30);
        var obras = new ObraActividad[]
        {
            new(1, "Quieta", hoy.AddDays(-31)),
            new(2, "Nunca", null),
            new(3, "Al día", hoy.AddDays(-30))
        };

        var alertas = ReglasAlertas.ObrasSinActividad(obras, hoy, Opciones);

        Assert.Equal(2, alertas.Count);
        Assert.DoesNotContain(alertas, alerta => alerta.ConstruccionId == 3);
        Assert.All(alertas, alerta => Assert.Equal("Baja", alerta.Severidad));
    }

    [Fact]
    public void A5_alerta_solo_despues_del_limite_de_dias()
    {
        var tarde = new ActaRegistro(8, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 17), "Oficina", 1, "Obra");
        var justo = new ActaRegistro(9, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 16), "Oficina", 1, "Obra");

        var alerta = Assert.Single(ReglasAlertas.ActasTardias([tarde, justo], Opciones));

        Assert.Equal("A5", alerta.Tipo);
        Assert.Equal(8, alerta.NumeroActa);
        Assert.Equal("Baja", alerta.Severidad);
    }

    private static List<LineaParaAlerta> Referencias(decimal precio, int cantidad, DateOnly inicio)
    {
        var lineas = new List<LineaParaAlerta>();
        for (var indice = 0; indice < cantidad; indice++)
        {
            lineas.Add(Linea(indice + 1, 3, inicio.AddDays(indice), precio, 1));
        }

        return lineas;
    }

    private static LineaParaAlerta Linea(
        int numero,
        int construccionId,
        DateOnly fin,
        decimal precio,
        decimal cantidad,
        DateOnly? inicio = null,
        int contratistaId = 5,
        int? materialId = null) =>
        new(
            numero,
            numero,
            inicio ?? fin.AddDays(-5),
            fin,
            contratistaId,
            "Contratista",
            construccionId,
            "Obra",
            2,
            "Concreto",
            1,
            materialId,
            materialId is null ? null : "Material",
            cantidad,
            precio,
            precio * cantidad);
}
