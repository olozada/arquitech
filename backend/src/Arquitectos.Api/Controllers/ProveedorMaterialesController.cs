using Arquitectos.Application.Auth;
using Arquitectos.Application.ProveedorMateriales;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arquitectos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/proveedores/{proveedorId:int}/materiales")]
public sealed class ProveedorMaterialesController : ControllerBase
{
    private readonly IProveedorMaterialServicio _servicio;
    private readonly IValidator<List<AgregarMaterialSolicitud>> _validadorAgregar;
    private readonly IValidator<List<int>> _validadorQuitar;

    public ProveedorMaterialesController(
        IProveedorMaterialServicio servicio,
        IValidator<List<AgregarMaterialSolicitud>> validadorAgregar,
        IValidator<List<int>> validadorQuitar)
    {
        _servicio = servicio;
        _validadorAgregar = validadorAgregar;
        _validadorQuitar = validadorQuitar;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MaterialAgregadoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Agregados(int proveedorId, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.ListarAgregadosAsync(proveedorId, cancellationToken);
        return Traducir(resultado);
    }

    [HttpGet("disponibles")]
    [ProducesResponseType(typeof(IReadOnlyList<MaterialDisponibleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Disponibles(int proveedorId, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.ListarDisponiblesAsync(proveedorId, cancellationToken);
        return Traducir(resultado);
    }

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Agregar(
        int proveedorId,
        [FromBody] List<AgregarMaterialSolicitud> materiales,
        CancellationToken cancellationToken)
    {
        var error = Validar(_validadorAgregar, materiales);
        if (error is not null)
        {
            return error;
        }

        var resultado = await _servicio.AgregarAsync(proveedorId, materiales, cancellationToken);
        return Traducir(resultado);
    }

    [HttpGet("mapeo")]
    [ProducesResponseType(typeof(MapeoProductosDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Mapeo(int proveedorId, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.ObtenerMapeoAsync(proveedorId, cancellationToken);
        return Traducir(resultado);
    }

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpPost("vista")]
    [RequestSizeLimit(10_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 10_000_000)]
    [ProducesResponseType(typeof(VistaCargueDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Vista(int proveedorId, IFormFile archivo, CancellationToken cancellationToken)
    {
        if (archivo is null || archivo.Length == 0)
        {
            return Problem(title: "Solicitud inválida", detail: "Seleccione un archivo.", statusCode: StatusCodes.Status400BadRequest);
        }

        await using var contenido = archivo.OpenReadStream();
        var resultado = await _servicio.PrevisualizarCargueAsync(proveedorId, contenido, archivo.FileName, cancellationToken);
        return Traducir(resultado);
    }

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpPost("cargar")]
    [RequestSizeLimit(10_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 10_000_000)]
    [ProducesResponseType(typeof(ResultadoCargueDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cargar(
        int proveedorId,
        IFormFile archivo,
        [FromForm] string? codigo,
        [FromForm] string? descripcion,
        [FromForm] string? unidad,
        [FromForm] string? precio,
        CancellationToken cancellationToken)
    {
        if (archivo is null || archivo.Length == 0)
        {
            return Problem(title: "Solicitud inválida", detail: "Seleccione un archivo.", statusCode: StatusCodes.Status400BadRequest);
        }

        var mapeo = new MapeoProductosDto
        {
            Codigo = codigo ?? "",
            Descripcion = descripcion ?? "",
            Unidad = unidad ?? "",
            Precio = precio ?? ""
        };
        await using var contenido = archivo.OpenReadStream();
        var resultado = await _servicio.CargarProductosAsync(proveedorId, contenido, archivo.FileName, mapeo, cancellationToken);
        return Traducir(resultado);
    }

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Quitar(
        int proveedorId,
        [FromBody] List<int> materialIds,
        CancellationToken cancellationToken)
    {
        var error = Validar(_validadorQuitar, materialIds);
        if (error is not null)
        {
            return error;
        }

        var resultado = await _servicio.QuitarAsync(proveedorId, materialIds, cancellationToken);
        return Traducir(resultado);
    }

    private IActionResult? Validar<T>(IValidator<T> validador, T solicitud)
    {
        var validacion = validador.Validate(solicitud);
        if (validacion.IsValid)
        {
            return null;
        }

        return Problem(
            title: "Solicitud inválida",
            detail: validacion.Errors[0].ErrorMessage,
            statusCode: StatusCodes.Status400BadRequest);
    }

    private IActionResult Traducir<T>(ResultadoProveedorMaterial<T> resultado)
    {
        return resultado.Estado switch
        {
            EstadoProveedorMaterial.Ok when resultado.Valor is bool => NoContent(),
            EstadoProveedorMaterial.Ok => Ok(resultado.Valor),
            EstadoProveedorMaterial.NoEncontrado => Problem(
                title: "No encontrado",
                detail: resultado.Mensaje,
                statusCode: StatusCodes.Status404NotFound),
            EstadoProveedorMaterial.Duplicado => Problem(
                title: "Registro duplicado",
                detail: resultado.Mensaje,
                statusCode: StatusCodes.Status409Conflict),
            _ => Problem(
                title: "Solicitud inválida",
                detail: resultado.Mensaje,
                statusCode: StatusCodes.Status400BadRequest)
        };
    }
}
