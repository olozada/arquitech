namespace Arquitectos.Application.Presupuestos;

public sealed record ResumenCosto(
    decimal Subtotal,
    decimal TotalMaterial,
    decimal TotalManoObra,
    decimal Imprevistos,
    decimal Incremento,
    decimal Administracion,
    decimal Utilidad,
    decimal IvaUtilidad,
    decimal Total,
    decimal? ValorPorM2);

public sealed record SeguimientoCalculado(
    decimal CantidadPresupuestada,
    decimal CantidadEjecutada,
    decimal AvanceFisico,
    decimal ValorPresupuestado,
    decimal ValorEjecutado,
    decimal AvanceFinanciero,
    decimal Saldo,
    decimal PrecioPresupuestado,
    decimal PrecioActas,
    decimal ProyeccionCierre);

public static class CalculadoraPresupuesto
{
    public static (decimal Material, decimal ManoObra) UnitariosDesdeApu(
        IReadOnlyDictionary<string, decimal> grupos,
        decimal factorConversion)
    {
        var manoObra = grupos.GetValueOrDefault("ManoObra");
        var material = grupos.Where(par => !par.Key.Equals("ManoObra", StringComparison.OrdinalIgnoreCase)).Sum(par => par.Value);
        return (material * factorConversion, manoObra * factorConversion);
    }

    public static ResumenCosto Resumir(
        decimal subtotal,
        decimal totalMaterial,
        decimal totalManoObra,
        decimal pctImprevistos,
        decimal pctIncremento,
        decimal pctAdministracion,
        decimal pctUtilidad,
        decimal pctIvaUtilidad,
        decimal areaM2)
    {
        var imprevistos = subtotal * pctImprevistos;
        var incremento = subtotal * pctIncremento;
        var administracion = subtotal * pctAdministracion;
        var utilidad = subtotal * pctUtilidad;
        var iva = utilidad * pctIvaUtilidad;
        var total = subtotal + imprevistos + incremento + administracion + utilidad + iva;
        decimal? porM2 = areaM2 > 0 ? total / areaM2 : null;
        return new ResumenCosto(subtotal, totalMaterial, totalManoObra, imprevistos, incremento, administracion, utilidad, iva, total, porM2);
    }

    public static SeguimientoCalculado Seguimiento(
        decimal cantidadPresupuestada,
        decimal valorUnitario,
        decimal cantidadEjecutada,
        decimal valorEjecutado)
    {
        var presupuestado = cantidadPresupuestada * valorUnitario;
        var avanceFisico = cantidadPresupuestada == 0 ? 0 : cantidadEjecutada / cantidadPresupuestada;
        var avanceFinanciero = presupuestado == 0 ? 0 : valorEjecutado / presupuestado;
        var precioActas = cantidadEjecutada == 0 ? 0 : valorEjecutado / cantidadEjecutada;
        decimal proyeccion;
        if (cantidadEjecutada == 0 || valorUnitario == 0)
        {
            proyeccion = presupuestado;
        }
        else
        {
            var saldoCantidad = cantidadPresupuestada - cantidadEjecutada;
            proyeccion = valorEjecutado + saldoCantidad * valorUnitario * (precioActas / valorUnitario);
        }

        return new SeguimientoCalculado(
            cantidadPresupuestada,
            cantidadEjecutada,
            avanceFisico,
            presupuestado,
            valorEjecutado,
            avanceFinanciero,
            presupuestado - valorEjecutado,
            valorUnitario,
            precioActas,
            proyeccion);
    }
}
