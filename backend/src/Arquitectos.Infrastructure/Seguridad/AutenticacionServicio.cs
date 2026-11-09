using Arquitectos.Application.Auth;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Seguridad;

public sealed class AutenticacionServicio : IAutenticacionServicio
{
    private readonly ArquitectosDbContext _context;
    private readonly EmisorJwt _emisor;
    private readonly PasswordHasher<string> _hasher = new();

    public AutenticacionServicio(ArquitectosDbContext context, EmisorJwt emisor)
    {
        _context = context;
        _emisor = emisor;
    }

    public async Task<LoginRespuesta?> IniciarSesion(LoginSolicitud solicitud, CancellationToken cancellationToken)
    {
        var nombreUsuario = solicitud.Usuario.Trim();
        if (nombreUsuario.Length == 0 || string.IsNullOrEmpty(solicitud.Password))
        {
            return null;
        }

        var usuario = await _context.Usuarios
            .AsNoTracking()
            .Where(u => u.NombreUsuario == nombreUsuario)
            .Select(u => new
            {
                u.Id,
                u.NombreUsuario,
                u.Nombre,
                u.PasswordHash,
                u.Activo,
                u.TipoUsuarioId,
                TipoUsuario = u.TipoUsuario.Nombre
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (usuario is null || !usuario.Activo)
        {
            return null;
        }

        var verificacion = _hasher.VerifyHashedPassword(usuario.NombreUsuario, usuario.PasswordHash, solicitud.Password);
        if (verificacion == PasswordVerificationResult.Failed)
        {
            return null;
        }

        var token = _emisor.Emitir(
            usuario.Id,
            usuario.NombreUsuario,
            usuario.Nombre,
            usuario.TipoUsuarioId,
            usuario.TipoUsuario);

        return new LoginRespuesta(token, usuario.Nombre, usuario.TipoUsuario);
    }
}
