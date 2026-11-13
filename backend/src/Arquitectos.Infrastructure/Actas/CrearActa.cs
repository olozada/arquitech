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
    public async Task<ResultadoActa<ActaCreadaDto>> CrearAsync(
        CrearActaSolicitud solicitud,
        int? usuarioId,
        CancellationToken cancellationToken)
    {
        var error = await ValidarSolicitud(solicitud, cancellationToken);
        if (error is not null)
        {
            return ResultadoActa<ActaCreadaDto>.Invalido(error);
        }

        int? usuarioRegistroId = usuarioId is int id &&
            await _context.Usuarios.AnyAsync(usuario => usuario.Id == id && usuario.Activo, cancellationToken)
            ? id
            : null;

        var acta = new Acta
        {
            FechaInicial = solicitud.FechaInicial,
            FechaFinal = solicitud.FechaFinal,
            ArquitectoId = solicitud.ArquitectoId,
            RevisorId = solicitud.RevisorId,
            ContratistaId = solicitud.ContratistaId,
            Observacion = Texto(solicitud.Observacion),
            FechaRegistro = DateTime.UtcNow,
            UsuarioRegistroId = usuarioRegistroId
        };

        foreach (var linea in solicitud.Detalles)
        {
            acta.Detalles.Add(CrearDetalle(linea, PerfilesUsuario.Arquitecto));
        }

        await using var transaccion = await _context.Database.BeginTransactionAsync(cancellationToken);
        _context.Actas.Add(acta);
        await _context.SaveChangesAsync(cancellationToken);
        await transaccion.CommitAsync(cancellationToken);

        return ResultadoActa<ActaCreadaDto>.Ok(new ActaCreadaDto(acta.NumeroActa));
    }


    private async Task<string?> ValidarSolicitud(CrearActaSolicitud solicitud, CancellationToken cancellationToken)
    {
        var fechas = ValidarRango(solicitud.FechaInicial, solicitud.FechaFinal);
        if (fechas is not null)
        {
            return fechas;
        }

        solicitud.Detalles ??= [];

        if (!await ExisteActivo(_context.Arquitectos, solicitud.ArquitectoId, cancellationToken))
        {
            return CrearActaValidador.MensajeArquitecto;
        }

        if (!await ExisteActivo(_context.Revisores, solicitud.RevisorId, cancellationToken))
        {
            return CrearActaValidador.MensajeRevisor;
        }

        if (!await ExisteActivo(_context.Contratistas, solicitud.ContratistaId, cancellationToken))
        {
            return CrearActaValidador.MensajeContratista;
        }

        foreach (var linea in solicitud.Detalles)
        {
            var errorLinea = await ValidadorLineaActa.ValidarAsync(_context, linea, cancellationToken);
            if (errorLinea is not null)
            {
                return errorLinea;
            }
        }

        return null;
    }


}
