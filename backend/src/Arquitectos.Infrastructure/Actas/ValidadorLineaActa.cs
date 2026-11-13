using Arquitectos.Application.Actas;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Actas;

internal static class ValidadorLineaActa
{
    public static async Task<string?> ValidarAsync(
        ArquitectosDbContext context,
        LineaActaSolicitud linea,
        CancellationToken cancellationToken)
    {
        if (!await Activo(context.Construcciones, linea.ConstruccionId, cancellationToken))
        {
            return CrearActaValidador.MensajeConstruccion;
        }

        if (!await Activo(context.Conceptos, linea.ConceptoId, cancellationToken))
        {
            return CrearActaValidador.MensajeConcepto;
        }

        if (linea.UnidadMedidaId is > 0 &&
            !await Activo(context.UnidadesMedida, linea.UnidadMedidaId.Value, cancellationToken))
        {
            return CrearActaValidador.MensajeUnidad;
        }

        if (linea.InsumoId is > 0 &&
            !await Activo(context.Insumos, linea.InsumoId.Value, cancellationToken))
        {
            return CrearActaValidador.MensajeInsumo;
        }

        if (linea.PresupuestoItemId is > 0)
        {
            var valido = await context.Set<PresupuestoItem>().AnyAsync(
                item => item.Id == linea.PresupuestoItemId
                    && item.Capitulo.Presupuesto.Estado == EstadoPresupuesto.Aprobado
                    && item.Capitulo.Presupuesto.ConstruccionId == linea.ConstruccionId,
                cancellationToken);
            if (!valido)
            {
                return CrearActaValidador.MensajePresupuesto;
            }
        }

        if (decimal.Round(linea.Cantidad, 4, MidpointRounding.AwayFromZero) <= 0)
        {
            return CrearActaValidador.MensajeCantidad;
        }

        if (decimal.Round(linea.Precio, 2, MidpointRounding.AwayFromZero) < 0)
        {
            return CrearActaValidador.MensajePrecio;
        }

        if ((linea.Descripcion ?? "").Trim().Length > 500)
        {
            return CrearActaValidador.MensajeDescripcionLarga;
        }

        return null;
    }

    private static Task<bool> Activo<TEntidad>(IQueryable<TEntidad> consulta, int id, CancellationToken cancellationToken)
        where TEntidad : class, ICatalogo =>
        consulta.AnyAsync(entidad => entidad.Id == id && entidad.Activo, cancellationToken);
}
