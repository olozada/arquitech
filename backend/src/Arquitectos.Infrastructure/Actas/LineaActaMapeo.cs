using Arquitectos.Application.Actas;
using Arquitectos.Domain.Entidades;

namespace Arquitectos.Infrastructure.Actas;

internal static class LineaActaMapeo
{
    public static ActaDetalle Crear(LineaActaSolicitud linea, int tipoUsuarioId, Guid? idCliente)
    {
        var detalle = new ActaDetalle
        {
            Descripcion = (linea.Descripcion ?? "").Trim(),
            TipoUsuarioId = tipoUsuarioId,
            Aprobado = true,
            IdCliente = idCliente
        };
        Copiar(detalle, linea);
        return detalle;
    }

    public static void Copiar(ActaDetalle detalle, LineaActaSolicitud linea)
    {
        detalle.ConstruccionId = linea.ConstruccionId;
        detalle.ConceptoId = linea.ConceptoId;
        detalle.UnidadMedidaId = linea.UnidadMedidaId is > 0 ? linea.UnidadMedidaId : null;
        detalle.InsumoId = linea.InsumoId is > 0 ? linea.InsumoId : null;
        detalle.Cantidad = decimal.Round(linea.Cantidad, 4, MidpointRounding.AwayFromZero);
        detalle.Precio = decimal.Round(linea.Precio, 2, MidpointRounding.AwayFromZero);
        detalle.Descripcion = (linea.Descripcion ?? "").Trim();
        detalle.PresupuestoItemId = linea.PresupuestoItemId is > 0 ? linea.PresupuestoItemId : null;
    }

    public static bool Igual(ActaDetalle detalle, LineaActaSolicitud linea) =>
        detalle.ConstruccionId == linea.ConstruccionId &&
        detalle.ConceptoId == linea.ConceptoId &&
        detalle.UnidadMedidaId == (linea.UnidadMedidaId is > 0 ? linea.UnidadMedidaId : null) &&
        detalle.InsumoId == (linea.InsumoId is > 0 ? linea.InsumoId : null) &&
        detalle.PresupuestoItemId == (linea.PresupuestoItemId is > 0 ? linea.PresupuestoItemId : null) &&
        detalle.Cantidad == decimal.Round(linea.Cantidad, 4, MidpointRounding.AwayFromZero) &&
        detalle.Precio == decimal.Round(linea.Precio, 2, MidpointRounding.AwayFromZero) &&
        detalle.Descripcion == (linea.Descripcion ?? "").Trim();
}
