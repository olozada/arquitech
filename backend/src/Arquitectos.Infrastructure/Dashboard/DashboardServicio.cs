using Arquitectos.Application.Dashboard;
using Arquitectos.Infrastructure.Persistence;
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
        int? materialId,
        CancellationToken cancellationToken)
    {
        var clave = $"precio|{desde:yyyyMMdd}|{hasta:yyyyMMdd}|{conceptoId}|{materialId}";
        if (_cache.TryGetValue(clave, out IReadOnlyList<PuntoPrecioDto>? cacheado) && cacheado is not null)
        {
            return cacheado;
        }

        var consulta = _context.ActaDetalles.AsNoTracking()
            .Where(detalle => detalle.Acta.FechaFinal >= desde && detalle.Acta.FechaFinal <= hasta);
        if (conceptoId is > 0)
        {
            consulta = consulta.Where(detalle => detalle.ConceptoId == conceptoId);
        }

        if (materialId is > 0)
        {
            consulta = consulta.Where(detalle => detalle.MaterialId == materialId);
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
        var kpis = new List<KpiDto>
        {
            Kpi("K1", "Valor ejecutado", actual.Valor, anterior.Valor, "moneda"),
            Kpi("K2", "Actas registradas", actual.Actas, anterior.Actas, "entero"),
            Kpi("K3", "Valor promedio por acta", Promedio(actual.Valor, actual.Actas), Promedio(anterior.Valor, anterior.Actas), "moneda"),
            Kpi("K4", "Obras activas", actual.Obras, anterior.Obras, "entero", actual.ObrasCatalogo),
            Kpi("K5", "Sobrecosto detectado", Suma(actual.Alertas, "A1"), Suma(anterior.Alertas, "A1"), "moneda"),
            Kpi("K6", "Ahorro potencial con proveedores", Suma(actual.Alertas, "A2"), Suma(anterior.Alertas, "A2"), "moneda"),
            Kpi("K7", "Días promedio de registro", actual.DiasPromedio, anterior.DiasPromedio, "dias", actual.PctTarde),
            Kpi("K8", "Alertas abiertas", abiertas.Count, abiertasAntes.Count, "entero")
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
            .Where(detalle => detalle.Acta.FechaFinal >= desde && detalle.Acta.FechaFinal <= hasta);
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

        return await consulta
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
                detalle.MaterialId,
                detalle.Material!.Descripcion,
                detalle.Cantidad,
                detalle.Precio,
                detalle.Subtotal))
            .ToListAsync(cancellationToken);
    }

    private async Task<List<ActaRegistro>> ActasAsync(
        FiltroDashboard filtro,
        DateOnly desde,
        DateOnly hasta,
        CancellationToken cancellationToken)
    {
        var consulta = _context.Actas.AsNoTracking()
            .Where(acta => acta.FechaFinal >= desde && acta.FechaFinal <= hasta);
        if (filtro.ContratistaId is > 0)
        {
            consulta = consulta.Where(acta => acta.ContratistaId == filtro.ContratistaId);
        }

        if (filtro.ArquitectoId is > 0)
        {
            consulta = consulta.Where(acta => acta.ArquitectoId == filtro.ArquitectoId);
        }

        if (filtro.ConstruccionId is > 0)
        {
            consulta = consulta.Where(acta => acta.Detalles.Any(detalle => detalle.ConstruccionId == filtro.ConstruccionId));
        }

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
        var historico = await _context.ActaDetalles.AsNoTracking()
            .Where(detalle => detalle.Acta.FechaFinal >= desdeHistorico && detalle.Acta.FechaFinal <= hasta)
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
                detalle.MaterialId,
                detalle.Material!.Descripcion,
                detalle.Cantidad,
                detalle.Precio,
                detalle.Subtotal))
            .ToListAsync(cancellationToken);

        var listas = await _context.ProveedorMateriales.AsNoTracking()
            .Select(precio => new { precio.MaterialId, precio.Precio, precio.Proveedor.Nombre })
            .ToListAsync(cancellationToken);
        var precios = listas
            .GroupBy(precio => precio.MaterialId)
            .Select(grupo =>
            {
                var minimo = grupo.Min(precio => precio.Precio);
                var proveedor = grupo.Where(precio => precio.Precio == minimo).Select(precio => precio.Nombre).OrderBy(nombre => nombre).First();
                return new PrecioProveedor(grupo.Key, minimo, proveedor);
            })
            .ToList();

        var obras = await ObrasAsync(filtro, cancellationToken);
        var hoy = ZonaHorariaDashboard.FechaLocal(DateTime.UtcNow, _zona);
        var alertas = ReglasAlertas.PreciosSobreReferencia(lineas, historico, _opciones)
            .Concat(ReglasAlertas.PreciosSobreLista(lineas, precios))
            .Concat(ReglasAlertas.DoblesCobros(lineas))
            .Concat(ReglasAlertas.ObrasSinActividad(obras, hoy, _opciones))
            .Concat(ReglasAlertas.ActasTardias(actas, _opciones));

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

        var fechas = await actividad
            .Select(detalle => new { detalle.ConstruccionId, detalle.Acta.FechaFinal })
            .ToListAsync(cancellationToken);
        var ultimas = fechas
            .GroupBy(fecha => fecha.ConstruccionId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Max(fecha => fecha.FechaFinal));

        var restringido = filtro.ContratistaId is > 0 || filtro.ArquitectoId is > 0;
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
            .Where(acta => acta.FechaFinal >= filtro.Desde && acta.FechaFinal <= filtro.Hasta);
        if (filtro.ContratistaId is > 0)
        {
            consulta = consulta.Where(acta => acta.ContratistaId == filtro.ContratistaId);
        }

        if (filtro.ArquitectoId is > 0)
        {
            consulta = consulta.Where(acta => acta.ArquitectoId == filtro.ArquitectoId);
        }

        if (filtro.ConstruccionId is > 0)
        {
            consulta = consulta.Where(acta => acta.Detalles.Any(detalle => detalle.ConstruccionId == filtro.ConstruccionId));
        }

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
        var activos = await _context.Materiales.AsNoTracking()
            .Where(material => material.Activo)
            .Select(material => material.Id)
            .ToListAsync(cancellationToken);
        var filasPrecio = await _context.ProveedorMateriales.AsNoTracking()
            .Select(precio => new { precio.MaterialId, precio.ProveedorId })
            .ToListAsync(cancellationToken);
        var precios = filasPrecio
            .GroupBy(precio => precio.MaterialId)
            .Select(grupo => new { Key = grupo.Key, Proveedores = grupo.Select(precio => precio.ProveedorId).Distinct().Count() })
            .ToList();
        var conPrecio = precios.Select(precio => precio.Key).ToHashSet();
        var pct = activos.Count == 0
            ? 0
            : Math.Round((decimal)activos.Count(id => conPrecio.Contains(id)) / activos.Count * 100m, 1);
        var unProveedor = precios.Count(precio => precio.Proveedores == 1 && activos.Contains(precio.Key));

        var actas = _context.Actas.AsNoTracking()
            .Where(acta => acta.FechaFinal >= filtro.Desde && acta.FechaFinal <= filtro.Hasta);
        if (filtro.ContratistaId is > 0)
        {
            actas = actas.Where(acta => acta.ContratistaId == filtro.ContratistaId);
        }

        if (filtro.ArquitectoId is > 0)
        {
            actas = actas.Where(acta => acta.ArquitectoId == filtro.ArquitectoId);
        }

        if (filtro.ConstruccionId is > 0)
        {
            actas = actas.Where(acta => acta.Detalles.Any(detalle => detalle.ConstruccionId == filtro.ConstruccionId));
        }

        var sinObservacion = await actas.CountAsync(
            acta => acta.Observacion == null || acta.Observacion == string.Empty,
            cancellationToken);

        return new CalidadDatosDto(
            pct,
            unProveedor,
            lineas.Count(linea => linea.MaterialId is null),
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

    private string Clave(FiltroDashboard filtro) =>
        $"{filtro.Desde:yyyyMMdd}|{filtro.Hasta:yyyyMMdd}|{filtro.ConstruccionId}|{filtro.ContratistaId}|{filtro.ArquitectoId}|{_opciones.UmbralDesvioPrecioPct}|{_opciones.VentanaReferenciaDias}|{_opciones.MinObservacionesReferencia}|{_opciones.DiasSinActividad}|{_opciones.DiasRegistroTardio}";

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
