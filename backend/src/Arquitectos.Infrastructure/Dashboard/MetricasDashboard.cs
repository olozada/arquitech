using Arquitectos.Application.Dashboard;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using Arquitectos.Infrastructure.Presupuestos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Arquitectos.Infrastructure.Dashboard;

public sealed partial class DashboardServicio : IDashboardServicio
{
    public async Task<DashboardDto> ObtenerAsync(FiltroDashboard filtro, CancellationToken cancellationToken)
    {
        var paquete = await ObtenerPaqueteAsync(filtro, cancellationToken);
        return paquete.Tablero;
    }


    public async Task<byte[]> ExportarAsync(FiltroDashboard filtro, CancellationToken cancellationToken)
    {
        var paquete = await ObtenerPaqueteAsync(filtro, cancellationToken);
        return GeneradorExcelDashboard.Generar(paquete.Tablero, paquete.Alertas);
    }

    private async Task<PaqueteDashboard> ObtenerPaqueteAsync(FiltroDashboard filtro, CancellationToken cancellationToken)
    {
        var clave = Clave(filtro);
        if (_cache.TryGetValue(clave, out PaqueteDashboard? cacheado) && cacheado is not null)
        {
            return cacheado;
        }

        var paquete = await CalcularAsync(filtro, cancellationToken);
        _cache.Set(clave, paquete, TimeSpan.FromSeconds(60));
        return paquete;
    }

    private async Task<PaqueteDashboard> CalcularAsync(FiltroDashboard filtro, CancellationToken cancellationToken)
    {
        var (desdeAnterior, hastaAnterior) = filtro.PeriodoAnterior();
        var actual = await PeriodoAsync(filtro, filtro.Desde, filtro.Hasta, cancellationToken);
        var anterior = await PeriodoAsync(filtro, desdeAnterior, hastaAnterior, cancellationToken);
        var anioAnterior = await LineasAsync(filtro, filtro.Desde.AddYears(-1), filtro.Hasta.AddYears(-1), cancellationToken);
        var calidad = await CalidadAsync(filtro, actual.Lineas, cancellationToken);

        var abiertas = actual.Alertas.Where(alerta => alerta.Tipo != "A5").ToList();
        var abiertasAntes = anterior.Alertas.Where(alerta => alerta.Tipo != "A5").ToList();
        var porSeveridad = Severidades.ToDictionary(
            severidad => severidad,
            severidad => abiertas.Count(alerta => alerta.Severidad == severidad));

        var total = actual.Valor;
        var metricas = await MetricasPresupuestoAsync(filtro, filtro.Hasta, cancellationToken);
        var metricasAnteriores = await MetricasPresupuestoAsync(filtro, hastaAnterior, cancellationToken);
        var kpis = new List<KpiDto>
        {
            Kpi("K1", "Valor ejecutado", actual.Valor, anterior.Valor, "moneda"),
            Kpi("K2", "Actas registradas", actual.Actas, anterior.Actas, "entero"),
            Kpi("K3", "Valor promedio por acta", Promedio(actual.Valor, actual.Actas), Promedio(anterior.Valor, anterior.Actas), "moneda"),
            Kpi("K4", "Obras activas", actual.Obras, anterior.Obras, "entero", actual.ObrasCatalogo),
            Kpi("K5", "Sobrecosto detectado", Suma(actual.Alertas, "A1"), Suma(anterior.Alertas, "A1"), "moneda"),
            Kpi("K6", "Ahorro potencial con proveedores", Suma(actual.Alertas, "A2"), Suma(anterior.Alertas, "A2"), "moneda"),
            Kpi("K7", "Días promedio de registro", actual.DiasPromedio, anterior.DiasPromedio, "dias", actual.PctTarde),
            Kpi("K8", "Alertas abiertas", abiertas.Count, abiertasAntes.Count, "entero"),
            Kpi("K9", "% de ejecución del presupuesto", metricas.Ejecucion, metricasAnteriores.Ejecucion, "pct"),
            Kpi("K10", "Desviación proyectada", metricas.Desviacion, metricasAnteriores.Desviacion, "moneda", metricas.DesviacionPct)
        };

        var tablero = new DashboardDto(
            filtro,
            desdeAnterior,
            hastaAnterior,
            kpis,
            CalculosDashboard.Tendencia(actual.Lineas, anioAnterior, filtro.Desde, filtro.Hasta),
            CalculosDashboard.Rankings(Agrupar(actual.Lineas, linea => linea.ConstruccionId, linea => linea.Construccion), total, 10),
            CalculosDashboard.Rankings(Agrupar(actual.Lineas, linea => linea.ContratistaId, linea => linea.Contratista), total, null),
            Conceptos(actual.Lineas, total),
            CalculosDashboard.Rankings(await PersonasAsync(filtro, true, cancellationToken), total, null),
            CalculosDashboard.Rankings(await PersonasAsync(filtro, false, cancellationToken), total, null),
            porSeveridad,
            actual.Alertas.Take(10).ToList(),
            calidad);

        return new PaqueteDashboard(tablero, actual.Alertas);
    }

    private async Task<Periodo> PeriodoAsync(
        FiltroDashboard filtro,
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken)
    {
        var lineas = await LineasAsync(filtro, desde, hasta, cancellationToken);
        var actas = await ActasAsync(filtro, desde, hasta, cancellationToken);
        var alertas = await AlertasAsync(filtro, desde, hasta, lineas, actas, cancellationToken);
        var dias = actas.Select(acta => acta.FechaRegistroLocal.DayNumber - acta.FechaFinal.DayNumber).ToList();
        var obrasCatalogo = await _context.Construcciones.AsNoTracking()
            .Where(obra => obra.Activo && (filtro.ConstruccionId == null || obra.Id == filtro.ConstruccionId))
            .CountAsync(cancellationToken);

        return new Periodo(
            lineas.Sum(linea => linea.Subtotal),
            actas.Count,
            lineas.Select(linea => linea.ConstruccionId).Distinct().Count(),
            obrasCatalogo,
            dias.Count == 0 ? 0 : Math.Round((decimal)dias.Average(), 1),
            dias.Count == 0 ? 0 : Math.Round((decimal)dias.Count(dia => dia > _opciones.DiasRegistroTardio) / dias.Count * 100m, 1),
            lineas,
            alertas);
    }


    private async Task<CalidadDatosDto> CalidadAsync(
        FiltroDashboard filtro,
        IReadOnlyList<LineaParaAlerta> lineas,
        CancellationToken cancellationToken)
    {
        var activos = await _context.Insumos.AsNoTracking()
            .Where(insumo => insumo.Activo)
            .Select(insumo => insumo.Id)
            .ToListAsync(cancellationToken);
        var filasPrecio = await _context.ProveedorMateriales.AsNoTracking()
            .Select(precio => new { precio.InsumoId, precio.ProveedorId })
            .ToListAsync(cancellationToken);
        var precios = filasPrecio
            .GroupBy(precio => precio.InsumoId)
            .Select(grupo => new { Key = grupo.Key, Proveedores = grupo.Select(precio => precio.ProveedorId).Distinct().Count() })
            .ToList();
        var conPrecio = precios.Select(precio => precio.Key).ToHashSet();
        var pct = activos.Count == 0
            ? 0
            : Math.Round((decimal)activos.Count(id => conPrecio.Contains(id)) / activos.Count * 100m, 1);
        var unProveedor = precios.Count(precio => precio.Proveedores == 1 && activos.Contains(precio.Key));

        var actas = _context.Actas.AsNoTracking()
            .Where(acta => acta.FechaInicial <= filtro.Hasta && acta.FechaFinal >= filtro.Desde);
        if (filtro.ContratistaId is > 0)
        {
            actas = actas.Where(acta => acta.ContratistaId == filtro.ContratistaId);
        }

        if (filtro.ArquitectoId is > 0)
        {
            actas = actas.Where(acta => acta.ArquitectoId == filtro.ArquitectoId);
        }

        if (filtro.RevisorId is > 0)
        {
            actas = actas.Where(acta => acta.RevisorId == filtro.RevisorId);
        }

        if (filtro.ConstruccionId is > 0)
        {
            actas = actas.Where(acta => acta.Detalles.Any(detalle => detalle.ConstruccionId == filtro.ConstruccionId));
        }

        actas = AplicarEstado(actas, filtro);

        var sinObservacion = await actas.CountAsync(
            acta => acta.Observacion == null || acta.Observacion == string.Empty,
            cancellationToken);

        return new CalidadDatosDto(
            pct,
            unProveedor,
            lineas.Count(linea => linea.InsumoId is null),
            sinObservacion);
    }

    private static KpiDto Kpi(string codigo, string titulo, decimal valor, decimal anterior, string formato, decimal? complemento = null) =>
        new(codigo, titulo, valor, anterior, CalculosDashboard.Variacion(valor, anterior), formato, complemento);

    private static decimal Promedio(decimal valor, int actas) =>
        actas == 0 ? 0 : Math.Round(valor / actas, 0);

    private static decimal Suma(IReadOnlyList<AlertaDto> alertas, string tipo) =>
        alertas.Where(alerta => alerta.Tipo == tipo).Sum(alerta => alerta.Impacto ?? 0);

    private static IQueryable<ActaDetalle> AplicarEstado(IQueryable<ActaDetalle> consulta, FiltroDashboard filtro) =>
        filtro.Cerrada is bool cerrada
            ? consulta.Where(detalle => detalle.Acta.Cerrada == cerrada)
            : consulta;

    private static IQueryable<Acta> AplicarEstado(IQueryable<Acta> consulta, FiltroDashboard filtro) =>
        filtro.Cerrada is bool cerrada
            ? consulta.Where(acta => acta.Cerrada == cerrada)
            : consulta;

    private string Clave(FiltroDashboard filtro) =>
        $"{filtro.Desde:yyyyMMdd}|{filtro.Hasta:yyyyMMdd}|{filtro.ConstruccionId}|{filtro.ContratistaId}|{filtro.ArquitectoId}|{filtro.RevisorId}|{filtro.Cerrada}|{_opciones.UmbralDesvioPrecioPct}|{_opciones.UmbralDesvioPresupuestoPct}|{_opciones.VentanaReferenciaDias}|{_opciones.MinObservacionesReferencia}|{_opciones.DiasSinActividad}|{_opciones.DiasRegistroTardio}";

    private async Task<(decimal Ejecucion, decimal Desviacion, decimal DesviacionPct)> MetricasPresupuestoAsync(
        FiltroDashboard filtro,
        DateOnly hasta,
        CancellationToken cancellationToken)
    {
        var items = await ItemsAprobadosAsync(filtro, cancellationToken);
        if (items.Count == 0)
        {
            return (0, 0, 0);
        }

        var ejecutado = await EjecucionAsync(filtro, items.Select(item => item.Id).ToList(), null, hasta, cancellationToken);
        return ResumirMetricas(items, ejecutado);
    }


    private static (decimal Ejecucion, decimal Desviacion, decimal DesviacionPct) ResumirMetricas(
        IReadOnlyList<ItemPresupuestoMetrica> items,
        IReadOnlyDictionary<int, (decimal Cantidad, decimal Valor)> ejecutado)
    {
        decimal presupuestado = 0;
        decimal gastado = 0;
        decimal proyeccion = 0;
        foreach (var item in items)
        {
            var eje = ejecutado.GetValueOrDefault(item.Id);
            var seguimiento = CalculadoraPresupuesto.Seguimiento(item.Cantidad, item.Unitario, eje.Cantidad, eje.Valor);
            presupuestado += seguimiento.ValorPresupuestado;
            gastado += seguimiento.ValorEjecutado;
            proyeccion += seguimiento.ProyeccionCierre;
        }

        var desviacion = proyeccion - presupuestado;
        return (
            presupuestado == 0 ? 0 : Math.Round(gastado / presupuestado * 100m, 1),
            desviacion,
            presupuestado == 0 ? 0 : Math.Round(desviacion / presupuestado * 100m, 1));
    }

    private sealed record ItemPresupuestoMetrica(int Id, int ConstruccionId, string Construccion, string Codigo, decimal Cantidad, decimal Unitario);

    private sealed record Periodo(
        decimal Valor,
        int Actas,
        int Obras,
        int ObrasCatalogo,
        decimal DiasPromedio,
        decimal PctTarde,
        IReadOnlyList<LineaParaAlerta> Lineas,
        IReadOnlyList<AlertaDto> Alertas);

    private sealed record PaqueteDashboard(DashboardDto Tablero, IReadOnlyList<AlertaDto> Alertas);

}
