using Arquitectos.Application.Auth;

namespace Arquitectos.Api.Configuracion;

public static class CookiesSesion
{
    public const string Acceso = "arquitectos.acceso";

    public const string Renovacion = "arquitectos.renovacion";

    public const string Encabezado = "X-Requested-With";

    public const string ValorEncabezado = "arquitectos";

    public static void Escribir(HttpContext contexto, CredencialesSesion credenciales)
    {
        contexto.Response.Cookies.Append(Acceso, credenciales.TokenAcceso, OpcionesAcceso());
        contexto.Response.Cookies.Append(Renovacion, credenciales.TokenRenovacion, OpcionesRenovacion());
    }

    public static void Borrar(HttpContext contexto)
    {
        contexto.Response.Cookies.Delete(Acceso, OpcionesAcceso());
        contexto.Response.Cookies.Delete(Renovacion, OpcionesRenovacion());
    }

    private static CookieOptions OpcionesAcceso() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api",
        MaxAge = TimeSpan.FromMinutes(20)
    };

    private static CookieOptions OpcionesRenovacion() => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api/auth/renovar",
        MaxAge = TimeSpan.FromDays(7)
    };
}
