using Microsoft.AspNetCore.Http;

namespace Arquitectos.Infrastructure.Auditoria;

internal static class OrigenAuditoria
{
    public const string Web = "web";

    public const string Movil = "movil";

    public static string De(HttpContext? http) =>
        http?.Request.Path.StartsWithSegments("/api/movil") == true ? Movil : Web;
}
