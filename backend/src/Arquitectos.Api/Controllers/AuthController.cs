using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Arquitectos.Application.Auth;
using Arquitectos.Application.Usuarios;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        var respuesta = await _autenticacion.IniciarSesion(solicitud, cancellationToken);
        if (respuesta is null)
        {
            return Problem(
                title: "Usuario o contraseña inválidos.",
                detail: "Usuario o contraseña inválidos.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return Ok(respuesta);
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

    private static string ACamelCase(string nombre)
    {
        if (string.IsNullOrEmpty(nombre) || char.IsLower(nombre[0]))
        {
            return nombre;
        }

        return char.ToLowerInvariant(nombre[0]) + nombre[1..];
    }
}
