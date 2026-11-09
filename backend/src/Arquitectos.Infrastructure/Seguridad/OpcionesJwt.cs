namespace Arquitectos.Infrastructure.Seguridad;

public sealed class OpcionesJwt
{
    public const string Seccion = "Jwt";

    public string Issuer { get; set; } = "";

    public string Audience { get; set; } = "";

    public string Key { get; set; } = "";

    public int ExpirationMinutes { get; set; } = 480;
}
