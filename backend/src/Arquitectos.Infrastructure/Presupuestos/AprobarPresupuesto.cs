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
    public async Task<ResultadoOperacion<PresupuestoDetalleDto>> AprobarAsync(int id, int usuarioId, CancellationToken cancellationToken, bool confirmarCantidadesCero = false)
    {
        if (!await EsAdministradorAsync(usuarioId, cancellationToken))
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Prohibido("Solo el administrador puede aprobar.");
        }

        var presupuesto = await CargarAsync(id, cancellationToken);
        if (presupuesto is null)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.NoEncontrado("No se encontró el presupuesto.");
        }

        if (presupuesto.EsPlantilla)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Validacion("Una plantilla no se aprueba.");
        }

        if (presupuesto.Estado != EstadoPresupuesto.Borrador)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Validacion("Solo se aprueba un presupuesto en borrador.");
        }

        var validacion = await ValidarInternoAsync(presupuesto, cancellationToken);
        var bloqueos = validacion.Where(aviso => aviso.Tipo != "CantidadCero").ToList();
        if (bloqueos.Count > 0)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Validacion(string.Join(" ", bloqueos.Select(aviso => aviso.Mensaje)));
        }

        if (validacion.Any(aviso => aviso.Tipo == "CantidadCero") && !confirmarCantidadesCero)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Validacion("Hay cantidades en cero. Confirme para aprobar.");
        }

        await using var transaccion = await _context.Database.BeginTransactionAsync(cancellationToken);
        var anteriores = await _context.Presupuestos
            .Where(item => item.ConstruccionId == presupuesto.ConstruccionId && item.Estado == EstadoPresupuesto.Aprobado && item.Id != id)
            .ToListAsync(cancellationToken);
        foreach (var anterior in anteriores)
        {
            await _auditoria.RegistrarAsync(
                "Presupuesto",
                anterior.Id.ToString(),
                AccionAuditoria.Cerrar,
                usuarioId,
                CambiosAuditoria.Json("Estado", anterior.Estado.ToString(), EstadoPresupuesto.Cerrado.ToString()),
                cancellationToken);
            anterior.Estado = EstadoPresupuesto.Cerrado;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await AplicarCalculoAsync(presupuesto, true, cancellationToken);
        var contexto = await _motor.CargarAsync(cancellationToken);
        foreach (var item in presupuesto.Capitulos.SelectMany(capitulo => capitulo.Items))
        {
            if (item.ApuId is int apuId && MotorCostos.Calcular(apuId, contexto) is ResultadoApu resultado)
            {
                item.DesgloseApuJson = JsonSerializer.Serialize(new { resultado.Total, resultado.Grupos });
            }
        }

        await _auditoria.RegistrarAsync(
            "Presupuesto",
            presupuesto.Id.ToString(),
            AccionAuditoria.Aprobar,
            usuarioId,
            CambiosAuditoria.Json("Estado", presupuesto.Estado.ToString(), EstadoPresupuesto.Aprobado.ToString()),
            cancellationToken);
        presupuesto.Estado = EstadoPresupuesto.Aprobado;
        presupuesto.FechaAprobacion = DateTime.UtcNow;
        presupuesto.UsuarioAprobacionId = usuarioId;
        await _context.SaveChangesAsync(cancellationToken);
        await transaccion.CommitAsync(cancellationToken);
        return await ObtenerAsync(id, usuarioId, cancellationToken);
    }


    public async Task<ResultadoOperacion<PresupuestoDetalleDto>> CerrarAsync(int id, int usuarioId, CancellationToken cancellationToken)
    {
        if (!await EsAdministradorAsync(usuarioId, cancellationToken))
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.Prohibido("Solo el administrador puede cerrar un presupuesto.");
        }

        var presupuesto = await _context.Presupuestos.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (presupuesto is null)
        {
            return ResultadoOperacion<PresupuestoDetalleDto>.NoEncontrado("No se encontró el presupuesto.");
        }

        await _auditoria.RegistrarAsync(
            "Presupuesto",
            presupuesto.Id.ToString(),
            AccionAuditoria.Cerrar,
            usuarioId,
            CambiosAuditoria.Json("Estado", presupuesto.Estado.ToString(), EstadoPresupuesto.Cerrado.ToString()),
            cancellationToken);
        presupuesto.Estado = EstadoPresupuesto.Cerrado;
        await _context.SaveChangesAsync(cancellationToken);
        return await ObtenerAsync(id, usuarioId, cancellationToken);
    }


}
