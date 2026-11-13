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
            return await RevisarAsync(acta, solicitud, actor, cancellationToken);
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
        var cambioLineas = false;
        foreach (var detalle in acta.Detalles.Where(detalle => !conservar.Contains(detalle.Id)).ToList())
        {
            acta.Detalles.Remove(detalle);
            _context.ActaDetalles.Remove(detalle);
            cambioLineas = true;
        }

        foreach (var linea in solicitud.Detalles)
        {
            if (linea.Id is > 0)
            {
                var detalle = actuales[linea.Id.Value];
                CopiarDetalle(detalle, linea);
                if (_context.Entry(detalle).State == EntityState.Modified)
                {
                    cambioLineas = true;
                }
            }
            else
            {
                acta.Detalles.Add(CrearDetalle(linea, PerfilesUsuario.Arquitecto));
                cambioLineas = true;
            }
        }

        if (cambioLineas)
        {
            acta.ModificadaEn = DateTime.UtcNow;
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
        var eliminar = acta.Detalles
            .Where(detalle => propias.Contains(detalle.Id) && !ids.Where(propias.Contains).Contains(detalle.Id))
            .Select(detalle => detalle.Id)
            .ToList();
        var editar = solicitud.Detalles
            .Where(linea => linea.Id is > 0 && propias.Contains(linea.Id.Value) && !LineaActaMapeo.Igual(actuales[linea.Id.Value], linea))
            .ToList();
        var agregar = solicitud.Detalles.Where(linea => linea.Id is not > 0).ToList();
        if (eliminar.Count == 0 && editar.Count == 0 && agregar.Count == 0)
        {
            return ResultadoActa<ActaDto>.Ok((await CargarAsync(acta.NumeroActa, cancellationToken))!);
        }

        await using var transaccion = await _context.Database.BeginTransactionAsync(cancellationToken);
        foreach (var lineaId in eliminar)
        {
            var detalle = actuales[lineaId];
            var resultado = await _operaciones.EliminarAsync(
                acta.NumeroActa,
                lineaId,
                Convert.ToBase64String(detalle.RowVersion),
                actor,
                cancellationToken);
            if (resultado.Estado != EstadoActa.Ok)
            {
                await transaccion.RollbackAsync(cancellationToken);
                _context.ChangeTracker.Clear();
                return Traducir(resultado);
            }
        }

        foreach (var linea in editar)
        {
            var detalle = actuales[linea.Id!.Value];
            var resultado = await _operaciones.EditarAsync(
                acta.NumeroActa,
                detalle.Id,
                Convert.ToBase64String(detalle.RowVersion),
                linea,
                actor,
                cancellationToken);
            if (resultado.Estado != EstadoActa.Ok)
            {
                await transaccion.RollbackAsync(cancellationToken);
                _context.ChangeTracker.Clear();
                return Traducir(resultado);
            }
        }

        foreach (var linea in agregar)
        {
            var resultado = await _operaciones.AgregarAsync(
                acta.NumeroActa,
                Guid.NewGuid(),
                linea,
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

    private static ResultadoActa<ActaDto> Traducir(ResultadoActa<LineaAplicadaDto> resultado) =>
        resultado.Estado switch
        {
            EstadoActa.NoEncontrado => ResultadoActa<ActaDto>.NoEncontrado(resultado.Mensaje ?? "No se encontró el acta."),
            EstadoActa.Conflicto => ResultadoActa<ActaDto>.Conflicto(resultado.Mensaje ?? CrearActaValidador.MensajeConflictoLinea),
            _ => ResultadoActa<ActaDto>.Invalido(resultado.Mensaje ?? "No se pudo guardar la línea.")
        };

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


    private static ActaDetalle CrearDetalle(LineaActaSolicitud linea, int tipoUsuarioId) =>
        LineaActaMapeo.Crear(linea, tipoUsuarioId, null);

    private static void CopiarDetalle(ActaDetalle detalle, LineaActaSolicitud linea) =>
        LineaActaMapeo.Copiar(detalle, linea);


}
