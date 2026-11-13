using System.Security.Claims;
using Arquitectos.Application.Auth;
using Arquitectos.Application.Movil;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Arquitectos.Api.Controllers.Movil;

[ApiController]
[Route("api/movil/v1/auth")]
[Authorize(Policy = Politicas.PerfilOperativo)]
[ExigirVersionApp]
public sealed class AuthMovilController : ControllerBase
{
    private readonly IAutenticacionMovil _autenticacion;
    private readonly IAutenticacionServicio _sesiones;
    private readonly IValidator<LoginMovilSolicitud> _validadorLogin;
    private readonly IValidator<RenovarMovilSolicitud> _validadorRenovar;

    public AuthMovilController(
        IAutenticacionMovil autenticacion,
        IAutenticacionServicio sesiones,
        IValidator<LoginMovilSolicitud> validadorLogin,
        IValidator<RenovarMovilSolicitud> validadorRenovar)
    {
        _autenticacion = autenticacion;
        _sesiones = sesiones;
        _validadorLogin = validadorLogin;
        _validadorRenovar = validadorRenovar;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginMovilSolicitud solicitud, CancellationToken cancellationToken)
    {
        var error = await Validar(_validadorLogin, solicitud, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        var resultado = await _autenticacion.IniciarAsync(solicitud, Ip(), Agente(), cancellationToken);
        return Traducir(resultado);
    }

    [HttpPost("renovar")]
    [AllowAnonymous]
    public async Task<IActionResult> Renovar(RenovarMovilSolicitud solicitud, CancellationToken cancellationToken)
    {
        var error = await Validar(_validadorRenovar, solicitud, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        var resultado = await _autenticacion.RenovarAsync(solicitud, Ip(), Agente(), cancellationToken);
        return Traducir(resultado);
    }

    [HttpPost("salir")]
    public async Task<IActionResult> Salir(CancellationToken cancellationToken)
    {
        var sid = User.FindFirstValue(ClaimsArquitectos.Sesion);
        if (long.TryParse(sid, out var sesionId))
        {
            await _sesiones.SalirAsync(sesionId, cancellationToken);
        }

        return NoContent();
    }

    private IActionResult Traducir<T>(ResultadoMovil<T> resultado) =>
        resultado.Estado switch
        {
            EstadoMovil.Ok => Ok(resultado.Valor),
            EstadoMovil.Prohibido => Problem(
                title: resultado.Mensaje,
                detail: resultado.Mensaje,
                statusCode: StatusCodes.Status403Forbidden),
            EstadoMovil.DispositivoRevocado => new ObjectResult(new
            {
                codigo = "dispositivo_revocado",
                detail = resultado.Mensaje
            })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            },
            _ => Problem(
                title: resultado.Mensaje,
                detail: resultado.Mensaje,
                statusCode: StatusCodes.Status401Unauthorized)
        };

    private async Task<IActionResult?> Validar<T>(IValidator<T> validador, T solicitud, CancellationToken cancellationToken)
    {
        var validacion = await validador.ValidateAsync(solicitud, cancellationToken);
        if (validacion.IsValid)
        {
            return null;
        }

        foreach (var fallo in validacion.Errors)
        {
            ModelState.AddModelError(ACamelCase(fallo.PropertyName), fallo.ErrorMessage);
        }

        return ValidationProblem(ModelState);
    }

    private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string Agente() => Request.Headers.UserAgent.ToString();

    private static string ACamelCase(string nombre)
    {
        if (string.IsNullOrEmpty(nombre) || char.IsLower(nombre[0]))
        {
            return nombre;
        }

        return char.ToLowerInvariant(nombre[0]) + nombre[1..];
    }
}
