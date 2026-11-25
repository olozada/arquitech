using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Arquitectos.Application.Auth;
using Arquitectos.Application.Facturas;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arquitectos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/facturas")]
public sealed class FacturasController : ControllerBase
{
    private readonly IFacturaServicio _servicio;
    private readonly IValidator<CrearFacturaSolicitud> _validador;

    public FacturasController(IFacturaServicio servicio, IValidator<CrearFacturaSolicitud> validador)
    {
        _servicio = servicio;
        _validador = validador;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ConsultaFacturasDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Consultar(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int? proveedorId,
        [FromQuery] int? construccionId,
        [FromQuery] string? numero,
        [FromQuery] string? nombreProveedor,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _servicio.ConsultarAsync(
            desde, hasta, proveedorId, construccionId, numero, nombreProveedor, pagina, tamano, cancellationToken);
        return Traducir(resultado);
    }

    [HttpGet("items")]
    [ProducesResponseType(typeof(InformeFacturasDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Items(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int? proveedorId,
        [FromQuery] int? construccionId,
        [FromQuery] string? vista,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 20,
        CancellationToken cancellationToken = default)
    {
        var resultado = await _servicio.ConsultarItemsAsync(
            desde, hasta, proveedorId, construccionId, Agrupado(vista), pagina, tamano, false, cancellationToken);
        return Traducir(resultado);
    }

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpGet("items/excel")]
    public async Task<IActionResult> Excel(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int? proveedorId,
        [FromQuery] int? construccionId,
        [FromQuery] string? vista,
        CancellationToken cancellationToken)
    {
        var resultado = await _servicio.ExcelItemsAsync(
            desde, hasta, proveedorId, construccionId, Agrupado(vista), cancellationToken);
        if (resultado.Estado != EstadoFactura.Ok)
        {
            return Traducir(resultado);
        }

        return File(
            resultado.Valor!,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "ReporteFacturas.xlsx");
    }

    [HttpGet("items/informe")]
    public async Task<IActionResult> Informe(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int? proveedorId,
        [FromQuery] int? construccionId,
        [FromQuery] string? vista,
        CancellationToken cancellationToken)
    {
        var resultado = await _servicio.PdfItemsAsync(
            desde, hasta, proveedorId, construccionId, Agrupado(vista), cancellationToken);
        if (resultado.Estado != EstadoFactura.Ok)
        {
            return Traducir(resultado);
        }

        return File(resultado.Valor!, "application/pdf", "ReporteFacturas.pdf");
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(FacturaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int id, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.ObtenerAsync(id, cancellationToken);
        return Traducir(resultado);
    }

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpPost]
    [ProducesResponseType(typeof(FacturaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear(CrearFacturaSolicitud solicitud, CancellationToken cancellationToken)
    {
        var validacion = await _validador.ValidateAsync(solicitud, cancellationToken);
        if (!validacion.IsValid)
        {
            return Problema(validacion.Errors[0].ErrorMessage);
        }

        if (LeerUsuarioId() is not int usuarioId)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.CrearAsync(solicitud, usuarioId, cancellationToken);
        return Traducir(resultado);
    }

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(FacturaDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Actualizar(int id, CrearFacturaSolicitud solicitud, CancellationToken cancellationToken)
    {
        var validacion = await _validador.ValidateAsync(solicitud, cancellationToken);
        if (!validacion.IsValid)
        {
            return Problema(validacion.Errors[0].ErrorMessage);
        }

        var resultado = await _servicio.ActualizarAsync(id, solicitud, cancellationToken);
        return Traducir(resultado);
    }

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Eliminar(int id, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.EliminarAsync(id, cancellationToken);
        return resultado.Estado == EstadoFactura.Ok ? NoContent() : Traducir(resultado);
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> Pdf(int id, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.PdfAsync(id, cancellationToken);
        if (resultado.Estado != EstadoFactura.Ok)
        {
            return Traducir(resultado);
        }

        return File(resultado.Valor!, "application/pdf", $"Factura-{id}.pdf");
    }

    private static bool Agrupado(string? vista) =>
        !string.Equals(vista?.Trim(), "detallado", StringComparison.OrdinalIgnoreCase);

    private int? LeerUsuarioId()
    {
        var valor = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(valor, out var id) ? id : null;
    }

    private IActionResult Problema(string mensaje) =>
        Problem(title: "Solicitud inválida", detail: mensaje, statusCode: StatusCodes.Status400BadRequest);

    private IActionResult Traducir<T>(ResultadoFactura<T> resultado) =>
        resultado.Estado switch
        {
            EstadoFactura.Ok => Ok(resultado.Valor),
            EstadoFactura.NoEncontrado => Problem(
                title: "No encontrado",
                detail: resultado.Mensaje,
                statusCode: StatusCodes.Status404NotFound),
            EstadoFactura.Conflicto => Problem(
                title: "Conflicto",
                detail: resultado.Mensaje,
                statusCode: StatusCodes.Status409Conflict),
            _ => Problema(resultado.Mensaje ?? "Solicitud inválida")
        };
}
