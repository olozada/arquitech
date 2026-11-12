using System.Security.Claims;
using Serilog.Core;
using Serilog.Events;

namespace Arquitectos.Api.Configuracion;

public sealed class UsuarioIdEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _http = new HttpContextAccessor();

    public void Enrich(LogEvent evento, ILogEventPropertyFactory propiedades)
    {
        var usuario = _http.HttpContext?.User;
        if (usuario?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var id = usuario.FindFirst("sub")?.Value
            ?? usuario.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        evento.AddPropertyIfAbsent(propiedades.CreateProperty("UsuarioId", id));
    }
}
