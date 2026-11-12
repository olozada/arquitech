namespace Arquitectos.Application.Auth;

public sealed record LoginRespuesta(string Nombre, string TipoUsuario, bool DebeCambiarContrasena);

public sealed record CredencialesSesion(LoginRespuesta Datos, string TokenAcceso, string TokenRenovacion);
