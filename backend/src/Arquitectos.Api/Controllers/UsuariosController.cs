using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Arquitectos.Application;
using Arquitectos.Application.Usuarios;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arquitectos.Api.Controllers;

[ApiController]
[Authorize(Roles = "Administrador")]
[Route("api/usuarios")]
public sealed class UsuariosController : ControllerBase
{
    private readonly IUsuarioServicio _servicio;
    private readonly IValidator<CrearUsuarioSolicitud> _validadorCrear;
    private readonly IValidator<ActualizarUsuarioSolicitud> _validadorActualizar;

    public UsuariosController(
        IUsuarioServicio servicio,
        IValidator<CrearUsuarioSolicitud> validadorCrear,
        IValidator<ActualizarUsuarioSolicitud> validadorActualizar)
    {
        _servicio = servicio;
        _validadorCrear = validadorCrear;
        _validadorActualizar = validadorActualizar;
    }

    [HttpGet]
    public async Task<ActionResult<PaginaDto<UsuarioDto>>> Listar(
        [FromQuery] bool incluirInactivos,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = Paginacion.TamanoDefecto,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _servicio.ListarAsync(incluirInactivos, cancellationToken, pagina, tamano));
    }

    [HttpGet("tipos")]
    public async Task<ActionResult<IReadOnlyList<TipoUsuarioDto>>> Tipos(CancellationToken cancellationToken)
    {
        return Ok(await _servicio.ListarTiposAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken cancellationToken)
    {
        var usuario = await _servicio.ObtenerAsync(id, cancellationToken);
        return usuario is null
            ? Problema(EstadoUsuario.NoEncontrado, MensajesUsuario.NoEncontrado)
            : Ok(usuario);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(CrearUsuarioSolicitud solicitud, CancellationToken cancellationToken)
    {
        var validacion = await ValidarAsync(_validadorCrear, solicitud, cancellationToken);
        if (validacion is not null)
        {
            return validacion;
        }

        var resultado = await _servicio.CrearAsync(solicitud, cancellationToken);
        var error = Traducir(resultado);
        return error ?? Created($"/api/usuarios/{resultado.Valor!.Id}", resultado.Valor);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(
        int id,
        ActualizarUsuarioSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        var validacion = await ValidarAsync(_validadorActualizar, solicitud, cancellationToken);
        if (validacion is not null)
        {
            return validacion;
        }

        var usuarioId = UsuarioActualId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.ActualizarAsync(id, solicitud, usuarioId.Value, cancellationToken);
        var error = Traducir(resultado);
        return error ?? Ok(resultado.Valor);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken cancellationToken)
    {
        var usuarioId = UsuarioActualId();
        if (usuarioId is null)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.EliminarAsync(id, usuarioId.Value, cancellationToken);
        var error = Traducir(resultado);
        return error ?? NoContent();
    }

    private async Task<IActionResult?> ValidarAsync<T>(
        IValidator<T> validador,
        T solicitud,
        CancellationToken cancellationToken)
    {
        var validacion = await validador.ValidateAsync(solicitud, cancellationToken);
        if (validacion.IsValid)
        {
            return null;
        }

        foreach (var error in validacion.Errors)
        {
            ModelState.AddModelError(ACamelCase(error.PropertyName), error.ErrorMessage);
        }

        return ValidationProblem(ModelState);
    }

    private IActionResult? Traducir<T>(ResultadoUsuario<T> resultado) =>
        resultado.Estado == EstadoUsuario.Ok ? null : Problema(resultado.Estado, resultado.Mensaje);

    private IActionResult Problema(EstadoUsuario estado, string? mensaje) =>
        estado switch
        {
            EstadoUsuario.NoEncontrado => Problem(
                title: "No encontrado",
                detail: mensaje,
                statusCode: StatusCodes.Status404NotFound),
            EstadoUsuario.Conflicto => Problem(
                title: "Registro duplicado",
                detail: mensaje,
                statusCode: StatusCodes.Status409Conflict),
            _ => Problem(
                title: mensaje,
                detail: mensaje,
                statusCode: StatusCodes.Status400BadRequest)
        };

    private int? UsuarioActualId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(sub, out var id) ? id : null;
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
