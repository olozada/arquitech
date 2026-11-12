using System.Globalization;
using System.Linq.Expressions;
using Arquitectos.Application.Actas;
using Arquitectos.Application.Auditoria;
using Arquitectos.Application.Usuarios;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Auditoria;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Actas;

public sealed partial class ActaServicio : IActaServicio
{
    private async Task<ResultadoActa<ActaDto>> RevisarAsync(
        Acta acta,
        CrearActaSolicitud solicitud,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        if (solicitud.Detalles.Any(linea => linea.Id is not > 0))
        {
            return ResultadoActa<ActaDto>.Invalido(CrearActaValidador.MensajeRevisorAgrega);
        }

        var actuales = acta.Detalles.ToDictionary(detalle => detalle.Id);
        var ids = solicitud.Detalles.Select(linea => linea.Id!.Value).ToList();
        if (ids.Distinct().Count() != ids.Count)
        {
            return ResultadoActa<ActaDto>.Invalido("Hay un ítem repetido en el acta.");
        }

        if (ids.Any(id => !actuales.ContainsKey(id)))
        {
            return ResultadoActa<ActaDto>.Invalido("Hay un ítem que no pertenece al acta.");
        }

        foreach (var linea in solicitud.Detalles)
        {
            var detalle = actuales[linea.Id!.Value];
            if (detalle.Aprobado == linea.Aprobado)
            {
                continue;
            }

            await _auditoria.RegistrarAsync(
                "ActaDetalle",
                detalle.Id.ToString(),
                linea.Aprobado ? AccionAuditoria.Aprobar : AccionAuditoria.Rechazar,
                usuarioId,
                CambiosAuditoria.Json("Aprobado", detalle.Aprobado, linea.Aprobado),
                cancellationToken);
            detalle.Aprobado = linea.Aprobado;
        }

        var conflictoRevision = await GuardarAsync(cancellationToken);
        if (conflictoRevision is not null)
        {
            return ResultadoActa<ActaDto>.Conflicto(conflictoRevision);
        }

        return ResultadoActa<ActaDto>.Ok((await CargarAsync(acta.NumeroActa, cancellationToken))!);
    }

    private static decimal TotalActa(IEnumerable<(decimal Subtotal, bool Aprobado)> lineas) =>
        lineas.Sum(linea => linea.Aprobado ? linea.Subtotal : 0);


}
