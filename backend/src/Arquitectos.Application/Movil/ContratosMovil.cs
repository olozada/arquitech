namespace Arquitectos.Application.Movil;

public sealed class LoginMovilSolicitud
{
    public string Usuario { get; set; } = "";

    public string Contrasena { get; set; } = "";

    public DispositivoMovilSolicitud? Dispositivo { get; set; }
}

public sealed class DispositivoMovilSolicitud
{
    public Guid Id { get; set; }

    public string Nombre { get; set; } = "";

    public string Plataforma { get; set; } = "";

    public string VersionApp { get; set; } = "";
}

public sealed class RenovarMovilSolicitud
{
    public string RefreshToken { get; set; } = "";

    public Guid DispositivoId { get; set; }
}

public sealed record UsuarioMovilDto(
    int Id,
    string Nombre,
    int TipoUsuarioId,
    int? ArquitectoId,
    int? ContratistaId,
    int? RevisorId);

public sealed record LoginMovilRespuesta(
    string AccessToken,
    DateTime ExpiraEn,
    string RefreshToken,
    UsuarioMovilDto Usuario,
    bool DebeCambiarContrasena);

public sealed record ParTokensMovil(string AccessToken, DateTime ExpiraEn, string RefreshToken);

public sealed record DispositivoDto(
    Guid Id,
    string Nombre,
    string Plataforma,
    string VersionApp,
    DateTime RegistradoEn,
    DateTime? UltimaSincronizacion,
    DateTime? RevocadoEn);

public enum EstadoMovil
{
    Ok,
    Invalido,
    Prohibido,
    DispositivoRevocado
}

public sealed class ResultadoMovil<T>
{
    public EstadoMovil Estado { get; private init; }

    public string? Mensaje { get; private init; }

    public T? Valor { get; private init; }

    public static ResultadoMovil<T> Ok(T valor) => new() { Estado = EstadoMovil.Ok, Valor = valor };

    public static ResultadoMovil<T> Invalido(string mensaje) =>
        new() { Estado = EstadoMovil.Invalido, Mensaje = mensaje };

    public static ResultadoMovil<T> Prohibido(string mensaje) =>
        new() { Estado = EstadoMovil.Prohibido, Mensaje = mensaje };

    public static ResultadoMovil<T> DispositivoRevocado(string mensaje) =>
        new() { Estado = EstadoMovil.DispositivoRevocado, Mensaje = mensaje };
}

public interface IAutenticacionMovil
{
    Task<ResultadoMovil<LoginMovilRespuesta>> IniciarAsync(
        LoginMovilSolicitud solicitud,
        string? ip,
        string? agente,
        CancellationToken cancellationToken);

    Task<ResultadoMovil<ParTokensMovil>> RenovarAsync(
        RenovarMovilSolicitud solicitud,
        string? ip,
        string? agente,
        CancellationToken cancellationToken);
}

public interface IDispositivosMovil
{
    Task<IReadOnlyList<DispositivoDto>> ListarAsync(int usuarioId, CancellationToken cancellationToken);

    Task<bool> RevocarAsync(int usuarioId, Guid dispositivoId, int administradorId, CancellationToken cancellationToken);
}
