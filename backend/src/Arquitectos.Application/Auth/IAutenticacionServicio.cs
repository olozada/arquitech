namespace Arquitectos.Application.Auth;

public interface IAutenticacionServicio
{
    Task<CredencialesSesion?> IniciarSesion(LoginSolicitud solicitud, string? ip, string? agente, CancellationToken cancellationToken);

    Task<CredencialesSesion?> RenovarAsync(string tokenRenovacion, string? ip, string? agente, CancellationToken cancellationToken);

    Task SalirAsync(long sesionId, CancellationToken cancellationToken);
}

public interface IRevocacionSesiones
{
    Task RevocarUsuarioAsync(int usuarioId, CancellationToken cancellationToken);
}
