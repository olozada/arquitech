using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Arquitectos.Application.Actas;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arquitectos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/actas")]
public sealed class ActasController : ControllerBase
{
    private readonly IActaServicio _servicio;
    private readonly IValidator<CrearActaSolicitud> _validador;

    public ActasController(IActaServicio servicio, IValidator<CrearActaSolicitud> validador)
    {
        _servicio = servicio;
        _validador = validador;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ConsultaActasDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Consultar(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int? arquitectoId,
        CancellationToken cancellationToken)
    {
        var resultado = await _servicio.ConsultarAsync(desde, hasta, arquitectoId, cancellationToken);
        return Traducir(resultado);
    }

    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Exportar(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int? arquitectoId,
        CancellationToken cancellationToken)
    {
        var resultado = await _servicio.ExportarAsync(desde, hasta, arquitectoId, cancellationToken);
        if (resultado.Estado != EstadoActa.Ok)
        {
            return Traducir(resultado);
        }

        return File(
            resultado.Valor!,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "ReporteActas.xlsx");
    }

    [HttpGet("{numero:int}")]
    [ProducesResponseType(typeof(ActaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int numero, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.ObtenerAsync(numero, cancellationToken);
        return Traducir(resultado);
    }

    [HttpGet("{numero:int}/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Pdf(int numero, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.GenerarPdfAsync(numero, cancellationToken);
        if (resultado.Estado != EstadoActa.Ok)
        {
            return Traducir(resultado);
        }

        Response.Headers.ContentDisposition = $"inline; filename=\"ImpresionActa-{numero}.pdf\"";
        return File(resultado.Valor!, "application/pdf");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ActaCreadaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Crear(CrearActaSolicitud solicitud, CancellationToken cancellationToken)
    {
        var validacion = await _validador.ValidateAsync(solicitud, cancellationToken);
        if (!validacion.IsValid)
        {
            return Problem(
                title: "Solicitud inválida",
                detail: validacion.Errors[0].ErrorMessage,
                statusCode: StatusCodes.Status400BadRequest);
        }

        var usuarioId = LeerUsuarioId();
        var resultado = await _servicio.CrearAsync(solicitud, usuarioId, cancellationToken);
        if (resultado.Estado != EstadoActa.Ok)
        {
            return Traducir(resultado);
        }

        return Created($"/api/actas/{resultado.Valor!.NumeroActa}", resultado.Valor);
    }

    private int? LeerUsuarioId()
    {
        var valor = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return int.TryParse(valor, out var id) ? id : null;
    }

    private IActionResult Traducir<T>(ResultadoActa<T> resultado) =>
        resultado.Estado switch
        {
            EstadoActa.Ok => Ok(resultado.Valor),
            EstadoActa.NoEncontrado => Problem(
                title: "No encontrado",
                detail: resultado.Mensaje,
                statusCode: StatusCodes.Status404NotFound),
            _ => Problem(
                title: "Solicitud inválida",
                detail: resultado.Mensaje,
                statusCode: StatusCodes.Status400BadRequest)
        };
}
