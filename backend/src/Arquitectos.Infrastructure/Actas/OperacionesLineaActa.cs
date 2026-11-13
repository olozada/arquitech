using Arquitectos.Application.Actas;
using Arquitectos.Application.Auditoria;
using Arquitectos.Application.Presupuestos;
using Arquitectos.Application.Usuarios;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Auditoria;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Actas;

public sealed class OperacionesLineaActa : IOperacionesLineaActa
{
    private readonly ArquitectosDbContext _context;
    private readonly IAuditoria _auditoria;

    public OperacionesLineaActa(ArquitectosDbContext context, IAuditoria auditoria)
    {
        _context = context;
        _auditoria = auditoria;
    }

    public Task<ResultadoActa<LineaAplicadaDto>> AgregarAsync(
        int numeroActa,
        Guid idCliente,
        LineaActaSolicitud linea,
        ActorActa actor,
        CancellationToken cancellationToken) =>
        Ejecutar(numeroActa, actor, cancellationToken, async acta =>
        {
            if (actor.TipoUsuarioId == PerfilesUsuario.Revisor)
            {
                return ResultadoActa<LineaAplicadaDto>.Invalido(CrearActaValidador.MensajeRevisorAgrega);
            }

            if (actor.TipoUsuarioId is not (PerfilesUsuario.Arquitecto or PerfilesUsuario.Contratista))
            {
                return ResultadoActa<LineaAplicadaDto>.Invalido(CrearActaValidador.MensajeRevisorNoEdita);
            }

            var error = await ValidadorLineaActa.ValidarAsync(_context, linea, cancellationToken);
            if (error is not null)
            {
                return ResultadoActa<LineaAplicadaDto>.Invalido(error);
            }

            if (idCliente != Guid.Empty &&
                await _context.ActaDetalles.AnyAsync(detalle => detalle.IdCliente == idCliente, cancellationToken))
            {
                return ResultadoActa<LineaAplicadaDto>.Invalido("Ya existe una línea con ese identificador.");
            }

            var detalle = LineaActaMapeo.Crear(linea, actor.TipoUsuarioId, idCliente == Guid.Empty ? null : idCliente);
            detalle.NumeroActa = acta.NumeroActa;
            acta.Detalles.Add(detalle);
            Marcar(acta);
            await _context.SaveChangesAsync(cancellationToken);
            await _context.Entry(detalle).ReloadAsync(cancellationToken);
            return ResultadoActa<LineaAplicadaDto>.Ok(Aplicada(detalle));
        });

    public Task<ResultadoActa<LineaAplicadaDto>> EditarAsync(
        int numeroActa,
        int lineaId,
        string rowVersionLinea,
        LineaActaSolicitud linea,
        ActorActa actor,
        CancellationToken cancellationToken) =>
        Ejecutar(numeroActa, actor, cancellationToken, async acta =>
        {
            var detalle = acta.Detalles.FirstOrDefault(item => item.Id == lineaId);
            if (detalle is null)
            {
                return ResultadoActa<LineaAplicadaDto>.Invalido(CrearActaValidador.MensajeItemAjeno);
            }

            var permiso = PuedeEscribir(actor, detalle);
            if (permiso is not null)
            {
                return permiso;
            }

            if (VersionDistinta(detalle.RowVersion, rowVersionLinea))
            {
                return ResultadoActa<LineaAplicadaDto>.Conflicto(CrearActaValidador.MensajeConflictoLinea);
            }

            var error = await ValidadorLineaActa.ValidarAsync(_context, linea, cancellationToken);
            if (error is not null)
            {
                return ResultadoActa<LineaAplicadaDto>.Invalido(error);
            }

            LineaActaMapeo.Copiar(detalle, linea);
            Marcar(acta);
            await _context.SaveChangesAsync(cancellationToken);
            await _context.Entry(detalle).ReloadAsync(cancellationToken);
            return ResultadoActa<LineaAplicadaDto>.Ok(Aplicada(detalle));
        });

    public Task<ResultadoActa<LineaAplicadaDto>> EliminarAsync(
        int numeroActa,
        int lineaId,
        string rowVersionLinea,
        ActorActa actor,
        CancellationToken cancellationToken) =>
        Ejecutar(numeroActa, actor, cancellationToken, async acta =>
        {
            var detalle = acta.Detalles.FirstOrDefault(item => item.Id == lineaId);
            if (detalle is null)
            {
                return ResultadoActa<LineaAplicadaDto>.Invalido(CrearActaValidador.MensajeItemAjeno);
            }

            var permiso = PuedeEscribir(actor, detalle);
            if (permiso is not null)
            {
                return permiso;
            }

            if (VersionDistinta(detalle.RowVersion, rowVersionLinea))
            {
                return ResultadoActa<LineaAplicadaDto>.Conflicto(CrearActaValidador.MensajeConflictoLinea);
            }

            var aplicada = Aplicada(detalle);
            acta.Detalles.Remove(detalle);
            _context.ActaDetalles.Remove(detalle);
            Marcar(acta);
            await _context.SaveChangesAsync(cancellationToken);
            return ResultadoActa<LineaAplicadaDto>.Ok(aplicada);
        });

    public Task<ResultadoActa<LineaAplicadaDto>> RevisarAsync(
        int numeroActa,
        int lineaId,
        string rowVersionLinea,
        bool aprobado,
        ActorActa actor,
        CancellationToken cancellationToken) =>
        Ejecutar(numeroActa, actor, cancellationToken, async acta =>
        {
            if (actor.TipoUsuarioId != PerfilesUsuario.Revisor)
            {
                return ResultadoActa<LineaAplicadaDto>.Invalido(CrearActaValidador.MensajeSoloRevisor);
            }

            var detalle = acta.Detalles.FirstOrDefault(item => item.Id == lineaId);
            if (detalle is null)
            {
                return ResultadoActa<LineaAplicadaDto>.Invalido(CrearActaValidador.MensajeItemAjeno);
            }

            if (VersionDistinta(detalle.RowVersion, rowVersionLinea))
            {
                return ResultadoActa<LineaAplicadaDto>.Conflicto(CrearActaValidador.MensajeConflictoLinea);
            }

            if (detalle.Aprobado != aprobado)
            {
                await _auditoria.RegistrarAsync(
                    "ActaDetalle",
                    detalle.Id.ToString(),
                    aprobado ? AccionAuditoria.Aprobar : AccionAuditoria.Rechazar,
                    actor.UsuarioId,
                    CambiosAuditoria.Json("Aprobado", detalle.Aprobado, aprobado),
                    cancellationToken);
                detalle.Aprobado = aprobado;
            }

            Marcar(acta);
            await _context.SaveChangesAsync(cancellationToken);
            await _context.Entry(detalle).ReloadAsync(cancellationToken);
            return ResultadoActa<LineaAplicadaDto>.Ok(Aplicada(detalle));
        });

    private async Task<ResultadoActa<LineaAplicadaDto>> Ejecutar(
        int numeroActa,
        ActorActa actor,
        CancellationToken cancellationToken,
        Func<Acta, Task<ResultadoActa<LineaAplicadaDto>>> aplicar)
    {
        try
        {
            var acta = await _context.Actas
                .Include(item => item.Detalles)
                .SingleOrDefaultAsync(item => item.NumeroActa == numeroActa, cancellationToken);
            if (acta is null || !PermisosActa.PuedeVer(
                    actor.TipoUsuarioId,
                    actor.ArquitectoId,
                    actor.ContratistaId,
                    actor.RevisorId,
                    acta.ArquitectoId,
                    acta.ContratistaId,
                    acta.RevisorId))
            {
                return ResultadoActa<LineaAplicadaDto>.NoEncontrado("No se encontró el acta.");
            }

            if (acta.Cerrada)
            {
                return ResultadoActa<LineaAplicadaDto>.Invalido(CrearActaValidador.MensajeCerrada);
            }

            return await aplicar(acta);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ResultadoActa<LineaAplicadaDto>.Conflicto(CrearActaValidador.MensajeConflictoLinea);
        }
    }

    private static ResultadoActa<LineaAplicadaDto>? PuedeEscribir(ActorActa actor, ActaDetalle detalle)
    {
        if (actor.TipoUsuarioId == PerfilesUsuario.Revisor)
        {
            return ResultadoActa<LineaAplicadaDto>.Invalido(CrearActaValidador.MensajeRevisorNoEdita);
        }

        if (actor.TipoUsuarioId is not (PerfilesUsuario.Arquitecto or PerfilesUsuario.Contratista))
        {
            return ResultadoActa<LineaAplicadaDto>.Invalido(CrearActaValidador.MensajeRevisorNoEdita);
        }

        if (detalle.TipoUsuarioId != actor.TipoUsuarioId)
        {
            return ResultadoActa<LineaAplicadaDto>.Invalido(CrearActaValidador.MensajeLineaAjena);
        }

        return null;
    }

    private static void Marcar(Acta acta) => acta.ModificadaEn = DateTime.UtcNow;

    private static LineaAplicadaDto Aplicada(ActaDetalle detalle) =>
        new(
            detalle.Id,
            detalle.IdCliente,
            Convert.ToBase64String(detalle.RowVersion ?? []),
            detalle.Subtotal,
            detalle.Aprobado);

    private static bool VersionDistinta(byte[]? actual, string? token) =>
        string.IsNullOrWhiteSpace(token) || ReglasPresupuesto.VersionDistinta(actual, token);
}
