using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Arquitectos.Application.Auth;
using Arquitectos.Application.Movil;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Arquitectos.Api.Controllers.Movil;

[ApiController]
[Route("api/movil/v1/paquete")]
[Authorize(Policy = Politicas.PerfilOperativo)]
[ExigirVersionApp]
public sealed class PaqueteMovilController : ControllerBase
{
    private readonly IGeneradorPaqueteMovil _generador;

    public PaqueteMovilController(IGeneradorPaqueteMovil generador)
    {
        _generador = generador;
    }

    [HttpGet]
    [EnableRateLimiting("paquete")]
    public async Task<IActionResult> Obtener(CancellationToken cancellationToken)
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(sub, out var usuarioId))
        {
            return Unauthorized();
        }

        long? sesionId = long.TryParse(User.FindFirstValue(ClaimsArquitectos.Sesion), out var sesion) ? sesion : null;
        var resultado = await _generador.GenerarAsync(usuarioId, sesionId, cancellationToken);
        if (resultado.Estado != EstadoPaquete.Ok || resultado.Contenido is null)
        {
            return resultado.Estado switch
            {
                EstadoPaquete.Prohibido => Problem(title: resultado.Mensaje, detail: resultado.Mensaje, statusCode: StatusCodes.Status403Forbidden),
                EstadoPaquete.DemasiadoGrande => Problem(title: resultado.Mensaje, detail: resultado.Mensaje, statusCode: StatusCodes.Status507InsufficientStorage),
                _ => Problem(title: resultado.Mensaje, detail: resultado.Mensaje, statusCode: StatusCodes.Status401Unauthorized)
            };
        }

        Response.Headers["X-Paquete-Esquema"] = resultado.Esquema.ToString(CultureInfo.InvariantCulture);
        Response.Headers["X-Paquete-Generado"] = resultado.GeneradoEn.ToString("yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", CultureInfo.InvariantCulture);
        Response.Headers["X-Paquete-Sha256"] = resultado.Sha256;
        return File(resultado.Contenido, "application/gzip", "arquitectos.sqlite.gz");
    }
}
