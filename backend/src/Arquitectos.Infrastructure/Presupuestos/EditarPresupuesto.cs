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
    public async Task<ResultadoOperacion<PresupuestoDetalleDto>> ActualizarAsync(
        int id,
        PresupuestoSolicitud solicitud,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var presupuesto = await CargarEditableAsync(id, cancellationToken);
        if (presupuesto is null)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.NoEncontrado("No se encontró el presupuesto o no está en borrador.");
        }

        if (!await EsAdministradorAsync(usuarioId, cancellationToken))
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Prohibido("Solo el administrador puede editar presupuestos.");
        }

        if (VersionDistinta(presupuesto, solicitud.RowVersion))
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Conflicto("El presupuesto fue modificado por otro usuario.");
        }

        presupuesto.Nombre = TextoPresupuesto.Recortar(string.IsNullOrWhiteSpace(solicitud.Nombre) ? presupuesto.Nombre : solicitud.Nombre, 200);
        if (solicitud.Fecha != default)
        {
            presupuesto.Fecha = solicitud.Fecha;
        }

        CopiarEncabezado(presupuesto, solicitud);
        var error = await ReemplazarArbolAsync(presupuesto, solicitud, cancellationToken);
        if (error is not null)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Validacion(error);
        }

        await AsegurarConceptosAsync(presupuesto, cancellationToken);
        await AplicarCalculoAsync(presupuesto, true, cancellationToken);
        var conflicto = await GuardarAsync(cancellationToken);
        if (conflicto is not null)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Conflicto(conflicto);
        }

        return await ObtenerAsync(id, usuarioId, cancellationToken);
    }

    public async Task<ResultadoOperacion<bool>> EliminarAsync(int id, int usuarioId, CancellationToken cancellationToken, string? rowVersion = null)
    {
        if (!await EsAdministradorAsync(usuarioId, cancellationToken))
        {
            return ResultadoOperacion<bool>.Prohibido("Solo el administrador puede eliminar presupuestos.");
        }

        var presupuesto = await _context.Presupuestos.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (presupuesto is null)
        {
            return ResultadoOperacion<bool>.NoEncontrado("No se encontró el presupuesto.");
        }

        if (VersionDistinta(presupuesto, rowVersion))
        {
            return ResultadoOperacion<bool>.Conflicto("El presupuesto fue modificado por otro usuario.");
        }

        if (presupuesto.Estado != EstadoPresupuesto.Borrador)
        {
            return ResultadoOperacion<bool>.Validacion("Solo se puede eliminar un presupuesto en borrador.");
        }

        var hijos = await _context.Presupuestos.Where(item => item.PresupuestoOrigenId == id).ToListAsync(cancellationToken);
        foreach (var hijo in hijos)
        {
            hijo.PresupuestoOrigenId = null;
        }

        var ligado = await _context.ActaDetalles.AnyAsync(
            detalle => detalle.PresupuestoItem != null && detalle.PresupuestoItem.Capitulo.PresupuestoId == id,
            cancellationToken);
        if (ligado)
        {
            return ResultadoOperacion<bool>.Conflicto("Hay actas ligadas a ítems de este presupuesto.");
        }

        _context.Presupuestos.Remove(presupuesto);
        await _context.SaveChangesAsync(cancellationToken);
        return ResultadoOperacion<bool>.Ok(true);
    }


    private async Task<string?> ReemplazarArbolAsync(Presupuesto presupuesto, PresupuestoSolicitud solicitud, CancellationToken cancellationToken)
    {
        var error = Codigos(solicitud);
        if (error is not null)
        {
            return error;
        }

        var idsConservados = solicitud.Capitulos.SelectMany(capitulo => capitulo.Items).Where(item => item.Id is not null).Select(item => item.Id!.Value).ToHashSet();
        var actuales = presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items).ToList();
        var eliminados = actuales.Where(item => !idsConservados.Contains(item.Id)).Select(item => item.Id).ToList();
        if (eliminados.Count > 0 && await _context.ActaDetalles.AnyAsync(detalle => detalle.PresupuestoItemId != null && eliminados.Contains(detalle.PresupuestoItemId.Value), cancellationToken))
        {
            return "No se puede quitar un ítem que ya está en un acta.";
        }

        foreach (var item in actuales.Where(item => eliminados.Contains(item.Id)))
        {
            _context.Set<PresupuestoMedicion>().RemoveRange(item.Mediciones);
            _context.Set<PresupuestoItem>().Remove(item);
            item.Capitulo.Items.Remove(item);
        }

        var ordenCapitulo = 1;
        foreach (var capitulo in solicitud.Capitulos)
        {
            var entidad = capitulo.Id is int idCapitulo
                ? presupuesto.Capitulos.FirstOrDefault(item => item.Id == idCapitulo)
                : null;
            if (entidad is null)
            {
                entidad = new PresupuestoCapitulo { Nombre = "" };
                presupuesto.Capitulos.Add(entidad);
            }

            entidad.Numero = capitulo.Numero;
            entidad.Nombre = TextoPresupuesto.Recortar(capitulo.Nombre, 200);
            entidad.Orden = ordenCapitulo++;
            var orden = 1;
            foreach (var item in capitulo.Items)
            {
                var fila = item.Id is int idItem ? actuales.FirstOrDefault(actual => actual.Id == idItem) : null;
                if (fila is null)
                {
                    fila = CrearItem(item, orden++);
                    entidad.Items.Add(fila);
                }
                else
                {
                    if (fila.Capitulo != entidad)
                    {
                        fila.Capitulo.Items.Remove(fila);
                        entidad.Items.Add(fila);
                    }

                    CopiarItem(fila, item, orden++);
                    _context.Set<PresupuestoMedicion>().RemoveRange(fila.Mediciones);
                    fila.Mediciones.Clear();
                    foreach (var medicion in item.Mediciones)
                    {
                        fila.Mediciones.Add(Medicion(medicion));
                    }
                }
            }
        }

        var idsCapitulos = solicitud.Capitulos.Where(capitulo => capitulo.Id is not null).Select(capitulo => capitulo.Id!.Value).ToHashSet();
        foreach (var capitulo in presupuesto.Capitulos.ToList())
        {
            var tieneVivos = capitulo.Items.Any(item => _context.Entry(item).State != EntityState.Deleted);
            if (_context.Entry(capitulo).State != EntityState.Added && !idsCapitulos.Contains(capitulo.Id) && !tieneVivos)
            {
                _context.Set<PresupuestoCapitulo>().Remove(capitulo);
                presupuesto.Capitulos.Remove(capitulo);
            }
        }

        var ciclo = CicloCantidades(presupuesto);
        return ciclo;
    }

    private static PresupuestoItem CrearItem(ItemPresupuestoSolicitud solicitud, int orden)
    {
        var item = new PresupuestoItem { Codigo = "", Descripcion = "" };
        CopiarItem(item, solicitud, orden);
        foreach (var medicion in solicitud.Mediciones)
        {
            item.Mediciones.Add(Medicion(medicion));
        }

        return item;
    }

    private static void CopiarItem(PresupuestoItem item, ItemPresupuestoSolicitud solicitud, int orden)
    {
        item.Codigo = TextoPresupuesto.Recortar(solicitud.Codigo, 20);
        item.Descripcion = TextoPresupuesto.Recortar(solicitud.Descripcion, 500);
        item.Unidad = TextoPresupuesto.Recortar(solicitud.Unidad, 20);
        item.UnidadMedidaId = solicitud.UnidadMedidaId is > 0 ? solicitud.UnidadMedidaId : null;
        item.ApuId = solicitud.ApuId is >= 0 ? solicitud.ApuId : null;
        item.FactorConversionApu = solicitud.FactorConversionApu == 0 ? 1 : solicitud.FactorConversionApu;
        item.ConceptoId = solicitud.ConceptoId is > 0 ? solicitud.ConceptoId : null;
        item.Cantidad = solicitud.Cantidad;
        item.CantidadOrigenItemId = solicitud.CantidadOrigenItemId is > 0 ? solicitud.CantidadOrigenItemId : null;
        item.CantidadOrigenFactor = solicitud.CantidadOrigenFactor == 0 ? 1 : solicitud.CantidadOrigenFactor;
        item.ValorMaterialUnitario = solicitud.ValorMaterialUnitario;
        item.ValorManoObraUnitario = solicitud.ValorManoObraUnitario;
        item.Orden = orden;
    }

    private static PresupuestoMedicion Medicion(MedicionSolicitud solicitud)
    {
        var parcial = CalculadoraApu.ParcialMedicion(solicitud.Veces == 0 ? 1 : solicitud.Veces, solicitud.Largo, solicitud.Ancho, solicitud.Alto);
        return new PresupuestoMedicion
        {
            Ubicacion = TextoPresupuesto.Recortar(solicitud.Ubicacion, 200),
            Veces = solicitud.Veces == 0 ? 1 : solicitud.Veces,
            Largo = solicitud.Largo,
            Ancho = solicitud.Ancho,
            Alto = solicitud.Alto,
            Parcial = parcial
        };
    }

    private static void CopiarEncabezado(Presupuesto presupuesto, PresupuestoSolicitud solicitud)
    {
        presupuesto.MesesProyecto = solicitud.MesesProyecto;
        presupuesto.AreaConstruidaM2 = solicitud.AreaConstruidaM2;
        presupuesto.PctImprevistos = solicitud.PctImprevistos;
        presupuesto.PctIncremento = solicitud.PctIncremento;
        presupuesto.PctAdministracion = solicitud.PctAdministracion;
        presupuesto.PctUtilidad = solicitud.PctUtilidad;
        presupuesto.PctIvaUtilidad = solicitud.PctIvaUtilidad;
    }

}
