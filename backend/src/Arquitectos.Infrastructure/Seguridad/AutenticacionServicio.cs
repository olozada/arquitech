using System.Security.Cryptography;
using System.Text;
using Arquitectos.Application.Auth;
using Arquitectos.Application.Movil;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Seguridad;

public sealed partial class AutenticacionServicio : IAutenticacionServicio, IRevocacionSesiones, IAutenticacionMovil
{
    private const int FallosParaBloquear = 5;

    private static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(15);

    private static readonly TimeSpan DuracionRenovacion = TimeSpan.FromDays(7);

    private readonly ArquitectosDbContext _context;
    private readonly EmisorJwt _emisor;
    private readonly TimeProvider _reloj;
    private readonly ValidadorSesion _validador;
    private readonly PasswordHasher<string> _hasher = new();

    public AutenticacionServicio(
        ArquitectosDbContext context,
        EmisorJwt emisor,
        TimeProvider reloj,
        ValidadorSesion validador)
    {
        _context = context;
        _emisor = emisor;
        _reloj = reloj;
        _validador = validador;
    }

    public async Task<CredencialesSesion?> IniciarSesion(
        LoginSolicitud solicitud,
        string? ip,
        string? agente,
        CancellationToken cancellationToken)
    {
        var usuario = await ComprobarAsync(solicitud.Usuario, solicitud.Password, cancellationToken);
        return usuario is null ? null : await AbrirAsync(usuario, ip, agente, cancellationToken);
    }

    public async Task<CredencialesSesion?> RenovarAsync(
        string tokenRenovacion,
        string? ip,
        string? agente,
        CancellationToken cancellationToken)
    {
        var hash = Hash(tokenRenovacion);
        var sesion = await _context.SesionesUsuario
            .Include(item => item.Usuario)
            .ThenInclude(usuario => usuario.TipoUsuario)
            .SingleOrDefaultAsync(item => item.HashToken == hash, cancellationToken);
        return sesion is null ? null : await RotarAsync(sesion, ip, agente, cancellationToken);
    }

    public async Task SalirAsync(long sesionId, CancellationToken cancellationToken)
    {
        var sesion = await _context.SesionesUsuario
            .SingleOrDefaultAsync(item => item.Id == sesionId, cancellationToken);
        if (sesion is null || sesion.RevocadoEn is not null)
        {
            return;
        }

        sesion.RevocadoEn = _reloj.GetUtcNow().UtcDateTime;
        await _context.SaveChangesAsync(cancellationToken);
        _validador.Invalidar(sesion.UsuarioId);
    }

    public async Task RevocarUsuarioAsync(int usuarioId, CancellationToken cancellationToken)
    {
        var ahora = _reloj.GetUtcNow().UtcDateTime;
        await _context.SesionesUsuario
            .Where(sesion => sesion.UsuarioId == usuarioId && sesion.RevocadoEn == null)
            .ExecuteUpdateAsync(sesion => sesion.SetProperty(item => item.RevocadoEn, ahora), cancellationToken);
        _validador.Invalidar(usuarioId);
    }

    private async Task<Usuario?> ComprobarAsync(string usuarioNombre, string? password, CancellationToken cancellationToken)
    {
        var nombreUsuario = usuarioNombre.Trim();
        if (nombreUsuario.Length == 0 || string.IsNullOrEmpty(password))
        {
            return null;
        }

        var usuario = await _context.Usuarios
            .Include(item => item.TipoUsuario)
            .SingleOrDefaultAsync(item => item.NombreUsuario == nombreUsuario, cancellationToken);
        if (usuario is null || !usuario.Activo)
        {
            return null;
        }

        var ahora = _reloj.GetUtcNow().UtcDateTime;
        if (usuario.BloqueadoHasta is DateTime hasta && hasta > ahora)
        {
            return null;
        }

        if (usuario.BloqueadoHasta is not null)
        {
            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;
        }

        var verificacion = _hasher.VerifyHashedPassword(usuario.NombreUsuario, usuario.PasswordHash, password);
        if (verificacion == PasswordVerificationResult.Failed)
        {
            usuario.IntentosFallidos++;
            if (usuario.IntentosFallidos >= FallosParaBloquear)
            {
                usuario.BloqueadoHasta = ahora.Add(DuracionBloqueo);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return null;
        }

        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        return usuario;
    }

    private async Task<CredencialesSesion?> RotarAsync(
        SesionUsuario sesion,
        string? ip,
        string? agente,
        CancellationToken cancellationToken)
    {
        if (sesion.RevocadoEn is not null)
        {
            await RevocarUsuarioAsync(sesion.UsuarioId, cancellationToken);
            return null;
        }

        var ahora = _reloj.GetUtcNow().UtcDateTime;
        if (sesion.Expira <= ahora || !sesion.Usuario.Activo ||
            (sesion.Usuario.BloqueadoHasta is DateTime hasta && hasta > ahora))
        {
            return null;
        }

        sesion.RevocadoEn = ahora;
        return await AbrirAsync(sesion.Usuario, ip, agente, cancellationToken, sesion.DispositivoId);
    }

    private async Task<CredencialesSesion> AbrirAsync(
        Usuario usuario,
        string? ip,
        string? agente,
        CancellationToken cancellationToken,
        Guid? dispositivoId = null)
    {
        var ahora = _reloj.GetUtcNow().UtcDateTime;
        var tokenRenovacion = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var sesion = new SesionUsuario
        {
            UsuarioId = usuario.Id,
            HashToken = Hash(tokenRenovacion),
            Expira = ahora.Add(DuracionRenovacion),
            CreadoEn = ahora,
            Ip = Recortar(ip, 64),
            UserAgent = Recortar(agente, 256),
            DispositivoId = dispositivoId
        };
        _context.SesionesUsuario.Add(sesion);
        await _context.SaveChangesAsync(cancellationToken);

        var acceso = _emisor.Emitir(
            usuario.Id,
            usuario.NombreUsuario,
            usuario.Nombre,
            usuario.TipoUsuarioId,
            usuario.TipoUsuario.Nombre,
            sesion.Id);
        return new CredencialesSesion(
            new LoginRespuesta(usuario.Nombre, usuario.TipoUsuario.Nombre, usuario.DebeCambiarContrasena),
            acceso,
            tokenRenovacion);
    }

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string? Recortar(string? texto, int maximo)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        var limpio = texto.Trim();
        return limpio.Length <= maximo ? limpio : limpio[..maximo];
    }
}
