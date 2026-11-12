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
    public async Task<ResultadoActa<ActaDto>> ActualizarAsync(
        int numeroActa,
        CrearActaSolicitud solicitud,
        int usuarioId,
        CancellationToken cancellationToken)
    {
        var error = await ValidarSolicitud(solicitud, cancellationToken);
        if (error is not null)
        {
            return ResultadoActa<ActaDto>.Invalido(error);
        }

        var actor = await CargarActorAsync(usuarioId, cancellationToken);
        if (actor is null)
        {
            return ResultadoActa<ActaDto>.Invalido("No se encontró el usuario.");
        }

        var acta = await _context.Actas
            .Include(item => item.Detalles)
            .SingleOrDefaultAsync(item => item.NumeroActa == numeroActa, cancellationToken);
        if (acta is null || !PuedeVer(acta, actor))
        {
            return ResultadoActa<ActaDto>.NoEncontrado("No se encontró el acta.");
        }

        if (VersionDistinta(acta.RowVersion, solicitud.RowVersion))
        {
            return ResultadoActa<ActaDto>.Conflicto("Otra persona modificó esta acta.");
        }

        if (acta.Cerrada)
        {
            return ResultadoActa<ActaDto>.Invalido(CrearActaValidador.MensajeCerrada);
        }

        if (actor.TipoUsuarioId == PerfilesUsuario.Revisor)
        {
            return await RevisarAsync(acta, solicitud, usuarioId, cancellationToken);
        }

        if (actor.TipoUsuarioId != PerfilesUsuario.Administrador)
        {
            return await ActualizarPerfilAsync(acta, solicitud, actor, cancellationToken);
        }

        var ids = solicitud.Detalles.Where(linea => linea.Id is > 0).Select(linea => linea.Id!.Value).ToList();
        if (ids.Distinct().Count() != ids.Count)
        {
            return ResultadoActa<ActaDto>.Invalido("Hay un ítem repetido en el acta.");
        }

        var actuales = acta.Detalles.ToDictionary(detalle => detalle.Id);
        if (ids.Any(id => !actuales.ContainsKey(id)))
        {
            return ResultadoActa<ActaDto>.Invalido("Hay un ítem que no pertenece al acta.");
        }

        acta.FechaInicial = solicitud.FechaInicial;
        acta.FechaFinal = solicitud.FechaFinal;
        acta.ArquitectoId = solicitud.ArquitectoId;
        acta.RevisorId = solicitud.RevisorId;
        acta.ContratistaId = solicitud.ContratistaId;
        acta.Observacion = Texto(solicitud.Observacion);

        var conservar = ids.ToHashSet();
        foreach (var detalle in acta.Detalles.Where(detalle => !conservar.Contains(detalle.Id)).ToList())
        {
            acta.Detalles.Remove(detalle);
            _context.ActaDetalles.Remove(detalle);
        }

        foreach (var linea in solicitud.Detalles)
        {
            if (linea.Id is > 0)
            {
                CopiarDetalle(actuales[linea.Id.Value], linea);
            }
            else
            {
                acta.Detalles.Add(CrearDetalle(linea, PerfilesUsuario.Arquitecto));
            }
        }

        var conflicto = await GuardarAsync(cancellationToken);
        if (conflicto is not null)
        {
            return ResultadoActa<ActaDto>.Conflicto(conflicto);
        }

        var actualizada = await CargarAsync(numeroActa, cancellationToken);
        return ResultadoActa<ActaDto>.Ok(actualizada!);
    }


    private async Task<ResultadoActa<ActaDto>> ActualizarPerfilAsync(
        Acta acta,
        CrearActaSolicitud solicitud,
        ActorActa actor,
        CancellationToken cancellationToken)
    {
        var ids = solicitud.Detalles.Where(linea => linea.Id is > 0).Select(linea => linea.Id!.Value).ToList();
        if (ids.Distinct().Count() != ids.Count)
        {
            return ResultadoActa<ActaDto>.Invalido("Hay un ítem repetido en el acta.");
        }

        var actuales = acta.Detalles.ToDictionary(detalle => detalle.Id);
        if (ids.Any(id => !actuales.ContainsKey(id)))
        {
            return ResultadoActa<ActaDto>.Invalido("Hay un ítem que no pertenece al acta.");
        }

        var propias = acta.Detalles
            .Where(detalle => detalle.TipoUsuarioId == actor.TipoUsuarioId)
            .Select(detalle => detalle.Id)
            .ToHashSet();
        var conservadas = ids.Where(propias.Contains).ToHashSet();
        foreach (var detalle in acta.Detalles.Where(detalle => propias.Contains(detalle.Id) && !conservadas.Contains(detalle.Id)).ToList())
        {
            acta.Detalles.Remove(detalle);
            _context.ActaDetalles.Remove(detalle);
        }

        foreach (var linea in solicitud.Detalles)
        {
            if (linea.Id is > 0)
            {
                var detalle = actuales[linea.Id.Value];
                if (detalle.TipoUsuarioId == actor.TipoUsuarioId)
                {
                    CopiarDetalle(detalle, linea);
                }
            }
            else
            {
                acta.Detalles.Add(CrearDetalle(linea, actor.TipoUsuarioId));
            }
        }

        var conflictoPerfil = await GuardarAsync(cancellationToken);
        if (conflictoPerfil is not null)
        {
            return ResultadoActa<ActaDto>.Conflicto(conflictoPerfil);
        }

        return ResultadoActa<ActaDto>.Ok((await CargarAsync(acta.NumeroActa, cancellationToken))!);
    }

    private async Task<string?> GuardarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            return "Otra persona modificó esta acta.";
        }
    }

    private static bool VersionDistinta(byte[]? actual, string? token) =>
        ReglasPresupuesto.VersionDistinta(actual, token);


    private static ActaDetalle CrearDetalle(LineaActaSolicitud linea, int tipoUsuarioId)
    {
        var detalle = new ActaDetalle
        {
            Descripcion = (linea.Descripcion ?? "").Trim(),
            TipoUsuarioId = tipoUsuarioId,
            Aprobado = true
        };
        CopiarDetalle(detalle, linea);
        return detalle;
    }

    private static void CopiarDetalle(ActaDetalle detalle, LineaActaSolicitud linea)
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


}
