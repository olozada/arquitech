using Arquitectos.Application.Auditoria;
using Arquitectos.Application.Movil;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Auditoria;
using Arquitectos.Infrastructure.Persistence;
using Arquitectos.Infrastructure.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Movil;

public sealed class DispositivosMovil : IDispositivosMovil
{
    private readonly ArquitectosDbContext _context;
    private readonly IAuditoria _auditoria;
    private readonly ValidadorSesion _validador;
    private readonly TimeProvider _reloj;

    public DispositivosMovil(
        ArquitectosDbContext context,
        IAuditoria auditoria,
        ValidadorSesion validador,
        TimeProvider reloj)
    {
        _context = context;
        _auditoria = auditoria;
        _validador = validador;
        _reloj = reloj;
    }

    public async Task<IReadOnlyList<DispositivoDto>> ListarAsync(int usuarioId, CancellationToken cancellationToken)
    {
        return await _context.Dispositivos
            .AsNoTracking()
            .Where(dispositivo => dispositivo.UsuarioId == usuarioId)
            .OrderByDescending(dispositivo => dispositivo.RegistradoEn)
            .Select(dispositivo => new DispositivoDto(
                dispositivo.Id,
                dispositivo.Nombre,
                dispositivo.Plataforma,
                dispositivo.VersionApp,
                dispositivo.RegistradoEn,
                dispositivo.UltimaSincronizacion,
                dispositivo.RevocadoEn))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RevocarAsync(
        int usuarioId,
        Guid dispositivoId,
        int administradorId,
        CancellationToken cancellationToken)
    {
        var dispositivo = await _context.Dispositivos
            .SingleOrDefaultAsync(item => item.Id == dispositivoId && item.UsuarioId == usuarioId, cancellationToken);
        if (dispositivo is null)
        {
            return false;
        }

        if (dispositivo.RevocadoEn is null)
        {
            var ahora = _reloj.GetUtcNow().UtcDateTime;
            dispositivo.RevocadoEn = ahora;
            await _auditoria.RegistrarAsync(
                "Dispositivo",
                dispositivo.Id.ToString(),
                AccionAuditoria.Modificar,
                administradorId,
                CambiosAuditoria.Json("RevocadoEn", null, ahora),
                cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await _context.SesionesUsuario
                .Where(sesion => sesion.DispositivoId == dispositivo.Id && sesion.RevocadoEn == null)
                .ExecuteUpdateAsync(sesion => sesion.SetProperty(item => item.RevocadoEn, ahora), cancellationToken);
            _validador.Invalidar(usuarioId);
        }

        return true;
    }
}
