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


}
