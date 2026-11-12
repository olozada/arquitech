using System.Text.Json;
using Arquitectos.Application.Auditoria;
using Arquitectos.Application.Presupuestos;
using Arquitectos.Application.Usuarios;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Auditoria;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Arquitectos.Infrastructure.Presupuestos;

public sealed partial class PresupuestoServicio
{
    public async Task<ResultadoOperacion<IReadOnlyList<PresupuestoResumenDto>>> ListarAsync(
        int? construccionId,
        int usuarioId,
        CancellationToken cancellationToken,
        string? estado = null,
        bool plantillas = false)
    {
        var actor = await ActorAsync(usuarioId, cancellationToken);
        if (actor is null)
        {
            return ResultadoOperacion<IReadOnlyList<PresupuestoResumenDto>>.Prohibido("No tiene acceso a los presupuestos.");
        }

        var consulta = _context.Presupuestos.AsNoTracking().Include(presupuesto => presupuesto.Construccion).AsQueryable();
        consulta = plantillas
            ? consulta.Where(presupuesto => presupuesto.EsPlantilla)
            : consulta.Where(presupuesto => !presupuesto.EsPlantilla);
        if (construccionId is > 0)
        {
            consulta = consulta.Where(presupuesto => presupuesto.ConstruccionId == construccionId);
        }

        if (!string.IsNullOrWhiteSpace(estado) && Enum.TryParse<EstadoPresupuesto>(estado, true, out var estadoFiltro))
        {
            consulta = consulta.Where(presupuesto => presupuesto.Estado == estadoFiltro);
        }

        if (!actor.Administrador)
        {
            if (plantillas)
            {
                return ResultadoOperacion<IReadOnlyList<PresupuestoResumenDto>>.Ok([]);
            }

            var obras = await ObrasDelArquitectoAsync(actor.ArquitectoId!.Value, cancellationToken);
            consulta = consulta.Where(presupuesto => presupuesto.ConstruccionId != null && obras.Contains(presupuesto.ConstruccionId.Value));
        }

        var presupuestos = await consulta
            .Include(presupuesto => presupuesto.Capitulos)
            .ThenInclude(capitulo => capitulo.Items)
            .OrderByDescending(presupuesto => presupuesto.Id)
            .ToListAsync(cancellationToken);
        var itemPresupuesto = presupuestos
            .SelectMany(presupuesto => presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items.Select(item => new { ItemId = item.Id, PresupuestoId = presupuesto.Id })))
            .ToDictionary(par => par.ItemId, par => par.PresupuestoId);
        var ejecutado = await EjecutadoPorItemAsync(itemPresupuesto.Keys.ToList(), cancellationToken);
        var porPresupuesto = ejecutado
            .GroupBy(par => itemPresupuesto[par.Key])
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Sum(par => par.Value));

        var lista = presupuestos.Select(presupuesto =>
        {
            var costo = CostoDe(presupuesto);
            var gastado = porPresupuesto.GetValueOrDefault(presupuesto.Id);
            var avance = costo.Subtotal == 0 ? 0 : gastado / costo.Subtotal;
            return new PresupuestoResumenDto(
                presupuesto.Id,
                presupuesto.ConstruccionId ?? 0,
                presupuesto.Construccion?.Descripcion ?? "",
                presupuesto.Nombre,
                presupuesto.Version,
                presupuesto.Estado.ToString(),
                costo.Total,
                costo.ValorPorM2,
                avance);
        }).ToList();
        return ResultadoOperacion<IReadOnlyList<PresupuestoResumenDto>>.Ok(lista);
    }

    public async Task<ResultadoOperacion<PresupuestoDetalleDto>> ObtenerAsync(int id, int usuarioId, CancellationToken cancellationToken)
    {
        var presupuesto = await CargarAsync(id, cancellationToken);
        if (presupuesto is null)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.NoEncontrado("No se encontró el presupuesto.");
        }

        if (!await PuedeVerAsync(usuarioId, presupuesto.ConstruccionId, cancellationToken))
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Prohibido("No tiene acceso a este presupuesto.");
        }

        return ResultadoOperacion<PresupuestoDetalleDto>.Ok(await DetalleAsync(presupuesto, cancellationToken));
    }


    public Task<ResultadoOperacion<byte[]>> ExcelAsync(int id, int usuarioId, CancellationToken cancellationToken) =>
        ExportarAsync(id, usuarioId, (presupuesto, token) => new ExportadorPresupuesto(_context).ExcelAsync(presupuesto, token), cancellationToken);

    public Task<ResultadoOperacion<byte[]>> PdfAsync(int id, int usuarioId, CancellationToken cancellationToken) =>
        ExportarAsync(id, usuarioId, (presupuesto, token) => Task.FromResult(new ExportadorPresupuesto(_context).Pdf(presupuesto)), cancellationToken);


    public async Task<IReadOnlyList<ItemActaDto>> ItemsParaActaAsync(int construccionId, bool incluirPrecio, CancellationToken cancellationToken)
    {
        var items = await _context.Set<PresupuestoItem>().AsNoTracking()
            .Where(item => !item.Capitulo.Presupuesto.EsPlantilla && item.Capitulo.Presupuesto.ConstruccionId == construccionId && item.Capitulo.Presupuesto.Estado == EstadoPresupuesto.Aprobado)
            .OrderBy(item => item.Codigo)
            .Select(item => new ItemActaDto(
                item.Id,
                item.Codigo,
                item.Descripcion,
                item.Unidad,
                item.ConceptoId,
                item.UnidadMedidaId,
                incluirPrecio ? item.ValorMaterialUnitario + item.ValorManoObraUnitario : null))
            .ToListAsync(cancellationToken);
        return items;
    }

    private async Task<ResultadoOperacion<byte[]>> ExportarAsync(
        int id,
        int usuarioId,
        Func<Presupuesto, CancellationToken, Task<byte[]>> generar,
        CancellationToken cancellationToken)
    {
        var presupuesto = await CargarAsync(id, cancellationToken);
        if (presupuesto is null)
        {
            return ResultadoOperacion<byte[]>.NoEncontrado("No se encontró el presupuesto.");
        }

        if (!await PuedeVerAsync(usuarioId, presupuesto.ConstruccionId, cancellationToken))
        {
            return ResultadoOperacion<byte[]>.Prohibido("No tiene acceso a este presupuesto.");
        }

        return ResultadoOperacion<byte[]>.Ok(await generar(presupuesto, cancellationToken));
    }

    private async Task<Presupuesto?> CargarAsync(int id, CancellationToken cancellationToken) =>
        await _context.Presupuestos
            .Include(presupuesto => presupuesto.Construccion)
            .Include(presupuesto => presupuesto.Capitulos)
            .ThenInclude(capitulo => capitulo.Items)
            .ThenInclude(item => item.Mediciones)
            .FirstOrDefaultAsync(presupuesto => presupuesto.Id == id, cancellationToken);

    private async Task<Presupuesto?> CargarEditableAsync(int id, CancellationToken cancellationToken)
    {
        var presupuesto = await CargarAsync(id, cancellationToken);
        return presupuesto is { Estado: EstadoPresupuesto.Borrador } ? presupuesto : null;
    }

    private async Task<PresupuestoDetalleDto> DetalleAsync(Presupuesto presupuesto, CancellationToken cancellationToken)
    {
        var apus = await _context.Apus.AsNoTracking()
            .Select(apu => new { apu.Id, apu.Descripcion, apu.Unidad })
            .ToDictionaryAsync(apu => apu.Id, cancellationToken);
        var costo = CostoDe(presupuesto);
        var capitulos = presupuesto.Capitulos.OrderBy(capitulo => capitulo.Orden).Select(capitulo =>
        {
            var items = capitulo.Items.OrderBy(item => item.Orden).Select(item =>
            {
                string? apu = null;
                string? unidadApu = null;
                if (item.ApuId is int apuId && apus.TryGetValue(apuId, out var datos))
                {
                    apu = datos.Descripcion;
                    unidadApu = datos.Unidad;
                }

                var unitario = item.ValorMaterialUnitario + item.ValorManoObraUnitario;
                return new ItemPresupuestoDto(
                    item.Id,
                    item.Codigo,
                    item.Descripcion,
                    item.Unidad,
                    item.UnidadMedidaId,
                    item.ApuId,
                    apu,
                    unidadApu,
                    item.FactorConversionApu,
                    item.ApuId is > 0 && item.FactorConversionApu == 1 && !TextoPresupuesto.MismaUnidad(item.Unidad, unidadApu),
                    item.ConceptoId,
                    item.Cantidad,
                    item.CantidadOrigenItemId,
                    item.CantidadOrigenFactor,
                    item.ValorMaterialUnitario,
                    item.ValorManoObraUnitario,
                    unitario,
                    item.Cantidad * unitario,
                    item.ApuId is > 0,
                    item.Mediciones.Select(medicion => new MedicionDto(medicion.Id, medicion.Ubicacion, medicion.Veces, medicion.Largo, medicion.Ancho, medicion.Alto, medicion.Parcial)).ToList());
            }).ToList();
            var total = items.Sum(item => item.Total);
            return new CapituloPresupuestoDto(capitulo.Id, capitulo.Numero, capitulo.Nombre, total, costo.Subtotal == 0 ? 0 : total / costo.Subtotal, items);
        }).ToList();

        return new PresupuestoDetalleDto(
            presupuesto.Id,
            presupuesto.ConstruccionId ?? 0,
            presupuesto.Construccion?.Descripcion ?? "",
            presupuesto.Nombre,
            presupuesto.Version,
            presupuesto.Estado.ToString(),
            presupuesto.Fecha,
            presupuesto.MesesProyecto,
            presupuesto.AreaConstruidaM2,
            presupuesto.PctImprevistos,
            presupuesto.PctIncremento,
            presupuesto.PctAdministracion,
            presupuesto.PctUtilidad,
            presupuesto.PctIvaUtilidad,
            presupuesto.FechaAprobacion,
            costo,
            capitulos,
            Convert.ToBase64String(presupuesto.RowVersion ?? []),
            presupuesto.EsPlantilla);
    }

    private static CostoPresupuestoDto CostoDe(Presupuesto presupuesto)
    {
        var items = presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items).ToList();
        var material = items.Sum(item => item.Cantidad * item.ValorMaterialUnitario);
        var manoObra = items.Sum(item => item.Cantidad * item.ValorManoObraUnitario);
        return Resumen(presupuesto, material + manoObra, material, manoObra);
    }

    private static CostoPresupuestoDto Resumen(Presupuesto presupuesto, decimal subtotal, decimal material, decimal manoObra)
    {
        var resumen = CalculadoraPresupuesto.Resumir(
            subtotal,
            material,
            manoObra,
            presupuesto.PctImprevistos,
            presupuesto.PctIncremento,
            presupuesto.PctAdministracion,
            presupuesto.PctUtilidad,
            presupuesto.PctIvaUtilidad,
            presupuesto.AreaConstruidaM2);
        return new CostoPresupuestoDto(
            resumen.Subtotal,
            resumen.TotalMaterial,
            resumen.TotalManoObra,
            resumen.Imprevistos,
            resumen.Incremento,
            resumen.Administracion,
            resumen.Utilidad,
            resumen.IvaUtilidad,
            resumen.Total,
            resumen.ValorPorM2);
    }

    private async Task<bool> EsAdministradorAsync(int usuarioId, CancellationToken cancellationToken)
    {
        var actor = await ActorAsync(usuarioId, cancellationToken);
        return actor is { Administrador: true };
    }

    private async Task<bool> PuedeVerAsync(int usuarioId, int? construccionId, CancellationToken cancellationToken)
    {
        var actor = await ActorAsync(usuarioId, cancellationToken);
        if (actor is null)
        {
            return false;
        }

        if (actor.Administrador)
        {
            return true;
        }

        if (construccionId is not int obraId)
        {
            return false;
        }

        var obras = await ObrasDelArquitectoAsync(actor.ArquitectoId!.Value, cancellationToken);
        return obras.Contains(obraId);
    }

    private async Task<HashSet<int>> ObrasDelArquitectoAsync(int arquitectoId, CancellationToken cancellationToken)
    {
        var obras = await _context.ActaDetalles.AsNoTracking()
            .Where(detalle => detalle.Acta.ArquitectoId == arquitectoId)
            .Select(detalle => detalle.ConstruccionId)
            .Distinct()
            .ToListAsync(cancellationToken);
        return obras.ToHashSet();
    }

    private async Task<Actor?> ActorAsync(int usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await _context.Usuarios.AsNoTracking()
            .Where(item => item.Id == usuarioId)
            .Select(item => new { item.TipoUsuarioId, item.ArquitectoId })
            .FirstOrDefaultAsync(cancellationToken);
        if (usuario is null)
        {
            return null;
        }

        if (usuario.TipoUsuarioId == PerfilesUsuario.Administrador)
        {
            return new Actor(true, null);
        }

        if (usuario.TipoUsuarioId == PerfilesUsuario.Arquitecto && usuario.ArquitectoId is int arquitectoId)
        {
            return new Actor(false, arquitectoId);
        }

        return null;
    }

    private sealed record Actor(bool Administrador, int? ArquitectoId);

}
