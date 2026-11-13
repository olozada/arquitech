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

        _cache.Set(clave, serie, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60),
            Size = 1
        });
        return serie;
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


}
