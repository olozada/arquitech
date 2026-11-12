using Arquitectos.Application.Dashboard;
using Arquitectos.Infrastructure.Dashboard;
using Arquitectos.Infrastructure.Presupuestos;

namespace Arquitectos.Tests;

public class SeguimientoPresupuestoTests
{
    [Fact]
    public void La_proyeccion_ajusta_el_saldo_con_el_precio_de_las_actas()
    {
        var seguimiento = CalculadoraPresupuesto.Seguimiento(10, 100, 4, 480);

        Assert.Equal(0.4m, seguimiento.AvanceFisico);
        Assert.Equal(1000m, seguimiento.ValorPresupuestado);
        Assert.Equal(0.48m, seguimiento.AvanceFinanciero);
        Assert.Equal(520m, seguimiento.Saldo);
        Assert.Equal(120m, seguimiento.PrecioActas);
        Assert.Equal(480m + 6 * 120m, seguimiento.ProyeccionCierre);
    }

    [Fact]
    public void Sin_ejecucion_la_proyeccion_es_el_presupuesto()
    {
        var seguimiento = CalculadoraPresupuesto.Seguimiento(5, 20, 0, 0);

        Assert.Equal(100m, seguimiento.ProyeccionCierre);
        Assert.Equal(100m, seguimiento.Saldo);
    }

    [Fact]
    public void Los_indirectos_y_el_valor_por_metro_cuadrado_siguen_al_excel()
    {
        var sinAiu = CalculadoraPresupuesto.Resumir(1000, 700, 300, 0.05m, 0.07m, 0, 0, 0, 10);
        var conAiu = CalculadoraPresupuesto.Resumir(1000, 700, 300, 0.05m, 0.07m, 0.10m, 0.05m, 0.19m, 10);

        Assert.Equal(1120m, sinAiu.Total);
        Assert.Equal(112m, sinAiu.ValorPorM2);
        Assert.Equal(1120m + 100m + 50m + 9.5m, conAiu.Total);
    }

    [Fact]
    public void Las_mediciones_definen_la_cantidad()
    {
        var parcial = CalculadoraApu.ParcialMedicion(2, 3, null, 4);

        Assert.Equal(24m, parcial);
    }

    [Fact]
    public void Las_alertas_de_presupuesto_marcan_precio_cantidad_y_pagos_sueltos()
    {
        var sobrePrecio = ReglasAlertas.PrecioSobrePresupuesto(
            [new LineaContraPresupuesto(8, 1, "Obra", "1.01", 130, 100, 2, 10, 2)],
            0.10m);
        var dentro = ReglasAlertas.PrecioSobrePresupuesto(
            [new LineaContraPresupuesto(8, 1, "Obra", "1.01", 105, 100, 2, 10, 2)],
            0.10m);
        var alta = ReglasAlertas.SobreEjecucion(
            [new LineaContraPresupuesto(null, 1, "Obra", "1.02", 0, 50, 13, 10, 13)]);
        var suelto = ReglasAlertas.EjecutadoNoPresupuestado(
            [new LineaContraPresupuesto(9, 1, "Obra", "Gas", 1000, 0, 1, 0, 1)]);

        Assert.Equal(60m, Assert.Single(sobrePrecio).Impacto);
        Assert.Empty(dentro);
        Assert.Equal("Alta", Assert.Single(alta).Severidad);
        Assert.Equal("A8", Assert.Single(suelto).Tipo);
    }
}
