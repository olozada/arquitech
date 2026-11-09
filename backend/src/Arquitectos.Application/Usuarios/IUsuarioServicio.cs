namespace Arquitectos.Application.Usuarios;

public interface IUsuarioServicio
{
    Task<IReadOnlyList<UsuarioDto>> ListarAsync(bool incluirInactivos, CancellationToken cancellationToken);

    Task<IReadOnlyList<TipoUsuarioDto>> ListarTiposAsync(CancellationToken cancellationToken);

    Task<UsuarioDto?> ObtenerAsync(int id, CancellationToken cancellationToken);

    Task<ResultadoUsuario<UsuarioDto>> CrearAsync(CrearUsuarioSolicitud solicitud, CancellationToken cancellationToken);

    Task<ResultadoUsuario<UsuarioDto>> ActualizarAsync(
        int id,
        ActualizarUsuarioSolicitud solicitud,
        int usuarioActualId,
        CancellationToken cancellationToken);

    Task<ResultadoUsuario<bool>> EliminarAsync(int id, int usuarioActualId, CancellationToken cancellationToken);

    Task<ResultadoUsuario<bool>> CambiarContrasenaAsync(
        int usuarioId,
        CambiarContrasenaSolicitud solicitud,
        CancellationToken cancellationToken);
}
