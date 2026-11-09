using Arquitectos.Application.Dashboard;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arquitectos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardServicio _servicio;
    private readonly IValidator<FiltroDashboard> _validador;

    public DashboardController(IDashboardServicio servicio, IValidator<FiltroDashboard> validador)
    {
        _servicio = servicio;
        _validador = validador;
    }

    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Obtener(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int? construccionId,
        [FromQuery] int? contratistaId,
        [FromQuery] int? arquitectoId,
        CancellationToken cancellationToken)
    {
        var filtro = CrearFiltro(desde, hasta, construccionId, contratistaId, arquitectoId);
        var error = await ValidarAsync(filtro, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        return Ok(await _servicio.ObtenerAsync(filtro, cancellationToken));
    }

    [HttpGet("alertas")]
    [ProducesResponseType(typeof(PaginaAlertasDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Alertas(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int? construccionId,
        [FromQuery] int? contratistaId,
        [FromQuery] int? arquitectoId,
        [FromQuery] string? tipo,
        [FromQuery] string? severidad,
        [FromQuery] int pagina = 1,
        CancellationToken cancellationToken = default)
    {
        var filtro = CrearFiltro(desde, hasta, construccionId, contratistaId, arquitectoId);
        var error = await ValidarAsync(filtro, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        return Ok(await _servicio.AlertasAsync(filtro, tipo, severidad, pagina, cancellationToken));
    }

    [HttpGet("precio-unitario")]
    [ProducesResponseType(typeof(IReadOnlyList<PuntoPrecioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PrecioUnitario(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int? conceptoId,
        [FromQuery] int? materialId,
        CancellationToken cancellationToken)
    {
        if (conceptoId is null or <= 0 && materialId is null or <= 0)
        {
            return Problem(
                title: "Solicitud inválida",
                detail: "Debe seleccionar un concepto o un material.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var filtro = CrearFiltro(desde, hasta, null, null, null);
        var error = await ValidarAsync(filtro, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        return Ok(await _servicio.PrecioUnitarioAsync(filtro.Desde, filtro.Hasta, conceptoId, materialId, cancellationToken));
    }

    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Exportar(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int? construccionId,
        [FromQuery] int? contratistaId,
        [FromQuery] int? arquitectoId,
        CancellationToken cancellationToken)
    {
        var filtro = CrearFiltro(desde, hasta, construccionId, contratistaId, arquitectoId);
        var error = await ValidarAsync(filtro, cancellationToken);
        if (error is not null)
        {
            return error;
        }

        var archivo = await _servicio.ExportarAsync(filtro, cancellationToken);
        return File(
            archivo,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "Dashboard.xlsx");
    }

    private static FiltroDashboard CrearFiltro(
        DateOnly? desde,
        DateOnly? hasta,
        int? construccionId,
        int? contratistaId,
        int? arquitectoId) =>
        new()
        {
            Desde = desde ?? default,
            Hasta = hasta ?? default,
            ConstruccionId = construccionId is > 0 ? construccionId : null,
            ContratistaId = contratistaId is > 0 ? contratistaId : null,
            ArquitectoId = arquitectoId is > 0 ? arquitectoId : null
        };

    private async Task<IActionResult?> ValidarAsync(FiltroDashboard filtro, CancellationToken cancellationToken)
    {
        var validacion = await _validador.ValidateAsync(filtro, cancellationToken);
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

    private static string ACamelCase(string nombre)
    {
        if (string.IsNullOrEmpty(nombre) || char.IsLower(nombre[0]))
        {
            return nombre;
        }

        return char.ToLowerInvariant(nombre[0]) + nombre[1..];
    }
}
