using Arquitectos.Application.Auth;
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
    private readonly IRevocacionSesiones _revocacion;
    private readonly PasswordHasher<string> _hasher = new();

    public UsuarioServicio(ArquitectosDbContext context, IRevocacionSesiones revocacion)
    {
        _context = context;
        _revocacion = revocacion;
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
                usuario.Activo,
                usuario.ArquitectoId ?? usuario.ContratistaId ?? usuario.RevisorId))
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
                usuario.Activo,
                usuario.ArquitectoId ?? usuario.ContratistaId ?? usuario.RevisorId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<ResultadoUsuario<UsuarioDto>> CrearAsync(
        CrearUsuarioSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        var nombreUsuario = solicitud.Usuario.Trim();
        var nombre = solicitud.Nombre.Trim();
        var perfil = await ValidarPerfilAsync(solicitud.TipoUsuarioId, solicitud.PerfilId, null, cancellationToken);
        if (perfil.Error is not null)
        {
            return ResultadoUsuario<UsuarioDto>.Invalido(perfil.Error);
        }

        var inactivo = await _context.Usuarios
            .FirstOrDefaultAsync(usuario => usuario.NombreUsuario == nombreUsuario && !usuario.Activo, cancellationToken);
        var contrasenaInvalida = PoliticaContrasena.Validar(solicitud.Password, nombreUsuario);
        if (contrasenaInvalida is not null)
        {
            return ResultadoUsuario<UsuarioDto>.Invalido(contrasenaInvalida);
        }

        if (inactivo is not null)
        {
            inactivo.NombreUsuario = nombreUsuario;
            inactivo.Nombre = nombre;
            inactivo.TipoUsuarioId = solicitud.TipoUsuarioId;
            AsignarPerfil(inactivo, solicitud.TipoUsuarioId, perfil.Id);
            inactivo.Activo = true;
            inactivo.DebeCambiarContrasena = true;
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
            ArquitectoId = solicitud.TipoUsuarioId == PerfilesUsuario.Arquitecto ? perfil.Id : null,
            ContratistaId = solicitud.TipoUsuarioId == PerfilesUsuario.Contratista ? perfil.Id : null,
            RevisorId = solicitud.TipoUsuarioId == PerfilesUsuario.Revisor ? perfil.Id : null,
            Activo = true,
            DebeCambiarContrasena = true,
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

        var perfil = await ValidarPerfilAsync(solicitud.TipoUsuarioId, solicitud.PerfilId, id, cancellationToken);
        if (perfil.Error is not null)
        {
            return ResultadoUsuario<UsuarioDto>.Invalido(perfil.Error);
        }

        entidad.NombreUsuario = nombreUsuario;
        entidad.Nombre = solicitud.Nombre.Trim();
        entidad.TipoUsuarioId = solicitud.TipoUsuarioId;
        AsignarPerfil(entidad, solicitud.TipoUsuarioId, perfil.Id);
        if (solicitud.Activo is bool activo)
        {
            entidad.Activo = activo;
        }

        if (!string.IsNullOrWhiteSpace(solicitud.Password))
        {
            var contrasenaInvalida = PoliticaContrasena.Validar(solicitud.Password, nombreUsuario);
            if (contrasenaInvalida is not null)
            {
                return ResultadoUsuario<UsuarioDto>.Invalido(contrasenaInvalida);
            }

            entidad.DebeCambiarContrasena = true;
            entidad.PasswordHash = _hasher.HashPassword(nombreUsuario, solicitud.Password);
        }

        if (!await GuardarAsync(cancellationToken))
        {
            return ResultadoUsuario<UsuarioDto>.Conflicto(MensajesUsuario.Duplicado);
        }

        if (!entidad.Activo)
        {
            await _revocacion.RevocarUsuarioAsync(entidad.Id, cancellationToken);
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
        await _revocacion.RevocarUsuarioAsync(entidad.Id, cancellationToken);
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

        var contrasenaInvalida = PoliticaContrasena.Validar(solicitud.ContrasenaNueva, usuario.NombreUsuario);
        if (contrasenaInvalida is not null)
        {
            return ResultadoUsuario<bool>.Invalido(contrasenaInvalida);
        }

        usuario.PasswordHash = _hasher.HashPassword(usuario.NombreUsuario, solicitud.ContrasenaNueva);
        usuario.DebeCambiarContrasena = false;
        await _context.SaveChangesAsync(cancellationToken);
        return ResultadoUsuario<bool>.Ok(true);
    }

    private async Task<(int? Id, string? Error)> ValidarPerfilAsync(
        int tipoUsuarioId,
        int? perfilId,
        int? usuarioId,
        CancellationToken cancellationToken)
    {
        if (!await _context.TiposUsuario.AnyAsync(tipo => tipo.Id == tipoUsuarioId, cancellationToken))
        {
            return (null, MensajesUsuario.Tipo);
        }

        if (!PerfilesUsuario.EsOperativo(tipoUsuarioId))
        {
            return (null, null);
        }

        if (perfilId is not > 0)
        {
            return (null, MensajesUsuario.Perfil);
        }

        var activo = tipoUsuarioId switch
        {
            PerfilesUsuario.Arquitecto => await _context.Arquitectos.AnyAsync(
                persona => persona.Id == perfilId && persona.Activo, cancellationToken),
            PerfilesUsuario.Contratista => await _context.Contratistas.AnyAsync(
                persona => persona.Id == perfilId && persona.Activo, cancellationToken),
            _ => await _context.Revisores.AnyAsync(
                persona => persona.Id == perfilId && persona.Activo, cancellationToken)
        };
        if (!activo)
        {
            return (null, MensajesUsuario.PerfilInvalido);
        }

        var ocupado = await _context.Usuarios.AnyAsync(usuario =>
            usuario.Activo &&
            usuario.Id != usuarioId &&
            ((tipoUsuarioId == PerfilesUsuario.Arquitecto && usuario.ArquitectoId == perfilId) ||
             (tipoUsuarioId == PerfilesUsuario.Contratista && usuario.ContratistaId == perfilId) ||
             (tipoUsuarioId == PerfilesUsuario.Revisor && usuario.RevisorId == perfilId)),
            cancellationToken);
        return ocupado ? (null, MensajesUsuario.PerfilOcupado) : (perfilId, null);
    }

    private static void AsignarPerfil(Usuario usuario, int tipoUsuarioId, int? perfilId)
    {
        usuario.ArquitectoId = tipoUsuarioId == PerfilesUsuario.Arquitecto ? perfilId : null;
        usuario.ContratistaId = tipoUsuarioId == PerfilesUsuario.Contratista ? perfilId : null;
        usuario.RevisorId = tipoUsuarioId == PerfilesUsuario.Revisor ? perfilId : null;
    }

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
