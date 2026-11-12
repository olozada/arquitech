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

        if (!await TodosActivos(_context.Construcciones, solicitud.Detalles.Select(linea => linea.ConstruccionId), cancellationToken))
        {
            return CrearActaValidador.MensajeConstruccion;
        }

        if (!await TodosActivos(_context.Conceptos, solicitud.Detalles.Select(linea => linea.ConceptoId), cancellationToken))
        {
            return CrearActaValidador.MensajeConcepto;
        }

        var unidades = solicitud.Detalles
            .Where(linea => linea.UnidadMedidaId is > 0)
            .Select(linea => linea.UnidadMedidaId!.Value)
            .ToList();
        if (unidades.Count > 0 &&
            !await TodosActivos(_context.UnidadesMedida, unidades, cancellationToken))
        {
            return CrearActaValidador.MensajeUnidad;
        }

        var insumos = solicitud.Detalles
            .Where(linea => linea.InsumoId is > 0)
            .Select(linea => linea.InsumoId!.Value)
            .ToList();
        if (insumos.Count > 0 &&
            !await TodosActivos(_context.Insumos, insumos, cancellationToken))
        {
            return "Debe seleccionar un insumo válido.";
        }

        var presupuestoIds = solicitud.Detalles.Where(linea => linea.PresupuestoItemId is > 0).Select(linea => linea.PresupuestoItemId!.Value).Distinct().ToList();
        if (presupuestoIds.Count > 0)
        {
            var validos = await _context.Set<PresupuestoItem>()
                .Where(item => presupuestoIds.Contains(item.Id) && item.Capitulo.Presupuesto.Estado == EstadoPresupuesto.Aprobado)
                .Select(item => new { item.Id, item.Capitulo.Presupuesto.ConstruccionId })
                .ToListAsync(cancellationToken);
            if (solicitud.Detalles.Any(linea => linea.PresupuestoItemId is > 0 && !validos.Any(item => item.Id == linea.PresupuestoItemId && item.ConstruccionId == linea.ConstruccionId)))
            {
                return "El ítem de presupuesto no pertenece a la construcción o el presupuesto no está aprobado.";
            }
        }

        if (solicitud.Detalles.Any(linea => decimal.Round(linea.Cantidad, 4, MidpointRounding.AwayFromZero) <= 0))
        {
            return CrearActaValidador.MensajeCantidad;
        }

        if (solicitud.Detalles.Any(linea => decimal.Round(linea.Precio, 2, MidpointRounding.AwayFromZero) < 0))
        {
            return CrearActaValidador.MensajePrecio;
        }

        return null;
    }


}
