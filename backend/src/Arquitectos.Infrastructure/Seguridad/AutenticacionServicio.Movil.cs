using Arquitectos.Application.Movil;
using Arquitectos.Application.Usuarios;
using Arquitectos.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Seguridad;

public sealed partial class AutenticacionServicio
{
    public async Task<ResultadoMovil<LoginMovilRespuesta>> IniciarAsync(
        LoginMovilSolicitud solicitud,
        string? ip,
        string? agente,
        CancellationToken cancellationToken)
    {
        var usuario = await ComprobarAsync(solicitud.Usuario, solicitud.Contrasena, cancellationToken);
        if (usuario is null)
        {
            return ResultadoMovil<LoginMovilRespuesta>.Invalido("Usuario o contraseña inválidos.");
        }

        if (usuario.TipoUsuarioId == PerfilesUsuario.Administrador)
        {
            await _context.SaveChangesAsync(cancellationToken);
            return ResultadoMovil<LoginMovilRespuesta>.Prohibido("La aplicación móvil no está disponible para el administrador.");
        }

        var dispositivo = solicitud.Dispositivo!;
        if (!await VincularDispositivoAsync(
                usuario,
                dispositivo.Id,
                dispositivo.Nombre,
                dispositivo.Plataforma,
                dispositivo.VersionApp,
                cancellationToken))
        {
            return ResultadoMovil<LoginMovilRespuesta>.Prohibido("El dispositivo está registrado a otro usuario.");
        }

        var credenciales = await AbrirAsync(usuario, ip, agente, cancellationToken, dispositivo.Id);
        return ResultadoMovil<LoginMovilRespuesta>.Ok(new LoginMovilRespuesta(
            credenciales.TokenAcceso,
            _reloj.GetUtcNow().UtcDateTime.AddMinutes(_emisor.Minutos),
            credenciales.TokenRenovacion,
            new UsuarioMovilDto(
                usuario.Id,
                usuario.Nombre,
                usuario.TipoUsuarioId,
                usuario.ArquitectoId,
                usuario.ContratistaId,
                usuario.RevisorId),
            usuario.DebeCambiarContrasena));
    }

    public async Task<ResultadoMovil<ParTokensMovil>> RenovarAsync(
        RenovarMovilSolicitud solicitud,
        string? ip,
        string? agente,
        CancellationToken cancellationToken)
    {
        var hash = Hash(solicitud.RefreshToken);
        var sesion = await _context.SesionesUsuario
            .Include(item => item.Usuario)
            .ThenInclude(usuario => usuario.TipoUsuario)
            .Include(item => item.Dispositivo)
            .SingleOrDefaultAsync(item => item.HashToken == hash, cancellationToken);
        if (sesion is null || sesion.DispositivoId != solicitud.DispositivoId)
        {
            return ResultadoMovil<ParTokensMovil>.Invalido("La sesión no es válida.");
        }

        if (sesion.Dispositivo?.RevocadoEn is not null)
        {
            return ResultadoMovil<ParTokensMovil>.DispositivoRevocado("Este dispositivo fue revocado.");
        }

        var credenciales = await RotarAsync(sesion, ip, agente, cancellationToken);
        if (credenciales is null)
        {
            return ResultadoMovil<ParTokensMovil>.Invalido("La sesión no es válida.");
        }

        return ResultadoMovil<ParTokensMovil>.Ok(new ParTokensMovil(
            credenciales.TokenAcceso,
            _reloj.GetUtcNow().UtcDateTime.AddMinutes(_emisor.Minutos),
            credenciales.TokenRenovacion));
    }

    private async Task<bool> VincularDispositivoAsync(
        Usuario usuario,
        Guid dispositivoId,
        string? nombre,
        string? plataforma,
        string? versionApp,
        CancellationToken cancellationToken)
    {
        if (!PerfilesUsuario.EsOperativo(usuario.TipoUsuarioId))
        {
            return true;
        }

        var guardado = await _context.Dispositivos
            .SingleOrDefaultAsync(item => item.Id == dispositivoId, cancellationToken);
        if (guardado is not null && guardado.UsuarioId != usuario.Id)
        {
            return false;
        }

        var ahora = _reloj.GetUtcNow().UtcDateTime;
        var etiqueta = string.IsNullOrWhiteSpace(nombre) ? "Cliente móvil" : nombre.Trim();
        var sistema = string.IsNullOrWhiteSpace(plataforma) ? "desconocida" : plataforma.Trim();
        var version = string.IsNullOrWhiteSpace(versionApp) ? "0" : versionApp.Trim();
        if (guardado is null)
        {
            _context.Dispositivos.Add(new Dispositivo
            {
                Id = dispositivoId,
                UsuarioId = usuario.Id,
                Nombre = etiqueta,
                Plataforma = sistema,
                VersionApp = version,
                RegistradoEn = ahora
            });
        }
        else
        {
            guardado.Nombre = etiqueta;
            guardado.Plataforma = sistema;
            guardado.VersionApp = version;
            guardado.RevocadoEn = null;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
