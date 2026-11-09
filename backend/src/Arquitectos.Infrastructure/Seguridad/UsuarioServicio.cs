using Arquitectos.Application.Usuarios;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Seguridad;

public sealed class UsuarioServicio : IUsuarioServicio
{
    private readonly ArquitectosDbContext _context;
    private readonly PasswordHasher<string> _hasher = new();

    public UsuarioServicio(ArquitectosDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<UsuarioDto>> ListarAsync(bool incluirInactivos, CancellationToken cancellationToken)
    {
        var consulta = _context.Usuarios.AsNoTracking();
        if (!incluirInactivos)
        {
            consulta = consulta.Where(usuario => usuario.Activo);
        }

        return await consulta
            .OrderBy(usuario => usuario.NombreUsuario)
            .Select(usuario => new UsuarioDto(
                usuario.Id,
                usuario.NombreUsuario,
                usuario.Nombre,
                usuario.TipoUsuarioId,
                usuario.TipoUsuario.Nombre,
                usuario.Activo))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TipoUsuarioDto>> ListarTiposAsync(CancellationToken cancellationToken)
    {
        return await _context.TiposUsuario
            .AsNoTracking()
            .OrderBy(tipo => tipo.Nombre)
            .Select(tipo => new TipoUsuarioDto(tipo.Id, tipo.Nombre))
            .ToListAsync(cancellationToken);
    }

    public async Task<UsuarioDto?> ObtenerAsync(int id, CancellationToken cancellationToken)
    {
        return await _context.Usuarios
            .AsNoTracking()
            .Where(usuario => usuario.Id == id)
            .Select(usuario => new UsuarioDto(
                usuario.Id,
                usuario.NombreUsuario,
                usuario.Nombre,
                usuario.TipoUsuarioId,
                usuario.TipoUsuario.Nombre,
                usuario.Activo))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ResultadoUsuario<UsuarioDto>> CrearAsync(
        CrearUsuarioSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        var nombreUsuario = solicitud.Usuario.Trim();
        var nombre = solicitud.Nombre.Trim();
        if (!await TipoExisteAsync(solicitud.TipoUsuarioId, cancellationToken))
        {
            return ResultadoUsuario<UsuarioDto>.Invalido(MensajesUsuario.Tipo);
        }

        var inactivo = await _context.Usuarios
            .FirstOrDefaultAsync(usuario => usuario.NombreUsuario == nombreUsuario && !usuario.Activo, cancellationToken);
        if (inactivo is not null)
        {
            inactivo.NombreUsuario = nombreUsuario;
            inactivo.Nombre = nombre;
            inactivo.TipoUsuarioId = solicitud.TipoUsuarioId;
            inactivo.Activo = true;
            inactivo.PasswordHash = _hasher.HashPassword(nombreUsuario, solicitud.Password);
            if (!await GuardarAsync(cancellationToken))
            {
                return ResultadoUsuario<UsuarioDto>.Conflicto(MensajesUsuario.Duplicado);
            }

            return ResultadoUsuario<UsuarioDto>.Ok(await MapearAsync(inactivo.Id, cancellationToken));
        }

        if (await _context.Usuarios.AnyAsync(usuario => usuario.NombreUsuario == nombreUsuario, cancellationToken))
        {
            return ResultadoUsuario<UsuarioDto>.Conflicto(MensajesUsuario.Duplicado);
        }

        var entidad = new Usuario
        {
            NombreUsuario = nombreUsuario,
            Nombre = nombre,
            TipoUsuarioId = solicitud.TipoUsuarioId,
            Activo = true,
            PasswordHash = _hasher.HashPassword(nombreUsuario, solicitud.Password)
        };
        _context.Usuarios.Add(entidad);
        if (!await GuardarAsync(cancellationToken))
        {
            return ResultadoUsuario<UsuarioDto>.Conflicto(MensajesUsuario.Duplicado);
        }

        return ResultadoUsuario<UsuarioDto>.Ok(await MapearAsync(entidad.Id, cancellationToken));
    }

    public async Task<ResultadoUsuario<UsuarioDto>> ActualizarAsync(
        int id,
        ActualizarUsuarioSolicitud solicitud,
        int usuarioActualId,
        CancellationToken cancellationToken)
    {
        var entidad = await _context.Usuarios.SingleOrDefaultAsync(usuario => usuario.Id == id, cancellationToken);
        if (entidad is null)
        {
            return ResultadoUsuario<UsuarioDto>.NoEncontrado(MensajesUsuario.NoEncontrado);
        }

        if (solicitud.Activo == false && id == usuarioActualId)
        {
            return ResultadoUsuario<UsuarioDto>.Invalido(MensajesUsuario.Propio);
        }

        var nombreUsuario = solicitud.Usuario.Trim();
        if (await _context.Usuarios.AnyAsync(
                usuario => usuario.NombreUsuario == nombreUsuario && usuario.Id != id,
                cancellationToken))
        {
            return ResultadoUsuario<UsuarioDto>.Conflicto(MensajesUsuario.Duplicado);
        }

        if (!await TipoExisteAsync(solicitud.TipoUsuarioId, cancellationToken))
        {
            return ResultadoUsuario<UsuarioDto>.Invalido(MensajesUsuario.Tipo);
        }

        entidad.NombreUsuario = nombreUsuario;
        entidad.Nombre = solicitud.Nombre.Trim();
        entidad.TipoUsuarioId = solicitud.TipoUsuarioId;
        if (solicitud.Activo is bool activo)
        {
            entidad.Activo = activo;
        }

        if (!string.IsNullOrWhiteSpace(solicitud.Password))
        {
            entidad.PasswordHash = _hasher.HashPassword(nombreUsuario, solicitud.Password);
        }

        if (!await GuardarAsync(cancellationToken))
        {
            return ResultadoUsuario<UsuarioDto>.Conflicto(MensajesUsuario.Duplicado);
        }

        return ResultadoUsuario<UsuarioDto>.Ok(await MapearAsync(entidad.Id, cancellationToken));
    }

    public async Task<ResultadoUsuario<bool>> EliminarAsync(
        int id,
        int usuarioActualId,
        CancellationToken cancellationToken)
    {
        if (id == usuarioActualId)
        {
            return ResultadoUsuario<bool>.Invalido(MensajesUsuario.Propio);
        }

        var entidad = await _context.Usuarios.SingleOrDefaultAsync(usuario => usuario.Id == id, cancellationToken);
        if (entidad is null || !entidad.Activo)
        {
            return ResultadoUsuario<bool>.NoEncontrado(MensajesUsuario.NoEncontrado);
        }

        entidad.Activo = false;
        await _context.SaveChangesAsync(cancellationToken);
        return ResultadoUsuario<bool>.Ok(true);
    }

    public async Task<ResultadoUsuario<bool>> CambiarContrasenaAsync(
        int usuarioId,
        CambiarContrasenaSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        var usuario = await _context.Usuarios
            .SingleOrDefaultAsync(item => item.Id == usuarioId && item.Activo, cancellationToken);
        if (usuario is null)
        {
            return ResultadoUsuario<bool>.NoEncontrado(MensajesUsuario.NoEncontrado);
        }

        var verificacion = _hasher.VerifyHashedPassword(
            usuario.NombreUsuario,
            usuario.PasswordHash,
            solicitud.ContrasenaActual);
        if (verificacion == PasswordVerificationResult.Failed)
        {
            return ResultadoUsuario<bool>.Invalido(MensajesUsuario.ActualIncorrecta);
        }

        if (solicitud.ContrasenaActual == solicitud.ContrasenaNueva)
        {
            return ResultadoUsuario<bool>.Invalido(MensajesUsuario.Distinta);
        }

        usuario.PasswordHash = _hasher.HashPassword(usuario.NombreUsuario, solicitud.ContrasenaNueva);
        await _context.SaveChangesAsync(cancellationToken);
        return ResultadoUsuario<bool>.Ok(true);
    }

    private async Task<bool> TipoExisteAsync(int tipoUsuarioId, CancellationToken cancellationToken) =>
        await _context.TiposUsuario.AnyAsync(tipo => tipo.Id == tipoUsuarioId, cancellationToken);

    private async Task<UsuarioDto> MapearAsync(int id, CancellationToken cancellationToken) =>
        (await ObtenerAsync(id, cancellationToken))!;

    private async Task<bool> GuardarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException excepcion) when (EsViolacionUnica(excepcion))
        {
            return false;
        }
    }

    private static bool EsViolacionUnica(DbUpdateException excepcion)
    {
        for (var actual = excepcion.InnerException; actual is not null; actual = actual.InnerException)
        {
            if (actual is SqlException sql && sql.Number is 2601 or 2627)
            {
                return true;
            }
        }

        return false;
    }
}
