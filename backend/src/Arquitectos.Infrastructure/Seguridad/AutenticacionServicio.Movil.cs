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
        var guardado = await _context.Dispositivos
            .SingleOrDefaultAsync(item => item.Id == dispositivo.Id, cancellationToken);
        if (guardado is not null && guardado.UsuarioId != usuario.Id)
        {
            return ResultadoMovil<LoginMovilRespuesta>.Prohibido("El dispositivo está registrado a otro usuario.");
        }

        var ahora = _reloj.GetUtcNow().UtcDateTime;
        if (guardado is null)
        {
            guardado = new Dispositivo
            {
                Id = dispositivo.Id,
                UsuarioId = usuario.Id,
                Nombre = dispositivo.Nombre.Trim(),
                Plataforma = dispositivo.Plataforma.Trim(),
                VersionApp = dispositivo.VersionApp.Trim(),
                RegistradoEn = ahora
            };
            _context.Dispositivos.Add(guardado);
        }
        else
        {
            guardado.Nombre = dispositivo.Nombre.Trim();
            guardado.Plataforma = dispositivo.Plataforma.Trim();
            guardado.VersionApp = dispositivo.VersionApp.Trim();
            guardado.RevocadoEn = null;
        }

        var credenciales = await AbrirAsync(usuario, ip, agente, cancellationToken, guardado.Id);
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
}
