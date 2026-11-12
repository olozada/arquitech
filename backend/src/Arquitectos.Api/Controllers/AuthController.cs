using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Arquitectos.Api.Configuracion;
using Arquitectos.Application.Auth;
using Arquitectos.Application.Usuarios;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Arquitectos.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAutenticacionServicio _autenticacion;
    private readonly IUsuarioServicio _usuarios;
    private readonly IValidator<LoginSolicitud> _validador;
    private readonly IValidator<CambiarContrasenaSolicitud> _validadorContrasena;

    public AuthController(
        IAutenticacionServicio autenticacion,
        IUsuarioServicio usuarios,
        IValidator<LoginSolicitud> validador,
        IValidator<CambiarContrasenaSolicitud> validadorContrasena)
    {
        _autenticacion = autenticacion;
        _usuarios = usuarios;
        _validador = validador;
        _validadorContrasena = validadorContrasena;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(LoginRespuesta), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginSolicitud solicitud, CancellationToken cancellationToken)
    {
        var validacion = await _validador.ValidateAsync(solicitud, cancellationToken);
        if (!validacion.IsValid)
        {
            foreach (var error in validacion.Errors)
            {
                ModelState.AddModelError(ACamelCase(error.PropertyName), error.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var credenciales = await _autenticacion.IniciarSesion(solicitud, Ip(), Agente(), cancellationToken);
        if (credenciales is null)
        {
            return Problem(
                title: "Usuario o contraseña inválidos.",
                detail: "Usuario o contraseña inválidos.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        CookiesSesion.Escribir(HttpContext, credenciales);
        return Ok(credenciales.Datos);
    }

    [HttpPost("renovar")]
    [AllowAnonymous]
    public async Task<IActionResult> Renovar(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(CookiesSesion.Renovacion, out var token) ||
            string.IsNullOrWhiteSpace(token))
        {
            CookiesSesion.Borrar(HttpContext);
            return Unauthorized();
        }

        var credenciales = await _autenticacion.RenovarAsync(token, Ip(), Agente(), cancellationToken);
        if (credenciales is null)
        {
            CookiesSesion.Borrar(HttpContext);
            return Unauthorized();
        }

        CookiesSesion.Escribir(HttpContext, credenciales);
        return NoContent();
    }

    [HttpPost("salir")]
    [Authorize]
    public async Task<IActionResult> Salir(CancellationToken cancellationToken)
    {
        var sid = User.FindFirstValue(ClaimsArquitectos.Sesion);
        if (long.TryParse(sid, out var sesionId))
        {
            await _autenticacion.SalirAsync(sesionId, cancellationToken);
        }

        CookiesSesion.Borrar(HttpContext);
        return NoContent();
    }

    [HttpPost("cambiar-contrasena")]
    [Authorize]
    public async Task<IActionResult> CambiarContrasena(
        CambiarContrasenaSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        var validacion = await _validadorContrasena.ValidateAsync(solicitud, cancellationToken);
        if (!validacion.IsValid)
        {
            foreach (var error in validacion.Errors)
            {
                ModelState.AddModelError(ACamelCase(error.PropertyName), error.ErrorMessage);
            }

            return ValidationProblem(ModelState);
        }

        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!int.TryParse(sub, out var usuarioId))
        {
            return Unauthorized();
        }

        var resultado = await _usuarios.CambiarContrasenaAsync(usuarioId, solicitud, cancellationToken);
        if (resultado.Estado == EstadoUsuario.Ok)
        {
            return NoContent();
        }

        if (resultado.Estado == EstadoUsuario.NoEncontrado)
        {
            return Problem(
                title: "No encontrado",
                detail: resultado.Mensaje,
                statusCode: StatusCodes.Status404NotFound);
        }

        return Problem(
            title: resultado.Mensaje,
            detail: resultado.Mensaje,
            statusCode: StatusCodes.Status400BadRequest);
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
