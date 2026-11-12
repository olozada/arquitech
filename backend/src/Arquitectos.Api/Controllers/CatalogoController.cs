using Arquitectos.Application.Catalogos;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arquitectos.Api.Controllers;

[ApiController]
[Authorize]
public abstract class CatalogoController<TEntidad, TDto, TSolicitud> : ControllerBase
    where TDto : class, IConId
{
    private readonly ICatalogoServicio<TEntidad, TDto, TSolicitud> _servicio;
    private readonly IValidator<TSolicitud> _validador;

    protected CatalogoController(
        ICatalogoServicio<TEntidad, TDto, TSolicitud> servicio,
        IValidator<TSolicitud> validador)
    {
        _servicio = servicio;
        _validador = validador;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TDto>>> Listar(
        [FromQuery] bool incluirInactivos,
        CancellationToken cancellationToken)
    {
        var items = await _servicio.ListarAsync(incluirInactivos, cancellationToken);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken cancellationToken)
    {
        var item = await _servicio.ObtenerAsync(id, cancellationToken);
        return item is null ? NoEncontrado() : Ok(item);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<IActionResult> Crear(TSolicitud solicitud, CancellationToken cancellationToken)
    {
        var validacion = Validar(solicitud);
        if (validacion is not null)
        {
            return validacion;
        }

        var resultado = await _servicio.CrearAsync(solicitud, cancellationToken);
        var error = Traducir(resultado);
        if (error is not null)
        {
            return error;
        }

        return Created($"{Request.Path}/{resultado.Valor!.Id}", resultado.Valor);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, TSolicitud solicitud, CancellationToken cancellationToken)
    {
        var validacion = Validar(solicitud);
        if (validacion is not null)
        {
            return validacion;
        }

        var resultado = await _servicio.ActualizarAsync(id, solicitud, cancellationToken);
        var error = Traducir(resultado);
        return error ?? Ok(resultado.Valor);
    }

    [Authorize(Roles = "Administrador")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.EliminarAsync(id, cancellationToken);
        if (!resultado.Encontrado)
        {
            return NoEncontrado(resultado.Mensaje);
        }

        return NoContent();
    }

    private IActionResult? Validar(TSolicitud solicitud)
    {
        var validacion = _validador.Validate(solicitud);
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

    private IActionResult? Traducir<T>(ResultadoCatalogo<T> resultado)
    {
        if (!resultado.Encontrado)
        {
            return NoEncontrado(resultado.Mensaje);
        }

        if (resultado.Duplicado)
        {
            return Problem(
                title: "Registro duplicado",
                detail: resultado.Mensaje,
                statusCode: StatusCodes.Status409Conflict);
        }

        return null;
    }

    private IActionResult NoEncontrado(string? detalle = null) =>
        Problem(
            title: "No encontrado",
            detail: detalle ?? "No se encontró el registro.",
            statusCode: StatusCodes.Status404NotFound);

    private static string ACamelCase(string nombre)
    {
        if (string.IsNullOrEmpty(nombre) || char.IsLower(nombre[0]))
        {
            return nombre;
        }

        return char.ToLowerInvariant(nombre[0]) + nombre[1..];
    }
}
