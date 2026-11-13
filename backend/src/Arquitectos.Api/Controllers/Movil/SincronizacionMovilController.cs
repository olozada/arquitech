using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Arquitectos.Application.Auth;
using Arquitectos.Application.Movil;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arquitectos.Api.Controllers.Movil;

[ApiController]
[Route("api/movil/v1/sincronizar")]
[Authorize(Policy = Politicas.PerfilOperativo)]
[ExigirVersionApp]
public sealed class SincronizacionMovilController : ControllerBase
{
    private readonly ISincronizadorMovil _sincronizador;
    private readonly IValidator<SincronizacionMovilSolicitud> _validador;

    public SincronizacionMovilController(ISincronizadorMovil sincronizador, IValidator<SincronizacionMovilSolicitud> validador)
    {
        _sincronizador = sincronizador;
        _validador = validador;
    }

    [HttpPost]
    public async Task<IActionResult> Sincronizar(SincronizacionMovilSolicitud solicitud, CancellationToken cancellationToken)
    {
        var validacion = await _validador.ValidateAsync(solicitud, cancellationToken);
        if (!validacion.IsValid)
        {
            foreach (var fallo in validacion.Errors)
            {
                ModelState.AddModelError(ACamelCase(fallo.PropertyName), fallo.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(sub, out var usuarioId))
        {
            return Unauthorized();
        }

        long? sesionId = long.TryParse(User.FindFirstValue(ClaimsArquitectos.Sesion), out var sesion) ? sesion : null;
        var resultado = await _sincronizador.SincronizarAsync(usuarioId, sesionId, solicitud, cancellationToken);
        return resultado.Estado switch
        {
            EstadoSincronizacion.Ok => Ok(resultado.Valor),
            EstadoSincronizacion.Prohibido => Problem(title: resultado.Mensaje, detail: resultado.Mensaje, statusCode: StatusCodes.Status403Forbidden),
            EstadoSincronizacion.DispositivoRevocado => new ObjectResult(new { codigo = "dispositivo_revocado", detail = resultado.Mensaje })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            },
            EstadoSincronizacion.SolicitudInvalida => Problem(title: resultado.Mensaje, detail: resultado.Mensaje, statusCode: StatusCodes.Status400BadRequest),
            _ => Problem(title: resultado.Mensaje, detail: resultado.Mensaje, statusCode: StatusCodes.Status401Unauthorized)
        };
    }

    private static string ACamelCase(string nombre)
    {
        if (string.IsNullOrEmpty(nombre) || char.IsLower(nombre[0]))
        {
            return nombre;
        }

        return char.ToLowerInvariant(nombre[0]) + nombre[1..];
    }
}
