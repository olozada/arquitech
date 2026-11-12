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
    public async Task<ResultadoActa<ActaDto>> CambiarCierreAsync(
        int numeroActa,
        bool cerrada,
        int usuarioId,
        CancellationToken cancellationToken,
        string? rowVersion = null)
    {
        var actor = await CargarActorAsync(usuarioId, cancellationToken);
        if (actor is null || actor.TipoUsuarioId != PerfilesUsuario.Administrador)
        {
            return ResultadoActa<ActaDto>.Invalido("Solo el administrador puede cerrar o abrir el acta.");
        }

        var acta = await _context.Actas
            .SingleOrDefaultAsync(item => item.NumeroActa == numeroActa, cancellationToken);
        if (acta is null)
        {
            return ResultadoActa<ActaDto>.NoEncontrado("No se encontró el acta.");
        }

        if (VersionDistinta(acta.RowVersion, rowVersion))
        {
            return ResultadoActa<ActaDto>.Conflicto("Otra persona modificó esta acta.");
        }

        if (acta.Cerrada != cerrada)
        {
            await _auditoria.RegistrarAsync(
                "Acta",
                acta.NumeroActa.ToString(),
                cerrada ? AccionAuditoria.Cerrar : AccionAuditoria.Reabrir,
                usuarioId,
                CambiosAuditoria.Json("Cerrada", acta.Cerrada, cerrada),
                cancellationToken);
        }

        acta.Cerrada = cerrada;
        var conflictoCierre = await GuardarAsync(cancellationToken);
        if (conflictoCierre is not null)
        {
            return ResultadoActa<ActaDto>.Conflicto(conflictoCierre);
        }

        return ResultadoActa<ActaDto>.Ok((await CargarAsync(numeroActa, cancellationToken))!);
    }


}
