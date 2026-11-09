namespace Arquitectos.Application.Auth;

public interface IAutenticacionServicio
{
    Task<LoginRespuesta?> IniciarSesion(LoginSolicitud solicitud, CancellationToken cancellationToken);
}
