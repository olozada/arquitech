using Arquitectos.Application.Dashboard;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using Arquitectos.Infrastructure.Presupuestos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Arquitectos.Infrastructure.Dashboard;

public sealed class DashboardServicio : IDashboardServicio
{
    private const int TamanoPagina = 20;

    private static readonly string[] Severidades = ["Alta", "Media", "Baja"];

    private readonly ArquitectosDbContext _context;
    private readonly OpcionesDashboard _opciones;
    private readonly IMemoryCache _cache;
    private readonly TimeZoneInfo _zona;

    /// <summary>Solo para pruebas. En la aplicación se usa el reloj UTC.</summary>
    internal DateTime? InstanteUtc { get; set; }

    public DashboardServicio(
        ArquitectosDbContext context,
        IOptions<OpcionesDashboard> opciones,
        IMemoryCache cache)
    {
        _context = context;
        _opciones = opciones.Value;
        _cache = cache;
        _zona = ZonaHorariaDashboard.Resolver(_opciones.ZonaHoraria);
    }

    public async Task<DashboardDto> ObtenerAsync(FiltroDashboard filtro, CancellationToken cancellationToken)
    {
        var paquete = await ObtenerPaqueteAsync(filtro, cancellationToken);
        return paquete.Tablero;
    }

    public async Task<PaginaAlertasDto> AlertasAsync(
        FiltroDashboard filtro,
        string? tipo,
        string? severidad,
        int pagina,
        CancellationToken cancellationToken)
    {
        var paquete = await ObtenerPaqueteAsync(filtro, cancellationToken);
        var consulta = paquete.Alertas.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(tipo))
        {
            consulta = consulta.Where(alerta => alerta.Tipo.Equals(tipo, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(severidad))
        {
            consulta = consulta.Where(alerta => alerta.Severidad.Equals(severidad, StringComparison.OrdinalIgnoreCase));
        }

        var lista = consulta.ToList();
        var actual = Math.Max(1, pagina);
        return new PaginaAlertasDto(
            lista.Skip((actual - 1) * TamanoPagina).Take(TamanoPagina).ToList(),
            lista.Count,
            actual,
            TamanoPagina);
    }

    public async Task<IReadOnlyList<PuntoPrecioDto>> PrecioUnitarioAsync(
        DateOnly desde,
        DateOnly hasta,
        int? conceptoId,
        int? insumoId,
        bool? cerrada,
        CancellationToken cancellationToken)
    {
        var clave = $"precio|{desde:yyyyMMdd}|{hasta:yyyyMMdd}|{conceptoId}|{insumoId}|{cerrada}";
        if (_cache.TryGetValue(clave, out IReadOnlyList<PuntoPrecioDto>? cacheado) && cacheado is not null)
        {
            return cacheado;
        }

        var consulta = _context.ActaDetalles.AsNoTracking()
            .Where(detalle => detalle.Acta.FechaInicial <= hasta && detalle.Acta.FechaFinal >= desde);
        if (conceptoId is > 0)
        {
            consulta = consulta.Where(detalle => detalle.ConceptoId == conceptoId);
        }

        if (insumoId is > 0)
        {
            consulta = consulta.Where(detalle => detalle.InsumoId == insumoId);
        }

        if (cerrada is bool soloCerradas)
        {
            consulta = consulta.Where(detalle => detalle.Acta.Cerrada == soloCerradas);
        }

        var lineas = await consulta
            .Select(detalle => new { detalle.Acta.FechaFinal, detalle.Precio })
            .ToListAsync(cancellationToken);

        var serie = lineas
            .GroupBy(linea => new { linea.FechaFinal.Year, linea.FechaFinal.Month })
            .OrderBy(grupo => grupo.Key.Year)
            .ThenBy(grupo => grupo.Key.Month)
            .Select(grupo => new PuntoPrecioDto(
                grupo.Key.Year,
                grupo.Key.Month,
                Math.Round((decimal)grupo.Average(linea => linea.Precio), 2),
                grupo.Min(linea => linea.Precio),
                grupo.Max(linea => linea.Precio),
                grupo.Count()))
            .ToList();

        _cache.Set(clave, serie, TimeSpan.FromSeconds(60));
        return serie;
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

    private async Task<List<LineaParaAlerta>> LineasAsync(
        FiltroDashboard filtro,
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken)
    {
        var consulta = _context.ActaDetalles.AsNoTracking()
            .Where(detalle => detalle.Acta.FechaInicial <= hasta && detalle.Acta.FechaFinal >= desde);
        if (filtro.ConstruccionId is > 0)
        {
            consulta = consulta.Where(detalle => detalle.ConstruccionId == filtro.ConstruccionId);
        }

        if (filtro.ContratistaId is > 0)
        {
            consulta = consulta.Where(detalle => detalle.Acta.ContratistaId == filtro.ContratistaId);
        }

        if (filtro.ArquitectoId is > 0)
        {
            consulta = consulta.Where(detalle => detalle.Acta.ArquitectoId == filtro.ArquitectoId);
        }

        if (filtro.RevisorId is > 0)
        {
            consulta = consulta.Where(detalle => detalle.Acta.RevisorId == filtro.RevisorId);
        }

        consulta = AplicarEstado(consulta, filtro);

        var lineas = await consulta
            .Select(detalle => new LineaParaAlerta(
                detalle.Id,
                detalle.NumeroActa,
                detalle.Acta.FechaInicial,
                detalle.Acta.FechaFinal,
                detalle.Acta.ContratistaId,
                detalle.Acta.Contratista.Nombre,
                detalle.ConstruccionId,
                detalle.Construccion.Descripcion,
                detalle.ConceptoId,
                detalle.Concepto.Descripcion,
                detalle.UnidadMedidaId,
                detalle.InsumoId,
                detalle.Insumo!.Descripcion,
                detalle.Cantidad,
                detalle.Precio,
                detalle.Subtotal))
            .ToListAsync(cancellationToken);
        if (lineas.Count == 0)
        {
            return lineas;
        }

        var numeros = lineas.Select(linea => linea.NumeroActa).Distinct().ToList();
        var rechazadas = await _context.ActaDetalles
            .AsNoTracking()
            .Where(detalle => numeros.Contains(detalle.NumeroActa) && !detalle.Aprobado)
            .Select(detalle => detalle.Id)
            .ToListAsync(cancellationToken);
        var idsRechazados = rechazadas.ToHashSet();
        return lineas
            .Select(linea => idsRechazados.Contains(linea.Id) ? linea with { Subtotal = 0 } : linea)
            .ToList();
    }

    private async Task<List<ActaRegistro>> ActasAsync(
        FiltroDashboard filtro,
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken)
    {
        var consulta = _context.Actas.AsNoTracking()
            .Where(acta => acta.FechaInicial <= hasta && acta.FechaFinal >= desde);
        if (filtro.ContratistaId is > 0)
        {
            consulta = consulta.Where(acta => acta.ContratistaId == filtro.ContratistaId);
        }

        if (filtro.ArquitectoId is > 0)
        {
            consulta = consulta.Where(acta => acta.ArquitectoId == filtro.ArquitectoId);
        }

        if (filtro.RevisorId is > 0)
        {
            consulta = consulta.Where(acta => acta.RevisorId == filtro.RevisorId);
        }

        if (filtro.ConstruccionId is > 0)
        {
            consulta = consulta.Where(acta => acta.Detalles.Any(detalle => detalle.ConstruccionId == filtro.ConstruccionId));
        }

        consulta = AplicarEstado(consulta, filtro);

        var filas = await consulta
            .Select(acta => new
            {
                acta.NumeroActa,
                acta.FechaFinal,
                acta.FechaRegistro,
                Contratista = acta.Contratista.Nombre
            })
            .ToListAsync(cancellationToken);

        return filas
            .Select(acta => new ActaRegistro(
                acta.NumeroActa,
                acta.FechaFinal,
                ZonaHorariaDashboard.FechaLocal(acta.FechaRegistro, _zona),
                acta.Contratista,
                null,
                null))
            .ToList();
    }

    private async Task<IReadOnlyList<AlertaDto>> AlertasAsync(
        FiltroDashboard filtro,
        DateOnly desde,
        DateOnly hasta,
        IReadOnlyList<LineaParaAlerta> lineas,
        IReadOnlyList<ActaRegistro> actas,
        CancellationToken cancellationToken)
    {
        var desdeHistorico = desde.AddDays(-_opciones.VentanaReferenciaDias);
        var historicoConsulta = _context.ActaDetalles.AsNoTracking()
            .Where(detalle => detalle.Acta.FechaFinal >= desdeHistorico && detalle.Acta.FechaFinal <= hasta);
        historicoConsulta = AplicarEstado(historicoConsulta, filtro);
        var historico = await historicoConsulta
            .Select(detalle => new LineaParaAlerta(
                detalle.Id,
                detalle.NumeroActa,
                detalle.Acta.FechaInicial,
                detalle.Acta.FechaFinal,
                detalle.Acta.ContratistaId,
                detalle.Acta.Contratista.Nombre,
                detalle.ConstruccionId,
                detalle.Construccion.Descripcion,
                detalle.ConceptoId,
                detalle.Concepto.Descripcion,
                detalle.UnidadMedidaId,
                detalle.InsumoId,
                detalle.Insumo!.Descripcion,
                detalle.Cantidad,
                detalle.Precio,
                detalle.Subtotal))
            .ToListAsync(cancellationToken);

        var listas = await _context.ProveedorMateriales.AsNoTracking()
            .Select(precio => new { precio.InsumoId, precio.Precio, precio.Proveedor.Nombre })
            .ToListAsync(cancellationToken);
        var precios = listas
            .GroupBy(precio => precio.InsumoId)
            .Select(grupo =>
            {
                var minimo = grupo.Min(precio => precio.Precio);
                var proveedor = grupo.Where(precio => precio.Precio == minimo).Select(precio => precio.Nombre).OrderBy(nombre => nombre).First();
                return new PrecioProveedor(grupo.Key, minimo, proveedor);
            })
            .ToList();

        var obras = await ObrasAsync(filtro, cancellationToken);
        var hoy = ZonaHorariaDashboard.FechaLocal(InstanteUtc ?? DateTime.UtcNow, _zona);
        var alertas = ReglasAlertas.PreciosSobreReferencia(lineas, historico, _opciones)
            .Concat(ReglasAlertas.PreciosSobreLista(lineas, precios))
            .Concat(ReglasAlertas.DoblesCobros(lineas))
            .Concat(ReglasAlertas.ObrasSinActividad(obras, hoy, _opciones))
            .Concat(ReglasAlertas.ActasTardias(actas, _opciones))
            .Concat(await AlertasPresupuestoAsync(filtro, desde, hasta, cancellationToken));

        return ReglasAlertas.Ordenar(alertas).Take(_opciones.MaxAlertas).ToList();
    }

    private async Task<List<ObraActividad>> ObrasAsync(FiltroDashboard filtro, CancellationToken cancellationToken)
    {
        var obras = _context.Construcciones.AsNoTracking().Where(obra => obra.Activo);
        if (filtro.ConstruccionId is > 0)
        {
            obras = obras.Where(obra => obra.Id == filtro.ConstruccionId);
        }

        var catalogo = await obras.Select(obra => new { obra.Id, obra.Descripcion }).ToListAsync(cancellationToken);
        var actividad = _context.ActaDetalles.AsNoTracking().AsQueryable();
        if (filtro.ConstruccionId is > 0)
        {
            actividad = actividad.Where(detalle => detalle.ConstruccionId == filtro.ConstruccionId);
        }

        if (filtro.ContratistaId is > 0)
        {
            actividad = actividad.Where(detalle => detalle.Acta.ContratistaId == filtro.ContratistaId);
        }

        if (filtro.ArquitectoId is > 0)
        {
            actividad = actividad.Where(detalle => detalle.Acta.ArquitectoId == filtro.ArquitectoId);
        }

        if (filtro.RevisorId is > 0)
        {
            actividad = actividad.Where(detalle => detalle.Acta.RevisorId == filtro.RevisorId);
        }

        actividad = AplicarEstado(actividad, filtro);

        var fechas = await actividad
            .Select(detalle => new { detalle.ConstruccionId, detalle.Acta.FechaFinal })
            .ToListAsync(cancellationToken);
        var ultimas = fechas
            .GroupBy(fecha => fecha.ConstruccionId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Max(fecha => fecha.FechaFinal));

        var restringido = filtro.ContratistaId is > 0 || filtro.ArquitectoId is > 0 || filtro.RevisorId is > 0 || filtro.Cerrada is not null;
        return catalogo
            .Where(obra => !restringido || filtro.ConstruccionId is > 0 || ultimas.ContainsKey(obra.Id))
            .Select(obra => new ObraActividad(
                obra.Id,
                obra.Descripcion,
                ultimas.TryGetValue(obra.Id, out var ultima) ? ultima : null))
            .ToList();
    }

    private async Task<List<(int Id, string Nombre, decimal Valor, int Actas)>> PersonasAsync(
        FiltroDashboard filtro,
        bool arquitectos,
        CancellationToken cancellationToken)
    {
        var lineas = await LineasAsync(filtro, filtro.Desde, filtro.Hasta, cancellationToken);
        var consulta = _context.Actas.AsNoTracking()
            .Where(acta => acta.FechaInicial <= filtro.Hasta && acta.FechaFinal >= filtro.Desde);
        if (filtro.ContratistaId is > 0)
        {
            consulta = consulta.Where(acta => acta.ContratistaId == filtro.ContratistaId);
        }

        if (filtro.ArquitectoId is > 0)
        {
            consulta = consulta.Where(acta => acta.ArquitectoId == filtro.ArquitectoId);
        }

        if (filtro.RevisorId is > 0)
        {
            consulta = consulta.Where(acta => acta.RevisorId == filtro.RevisorId);
        }

        if (filtro.ConstruccionId is > 0)
        {
            consulta = consulta.Where(acta => acta.Detalles.Any(detalle => detalle.ConstruccionId == filtro.ConstruccionId));
        }

        consulta = AplicarEstado(consulta, filtro);

        var personas = await consulta
            .Select(acta => new
            {
                acta.NumeroActa,
                Id = arquitectos ? acta.ArquitectoId : acta.RevisorId,
                Nombre = arquitectos ? acta.Arquitecto.Nombre : acta.Revisor.Nombre
            })
            .ToListAsync(cancellationToken);

        var valorPorActa = lineas
            .GroupBy(linea => linea.NumeroActa)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Sum(linea => linea.Subtotal));

        return personas
            .GroupBy(persona => new { persona.Id, persona.Nombre })
            .Select(grupo => (
                grupo.Key.Id,
                grupo.Key.Nombre,
                grupo.Sum(persona => valorPorActa.GetValueOrDefault(persona.NumeroActa)),
                grupo.Select(persona => persona.NumeroActa).Distinct().Count()))
            .ToList();
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

    private static IReadOnlyList<RankingDto> Conceptos(IReadOnlyList<LineaParaAlerta> lineas, decimal total)
    {
        var grupos = Agrupar(lineas, linea => linea.ConceptoId, linea => linea.Concepto)
            .OrderByDescending(fila => fila.Valor)
            .ThenBy(fila => fila.Nombre)
            .ToList();
        var principales = grupos.Take(8).ToList();
        var resto = grupos.Skip(8).ToList();
        if (resto.Count > 0)
        {
            principales.Add((0, "Otros", resto.Sum(fila => fila.Valor), resto.Sum(fila => fila.Actas)));
        }

        return CalculosDashboard.Rankings(principales, total, null);
    }

    private static List<(int Id, string Nombre, decimal Valor, int Actas)> Agrupar(
        IReadOnlyList<LineaParaAlerta> lineas,
        Func<LineaParaAlerta, int> id,
        Func<LineaParaAlerta, string> nombre) =>
        lineas
            .GroupBy(linea => new { Id = id(linea), Nombre = nombre(linea) })
            .Select(grupo => (
                grupo.Key.Id,
                grupo.Key.Nombre,
                grupo.Sum(linea => linea.Subtotal),
                grupo.Select(linea => linea.NumeroActa).Distinct().Count()))
            .ToList();

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

    private async Task<IReadOnlyList<AlertaDto>> AlertasPresupuestoAsync(
        FiltroDashboard filtro,
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken)
    {
        var items = await ItemsAprobadosAsync(filtro, cancellationToken);
        if (items.Count == 0)
        {
            return [];
        }

        var ids = items.Select(item => item.Id).ToList();
        var obras = items.Select(item => item.ConstruccionId).Distinct().ToList();
        var acumulado = await EjecucionAsync(filtro, ids, null, hasta, cancellationToken);
        var delPeriodo = await _context.ActaDetalles.AsNoTracking()
            .Where(detalle => detalle.Aprobado && detalle.Acta.FechaFinal >= desde && detalle.Acta.FechaFinal <= hasta)
            .Where(detalle => detalle.PresupuestoItemId != null && ids.Contains(detalle.PresupuestoItemId.Value))
            .Select(detalle => new
            {
                detalle.NumeroActa,
                detalle.ConstruccionId,
                Construccion = detalle.Construccion.Descripcion,
                detalle.PresupuestoItemId,
                detalle.Cantidad,
                detalle.Precio
            })
            .ToListAsync(cancellationToken);
        var porItem = items.ToDictionary(item => item.Id);
        var sobrePrecio = delPeriodo.Select(linea =>
        {
            var item = porItem[linea.PresupuestoItemId!.Value];
            var eje = acumulado.GetValueOrDefault(item.Id);
            return new LineaContraPresupuesto(linea.NumeroActa, linea.ConstruccionId, linea.Construccion, item.Codigo, linea.Precio, item.Unitario, linea.Cantidad, item.Cantidad, eje.Cantidad);
        }).ToList();
        var sobreCantidad = items.Select(item =>
        {
            var eje = acumulado.GetValueOrDefault(item.Id);
            return new LineaContraPresupuesto(null, item.ConstruccionId, item.Construccion, item.Codigo, eje.Cantidad == 0 ? 0 : eje.Valor / eje.Cantidad, item.Unitario, eje.Cantidad, item.Cantidad, eje.Cantidad);
        }).ToList();
        var sueltos = await _context.ActaDetalles.AsNoTracking()
            .Where(detalle => detalle.Aprobado && detalle.PresupuestoItemId == null && obras.Contains(detalle.ConstruccionId))
            .Where(detalle => detalle.Acta.FechaFinal >= desde && detalle.Acta.FechaFinal <= hasta)
            .Select(detalle => new LineaContraPresupuesto(
                detalle.NumeroActa,
                detalle.ConstruccionId,
                detalle.Construccion.Descripcion,
                detalle.Concepto.Descripcion,
                detalle.Precio,
                0,
                detalle.Cantidad,
                0,
                detalle.Cantidad))
            .ToListAsync(cancellationToken);
        return ReglasAlertas.PrecioSobrePresupuesto(sobrePrecio, _opciones.UmbralFraccionPresupuesto)
            .Concat(ReglasAlertas.SobreEjecucion(sobreCantidad))
            .Concat(ReglasAlertas.EjecutadoNoPresupuestado(sueltos))
            .ToList();
    }

    private async Task<List<ItemPresupuestoMetrica>> ItemsAprobadosAsync(FiltroDashboard filtro, CancellationToken cancellationToken)
    {
        var consulta = _context.Set<PresupuestoItem>().AsNoTracking()
            .Where(item => item.Capitulo.Presupuesto.Estado == EstadoPresupuesto.Aprobado && !item.Capitulo.Presupuesto.EsPlantilla);
        if (filtro.ConstruccionId is > 0)
        {
            consulta = consulta.Where(item => item.Capitulo.Presupuesto.ConstruccionId == filtro.ConstruccionId);
        }

        if (filtro.ArquitectoId is > 0)
        {
            consulta = consulta.Where(item => _context.Actas.Any(acta => acta.ArquitectoId == filtro.ArquitectoId && acta.Detalles.Any(detalle => detalle.ConstruccionId == item.Capitulo.Presupuesto.ConstruccionId)));
        }

        if (filtro.ContratistaId is > 0)
        {
            consulta = consulta.Where(item => _context.Actas.Any(acta => acta.ContratistaId == filtro.ContratistaId && acta.Detalles.Any(detalle => detalle.ConstruccionId == item.Capitulo.Presupuesto.ConstruccionId)));
        }

        if (filtro.RevisorId is > 0)
        {
            consulta = consulta.Where(item => _context.Actas.Any(acta => acta.RevisorId == filtro.RevisorId && acta.Detalles.Any(detalle => detalle.ConstruccionId == item.Capitulo.Presupuesto.ConstruccionId)));
        }

        return await consulta.Select(item => new ItemPresupuestoMetrica(
            item.Id,
            item.Capitulo.Presupuesto.ConstruccionId ?? 0,
            item.Capitulo.Presupuesto.Construccion!.Descripcion,
            item.Codigo,
            item.Cantidad,
            item.ValorMaterialUnitario + item.ValorManoObraUnitario)).ToListAsync(cancellationToken);
    }

    private async Task<Dictionary<int, (decimal Cantidad, decimal Valor)>> EjecucionAsync(
        FiltroDashboard filtro,
        IReadOnlyCollection<int> items,
        DateOnly? desde,
        DateOnly hasta,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return [];
        }

        var consulta = _context.ActaDetalles.AsNoTracking()
            .Where(detalle => detalle.Aprobado && detalle.PresupuestoItemId != null && items.Contains(detalle.PresupuestoItemId.Value))
            .Where(detalle => detalle.Acta.FechaFinal <= hasta);
        if (desde is DateOnly inicio)
        {
            consulta = consulta.Where(detalle => detalle.Acta.FechaFinal >= inicio);
        }

        if (filtro.ContratistaId is > 0)
        {
            consulta = consulta.Where(detalle => detalle.Acta.ContratistaId == filtro.ContratistaId);
        }

        var lineas = await consulta.Select(detalle => new { detalle.PresupuestoItemId, detalle.Cantidad, detalle.Subtotal }).ToListAsync(cancellationToken);
        return lineas.GroupBy(linea => linea.PresupuestoItemId!.Value)
            .ToDictionary(grupo => grupo.Key, grupo => (grupo.Sum(linea => linea.Cantidad), grupo.Sum(linea => linea.Subtotal)));
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
