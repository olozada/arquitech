using Arquitectos.Application.Presupuestos;
using Arquitectos.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Presupuestos;

public sealed partial class PresupuestoServicio
{
    public async Task<ResultadoOperacion<ParchePresupuestoDto>> ParchearItemAsync(
        int presupuestoId,
        int itemId,
        ItemParcheSolicitud solicitud,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var borrador = await BorradorDeAdministradorAsync(presupuestoId, solicitud.RowVersion, usuarioId, cancellationToken);
        if (borrador.Estado != EstadoOperacion.Ok)
        {
            return ResultadoOperacion<ParchePresupuestoDto>.MismoEstado(borrador);
        }

        var item = borrador.Valor!.Capitulos.SelectMany(capitulo => capitulo.Items).FirstOrDefault(fila => fila.Id == itemId);
        if (item is null)
        {
            return ResultadoOperacion<ParchePresupuestoDto>.NoEncontrado("No se encontró el ítem.");
        }

        var unidades = await _context.UnidadesMedida.AsNoTracking().Where(unidad => unidad.Activo).ToDictionaryAsync(unidad => unidad.Id, cancellationToken);
        var error = AplicarParcheItem(item, solicitud, unidades);
        if (error is not null)
        {
            return ResultadoOperacion<ParchePresupuestoDto>.Validacion(error);
        }

        await AplicarCalculoAsync(borrador.Valor, true, cancellationToken);
        var conflicto = await GuardarAsync(cancellationToken);
        if (conflicto is not null)
        {
            return ResultadoOperacion<ParchePresupuestoDto>.Conflicto(conflicto);
        }

        return ResultadoOperacion<ParchePresupuestoDto>.Ok(await ParcheDesdeAsync(borrador.Valor, cancellationToken));
    }

    public async Task<ResultadoOperacion<ParchePresupuestoDto>> CrearItemAsync(
        int presupuestoId,
        int capituloId,
        ItemParcheSolicitud solicitud,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var borrador = await BorradorDeAdministradorAsync(presupuestoId, solicitud.RowVersion, usuarioId, cancellationToken);
        if (borrador.Estado != EstadoOperacion.Ok)
        {
            return ResultadoOperacion<ParchePresupuestoDto>.MismoEstado(borrador);
        }

        var capitulo = borrador.Valor!.Capitulos.FirstOrDefault(item => item.Id == capituloId);
        if (capitulo is null)
        {
            return ResultadoOperacion<ParchePresupuestoDto>.NoEncontrado("No se encontró el capítulo.");
        }

        var orden = capitulo.Items.Select(fila => fila.Orden).DefaultIfEmpty(0).Max() + 1;
        var item = new PresupuestoItem
        {
            Codigo = TextoPresupuesto.Recortar(solicitud.Codigo ?? SiguienteCodigo(borrador.Valor, capitulo.Numero), 20),
            Descripcion = TextoPresupuesto.Recortar(solicitud.Descripcion ?? "", 500),
            Unidad = "",
            FactorConversionApu = 1,
            CantidadOrigenFactor = 1,
            Orden = orden
        };
        capitulo.Items.Add(item);

        var unidades = await _context.UnidadesMedida.AsNoTracking().Where(unidad => unidad.Activo).ToDictionaryAsync(unidad => unidad.Id, cancellationToken);
        var error = AplicarParcheItem(item, solicitud, unidades);
        if (error is not null)
        {
            return ResultadoOperacion<ParchePresupuestoDto>.Validacion(error);
        }

        await AsegurarConceptosAsync(borrador.Valor, cancellationToken);
        await AplicarCalculoAsync(borrador.Valor, true, cancellationToken);
        var conflicto = await GuardarAsync(cancellationToken);
        if (conflicto is not null)
        {
            return ResultadoOperacion<ParchePresupuestoDto>.Conflicto(conflicto);
        }

        return ResultadoOperacion<ParchePresupuestoDto>.Ok(await ParcheDesdeAsync(borrador.Valor, cancellationToken));
    }

    public async Task<ResultadoOperacion<ParchePresupuestoDto>> EliminarItemAsync(
        int presupuestoId,
        int itemId,
        int usuarioId,
        CancellationToken cancellationToken,
        string? rowVersion = null)
    {
        var borrador = await BorradorDeAdministradorAsync(presupuestoId, rowVersion, usuarioId, cancellationToken);
        if (borrador.Estado != EstadoOperacion.Ok)
        {
            return ResultadoOperacion<ParchePresupuestoDto>.MismoEstado(borrador);
        }

        var capitulo = borrador.Valor!.Capitulos.FirstOrDefault(cap => cap.Items.Any(fila => fila.Id == itemId));
        var item = capitulo?.Items.FirstOrDefault(fila => fila.Id == itemId);
        if (capitulo is null || item is null)
        {
            return ResultadoOperacion<ParchePresupuestoDto>.NoEncontrado("No se encontró el ítem.");
        }

        if (await _context.ActaDetalles.AnyAsync(detalle => detalle.PresupuestoItemId == itemId, cancellationToken))
        {
            return ResultadoOperacion<ParchePresupuestoDto>.Conflicto("No se puede quitar un ítem que ya está en un acta.");
        }

        capitulo.Items.Remove(item);
        await AplicarCalculoAsync(borrador.Valor, true, cancellationToken);
        var conflicto = await GuardarAsync(cancellationToken);
        if (conflicto is not null)
        {
            return ResultadoOperacion<ParchePresupuestoDto>.Conflicto(conflicto);
        }

        return ResultadoOperacion<ParchePresupuestoDto>.Ok(await ParcheDesdeAsync(borrador.Valor, cancellationToken));
    }

    private static string? AplicarParcheItem(PresupuestoItem item, ItemParcheSolicitud solicitud, IReadOnlyDictionary<int, UnidadMedida> unidades)
    {
        if (solicitud.Codigo != null)
        {
            item.Codigo = TextoPresupuesto.Recortar(solicitud.Codigo, 20);
        }

        if (solicitud.Descripcion != null)
        {
            item.Descripcion = TextoPresupuesto.Recortar(solicitud.Descripcion, 500);
        }

        if (solicitud.UnidadMedidaId.HasValue || solicitud.Unidad != null)
        {
            var temp = new ItemPresupuestoSolicitud
            {
                Codigo = item.Codigo,
                UnidadMedidaId = solicitud.UnidadMedidaId ?? item.UnidadMedidaId,
                Unidad = solicitud.Unidad ?? item.Unidad
            };
            var error = AplicarUnidadCatalogo(temp, unidades);
            if (error is not null)
            {
                return error;
            }

            item.UnidadMedidaId = temp.UnidadMedidaId;
            item.Unidad = temp.Unidad;
        }

        if (solicitud.ApuId.HasValue)
        {
            item.ApuId = solicitud.ApuId >= 0 ? solicitud.ApuId : null;
        }

        if (solicitud.FactorConversionApu.HasValue)
        {
            item.FactorConversionApu = solicitud.FactorConversionApu.Value == 0 ? 1 : solicitud.FactorConversionApu.Value;
        }

        if (solicitud.ConceptoId.HasValue)
        {
            item.ConceptoId = solicitud.ConceptoId > 0 ? solicitud.ConceptoId : null;
        }

        if (solicitud.Cantidad.HasValue)
        {
            item.Cantidad = solicitud.Cantidad.Value;
        }

        if (solicitud.FormulaCantidad != null)
        {
            item.FormulaCantidad = string.IsNullOrWhiteSpace(solicitud.FormulaCantidad)
                ? null
                : TextoPresupuesto.Recortar(solicitud.FormulaCantidad.Trim(), 200);
            if (!string.IsNullOrWhiteSpace(item.FormulaCantidad))
            {
                item.CantidadOrigenItemId = null;
                item.CantidadOrigenFactor = 1;
            }
        }

        if (solicitud.ValorMaterialUnitario.HasValue)
        {
            item.ValorMaterialUnitario = solicitud.ValorMaterialUnitario.Value;
        }

        if (solicitud.ValorManoObraUnitario.HasValue)
        {
            item.ValorManoObraUnitario = solicitud.ValorManoObraUnitario.Value;
        }

        if (solicitud.DesdeApu.HasValue)
        {
            // sin campo en entidad; precios vienen del APU en AplicarCalculoAsync
        }

        return null;
    }

    private async Task<ParchePresupuestoDto> ParcheDesdeAsync(Presupuesto presupuesto, CancellationToken cancellationToken)
    {
        var detalle = await DetalleAsync(presupuesto, cancellationToken);
        var items = detalle.Capitulos.SelectMany(capitulo => capitulo.Items).ToList();
        return new ParchePresupuestoDto(detalle.RowVersion, detalle.Resumen, items);
    }
}