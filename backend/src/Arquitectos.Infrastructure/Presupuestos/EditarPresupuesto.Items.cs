using Arquitectos.Application.Presupuestos;
using Arquitectos.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Presupuestos;

public sealed partial class PresupuestoServicio
{
    public async Task<ResultadoOperacion<PresupuestoDetalleDto>> AgregarDesdeApuAsync(
        int id,
        int capituloId,
        ItemsDesdeApuSolicitud solicitud,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var presupuesto = await BorradorDeAdministradorAsync(id, solicitud.RowVersion, usuarioId, cancellationToken);
        if (presupuesto.Estado != EstadoOperacion.Ok)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.MismoEstado(presupuesto);
        }

        var capitulo = presupuesto.Valor!.Capitulos.FirstOrDefault(item => item.Id == capituloId);
        if (capitulo is null)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.NoEncontrado("No se encontró el capítulo.");
        }

        var ids = solicitud.ApuIds.Where(item => item > 0).Distinct().ToList();
        var apus = await _context.Apus.AsNoTracking().Where(apu => ids.Contains(apu.Id) && apu.Activo).ToListAsync(cancellationToken);
        if (apus.Count != ids.Count)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Validacion("Hay análisis de precios que no existen.");
        }

        var orden = capitulo.Items.Select(item => item.Orden).DefaultIfEmpty(0).Max();
        foreach (var apuId in ids)
        {
            var apu = apus.First(item => item.Id == apuId);
            orden++;
            capitulo.Items.Add(new PresupuestoItem
            {
                Codigo = SiguienteCodigo(presupuesto.Valor, capitulo.Numero),
                Descripcion = TextoPresupuesto.Recortar(apu.Descripcion, 500),
                Unidad = TextoPresupuesto.Recortar(apu.Unidad, 20),
                UnidadMedidaId = apu.UnidadMedidaId,
                ApuId = apu.Id,
                FactorConversionApu = 1,
                ConceptoId = apu.ConceptoId,
                Cantidad = 0,
                CantidadOrigenFactor = 1,
                Orden = orden
            });
        }

        await AsegurarConceptosAsync(presupuesto.Valor, cancellationToken);
        await AplicarCalculoAsync(presupuesto.Valor, true, cancellationToken);
        var conflicto = await GuardarAsync(cancellationToken);
        return conflicto is null
            ? await ObtenerAsync(id, usuarioId, cancellationToken)
            : ResultadoOperacion<PresupuestoDetalleDto>.Conflicto(conflicto);
    }

    public async Task<ResultadoOperacion<PresupuestoDetalleDto>> RenumerarAsync(int id, string? rowVersion, int usuarioId, CancellationToken cancellationToken)
    {
        var presupuesto = await BorradorDeAdministradorAsync(id, rowVersion, usuarioId, cancellationToken);
        if (presupuesto.Estado != EstadoOperacion.Ok)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.MismoEstado(presupuesto);
        }

        var numero = 1;
        foreach (var capitulo in presupuesto.Valor!.Capitulos.OrderBy(item => item.Orden).ThenBy(item => item.Id))
        {
            capitulo.Numero = numero;
            var indice = 1;
            foreach (var item in capitulo.Items.OrderBy(fila => fila.Orden).ThenBy(fila => fila.Id))
            {
                item.Codigo = $"{numero}.{indice:00}";
                indice++;
            }

            numero++;
        }

        var conflicto = await GuardarAsync(cancellationToken);
        return conflicto is null
            ? await ObtenerAsync(id, usuarioId, cancellationToken)
            : ResultadoOperacion<PresupuestoDetalleDto>.Conflicto(conflicto);
    }

    public async Task<ResultadoOperacion<PresupuestoDetalleDto>> OrdenarAsync(int id, OrdenPresupuestoSolicitud solicitud, int usuarioId, CancellationToken cancellationToken)
    {
        var presupuesto = await BorradorDeAdministradorAsync(id, solicitud.RowVersion, usuarioId, cancellationToken);
        if (presupuesto.Estado != EstadoOperacion.Ok)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.MismoEstado(presupuesto);
        }

        foreach (var pedido in solicitud.Capitulos)
        {
            var capitulo = presupuesto.Valor!.Capitulos.FirstOrDefault(item => item.Id == pedido.Id);
            if (capitulo is null)
            {
                continue;
            }

            capitulo.Orden = pedido.Orden;
            var posicion = 1;
            foreach (var itemId in pedido.Items)
            {
                var item = capitulo.Items.FirstOrDefault(fila => fila.Id == itemId);
                if (item is not null)
                {
                    item.Orden = posicion++;
                }
            }
        }

        var conflicto = await GuardarAsync(cancellationToken);
        return conflicto is null
            ? await ObtenerAsync(id, usuarioId, cancellationToken)
            : ResultadoOperacion<PresupuestoDetalleDto>.Conflicto(conflicto);
    }

    public async Task<ResultadoOperacion<PresupuestoDetalleDto>> DuplicarItemAsync(int id, int itemId, string? rowVersion, int usuarioId, CancellationToken cancellationToken)
    {
        var presupuesto = await BorradorDeAdministradorAsync(id, rowVersion, usuarioId, cancellationToken);
        if (presupuesto.Estado != EstadoOperacion.Ok)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.MismoEstado(presupuesto);
        }

        var capitulo = presupuesto.Valor!.Capitulos.FirstOrDefault(item => item.Items.Any(fila => fila.Id == itemId));
        var origen = capitulo?.Items.FirstOrDefault(item => item.Id == itemId);
        if (capitulo is null || origen is null)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.NoEncontrado("No se encontró el ítem.");
        }

        foreach (var item in capitulo.Items.Where(item => item.Orden > origen.Orden))
        {
            item.Orden++;
        }

        capitulo.Items.Add(new PresupuestoItem
        {
            Codigo = SiguienteCodigo(presupuesto.Valor, capitulo.Numero),
            Descripcion = origen.Descripcion,
            Unidad = origen.Unidad,
            UnidadMedidaId = origen.UnidadMedidaId,
            ApuId = origen.ApuId,
            FactorConversionApu = origen.FactorConversionApu,
            ConceptoId = origen.ConceptoId,
            Cantidad = origen.Cantidad,
            CantidadOrigenFactor = origen.CantidadOrigenFactor,
            ValorMaterialUnitario = origen.ValorMaterialUnitario,
            ValorManoObraUnitario = origen.ValorManoObraUnitario,
            Orden = origen.Orden + 1
        });
        var conflicto = await GuardarAsync(cancellationToken);
        return conflicto is null
            ? await ObtenerAsync(id, usuarioId, cancellationToken)
            : ResultadoOperacion<PresupuestoDetalleDto>.Conflicto(conflicto);
    }

    public async Task<ResultadoOperacion<PresupuestoDetalleDto>> MoverItemAsync(int id, int itemId, MoverItemSolicitud solicitud, int usuarioId, CancellationToken cancellationToken)
    {
        var presupuesto = await BorradorDeAdministradorAsync(id, solicitud.RowVersion, usuarioId, cancellationToken);
        if (presupuesto.Estado != EstadoOperacion.Ok)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.MismoEstado(presupuesto);
        }

        var origen = presupuesto.Valor!.Capitulos.FirstOrDefault(item => item.Items.Any(fila => fila.Id == itemId));
        var destino = presupuesto.Valor.Capitulos.FirstOrDefault(item => item.Id == solicitud.CapituloDestinoId);
        var item = origen?.Items.FirstOrDefault(fila => fila.Id == itemId);
        if (origen is null || destino is null || item is null)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.NoEncontrado("No se encontró el capítulo o el ítem.");
        }

        origen.Items.Remove(item);
        item.Capitulo = destino;
        item.CapituloId = destino.Id;
        item.Orden = destino.Items.Select(fila => fila.Orden).DefaultIfEmpty(0).Max() + 1;
        destino.Items.Add(item);
        var conflicto = await GuardarAsync(cancellationToken);
        return conflicto is null
            ? await ObtenerAsync(id, usuarioId, cancellationToken)
            : ResultadoOperacion<PresupuestoDetalleDto>.Conflicto(conflicto);
    }

    private async Task<ResultadoOperacion<Presupuesto>> BorradorDeAdministradorAsync(int id, string? rowVersion, int usuarioId, CancellationToken cancellationToken)
    {
        if (!await EsAdministradorAsync(usuarioId, cancellationToken))
        {
            return ResultadoOperacion<Presupuesto>.Prohibido("Solo el administrador puede editar presupuestos.");
        }

        var presupuesto = await CargarEditableAsync(id, cancellationToken);
        if (presupuesto is null)
        {
            return ResultadoOperacion<Presupuesto>.NoEncontrado("No se encontró el presupuesto o no está en borrador.");
        }

        if (VersionDistinta(presupuesto, rowVersion))
        {
            return ResultadoOperacion<Presupuesto>.Conflicto("El presupuesto fue modificado por otro usuario.");
        }

        return ResultadoOperacion<Presupuesto>.Ok(presupuesto);
    }

    private static string SiguienteCodigo(Presupuesto presupuesto, int numeroCapitulo) =>
        ReglasPresupuesto.SiguienteCodigo(presupuesto, numeroCapitulo);

    private async Task<string?> GuardarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            return "El presupuesto fue modificado por otro usuario.";
        }
    }

    private static bool VersionDistinta(Presupuesto presupuesto, string? token) =>
        ReglasPresupuesto.VersionDistinta(presupuesto.RowVersion, token);

}
