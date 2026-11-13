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
        ActorActa actor,
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

        var cambios = solicitud.Detalles
            .Where(linea => actuales[linea.Id!.Value].Aprobado != linea.Aprobado)
            .ToList();
        if (cambios.Count == 0)
        {
            return ResultadoActa<ActaDto>.Ok((await CargarAsync(acta.NumeroActa, cancellationToken))!);
        }

        await using var transaccion = await _context.Database.BeginTransactionAsync(cancellationToken);
        foreach (var linea in cambios)
        {
            var detalle = actuales[linea.Id!.Value];
            var resultado = await _operaciones.RevisarAsync(
                acta.NumeroActa,
                detalle.Id,
                Convert.ToBase64String(detalle.RowVersion),
                linea.Aprobado,
                actor,
                cancellationToken);
            if (resultado.Estado != EstadoActa.Ok)
            {
                await transaccion.RollbackAsync(cancellationToken);
                _context.ChangeTracker.Clear();
                return Traducir(resultado);
            }
        }

        await transaccion.CommitAsync(cancellationToken);
        return ResultadoActa<ActaDto>.Ok((await CargarAsync(acta.NumeroActa, cancellationToken))!);
    }

    private static decimal TotalActa(IEnumerable<(decimal Subtotal, bool Aprobado)> lineas) =>
        lineas.Sum(linea => linea.Aprobado ? linea.Subtotal : 0);


}
