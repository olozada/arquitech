using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Arquitectos.Application;
using Arquitectos.Application.Actas;
using Arquitectos.Application.Auth;
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
        [FromQuery] int? revisorId,
        [FromQuery] int? contratistaId,
        [FromQuery] int? construccionId,
        [FromQuery] string? estado,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = Paginacion.TamanoDefecto,
        [FromQuery] string? numero = null,
        [FromQuery] string? nombreArquitecto = null,
        [FromQuery] string? nombreRevisor = null,
        [FromQuery] string? nombreContratista = null,
        [FromQuery] string? orden = null,
        [FromQuery] bool descendente = true,
        CancellationToken cancellationToken = default)
    {
        if (LeerUsuarioId() is not int usuarioId)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.ConsultarAsync(
            desde,
            hasta,
            arquitectoId,
            revisorId,
            contratistaId,
            construccionId,
            Cerrada(estado),
            usuarioId,
            cancellationToken,
            pagina,
            tamano,
            numero,
            nombreArquitecto,
            nombreRevisor,
            nombreContratista,
            orden,
            descendente);
        return Traducir(resultado);
    }

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Exportar(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int? arquitectoId,
        [FromQuery] int? revisorId,
        [FromQuery] int? contratistaId,
        [FromQuery] int? construccionId,
        [FromQuery] string? estado,
        CancellationToken cancellationToken)
    {
        if (LeerUsuarioId() is not int usuarioId)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.ExportarAsync(
            desde,
            hasta,
            arquitectoId,
            revisorId,
            contratistaId,
            construccionId,
            Cerrada(estado),
            usuarioId,
            cancellationToken);
        if (resultado.Estado != EstadoActa.Ok)
        {
            return Traducir(resultado);
        }

        return File(
            resultado.Valor!,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "ReporteActas.xlsx");
    }

    [HttpGet("items")]
    [ProducesResponseType(typeof(InformeItemsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Items(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int? arquitectoId,
        [FromQuery] int? revisorId,
        [FromQuery] int? contratistaId,
        [FromQuery] int? construccionId,
        [FromQuery] string? estado,
        [FromQuery] string? vista,
        [FromQuery] string? orden,
        [FromQuery] string? direccion,
        [FromQuery] bool romperPorActa,
        [FromQuery] int pagina = 0,
        [FromQuery] int tamano = 0,
        CancellationToken cancellationToken = default)
    {
        if (LeerUsuarioId() is not int usuarioId)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.ConsultarItemsAsync(
            desde,
            hasta,
            arquitectoId,
            revisorId,
            contratistaId,
            construccionId,
            Cerrada(estado),
            vista?.Trim().Equals("agrupado", StringComparison.OrdinalIgnoreCase) == true,
            usuarioId,
            orden,
            direccion?.Trim().Equals("desc", StringComparison.OrdinalIgnoreCase) == true,
            romperPorActa,
            cancellationToken,
            pagina,
            tamano);
        return Traducir(resultado);
    }

    [HttpGet("materiales")]
    [ProducesResponseType(typeof(InformeMaterialesDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Materiales(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int? arquitectoId,
        [FromQuery] int? revisorId,
        [FromQuery] int? contratistaId,
        [FromQuery] int? construccionId,
        [FromQuery] int? proveedorId,
        [FromQuery] string? estado,
        [FromQuery] string? vista,
        [FromQuery] string? orden,
        [FromQuery] string? direccion,
        [FromQuery] int pagina = 0,
        [FromQuery] int tamano = 0,
        CancellationToken cancellationToken = default)
    {
        if (LeerUsuarioId() is not int usuarioId)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.ConsultarMaterialesAsync(
            desde,
            hasta,
            arquitectoId,
            revisorId,
            contratistaId,
            construccionId,
            Cerrada(estado),
            proveedorId,
            usuarioId,
            cancellationToken,
            orden,
            direccion?.Trim().Equals("desc", StringComparison.OrdinalIgnoreCase) == true,
            VistaAgrupada(vista),
            pagina,
            tamano);
        return Traducir(resultado);
    }

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpGet("materiales/excel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MaterialesExcel(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int? arquitectoId,
        [FromQuery] int? revisorId,
        [FromQuery] int? contratistaId,
        [FromQuery] int? construccionId,
        [FromQuery] int? proveedorId,
        [FromQuery] string? estado,
        [FromQuery] string? vista,
        CancellationToken cancellationToken)
    {
        if (LeerUsuarioId() is not int usuarioId)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.ExportarMaterialesAsync(
            desde,
            hasta,
            arquitectoId,
            revisorId,
            contratistaId,
            construccionId,
            Cerrada(estado),
            proveedorId,
            usuarioId,
            cancellationToken,
            VistaAgrupada(vista));
        if (resultado.Estado != EstadoActa.Ok)
        {
            return Traducir(resultado);
        }

        return File(
            resultado.Valor!,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "ReporteMateriales.xlsx");
    }

    [HttpGet("materiales/informe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> MaterialesInforme(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int? arquitectoId,
        [FromQuery] int? revisorId,
        [FromQuery] int? contratistaId,
        [FromQuery] int? construccionId,
        [FromQuery] int? proveedorId,
        [FromQuery] string? estado,
        [FromQuery] string? orden,
        [FromQuery] string? direccion,
        [FromQuery] string? vista,
        CancellationToken cancellationToken)
    {
        if (LeerUsuarioId() is not int usuarioId)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.ImprimirMaterialesAsync(
            desde,
            hasta,
            arquitectoId,
            revisorId,
            contratistaId,
            construccionId,
            Cerrada(estado),
            proveedorId,
            usuarioId,
            orden,
            direccion?.Trim().Equals("desc", StringComparison.OrdinalIgnoreCase) == true,
            cancellationToken,
            VistaAgrupada(vista));
        if (resultado.Estado != EstadoActa.Ok)
        {
            return Traducir(resultado);
        }

        Response.Headers.ContentDisposition = "inline; filename=\"InformeMateriales.pdf\"";
        return File(resultado.Valor!, "application/pdf");
    }

    [HttpGet("informe")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Informe(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int? arquitectoId,
        [FromQuery] int? revisorId,
        [FromQuery] int? contratistaId,
        [FromQuery] int? construccionId,
        [FromQuery] string? estado,
        [FromQuery] string? vista,
        [FromQuery] string? orden,
        [FromQuery] string? direccion,
        [FromQuery] bool romperPorActa,
        CancellationToken cancellationToken)
    {
        if (LeerUsuarioId() is not int usuarioId)
        {
            return Unauthorized();
        }

        var agrupado = vista?.Trim().ToLowerInvariant() switch
        {
            "agrupado" => true,
            "detallado" => false,
            _ => (bool?)null
        };
        if (agrupado is null)
        {
            return Problem(
                title: "Solicitud inválida",
                detail: "Debe indicar si el informe es detallado o agrupado.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var resultado = await _servicio.ImprimirItemsAsync(
            desde,
            hasta,
            arquitectoId,
            revisorId,
            contratistaId,
            construccionId,
            Cerrada(estado),
            agrupado.Value,
            usuarioId,
            orden,
            direccion?.Trim().Equals("desc", StringComparison.OrdinalIgnoreCase) == true,
            romperPorActa,
            cancellationToken);
        if (resultado.Estado != EstadoActa.Ok)
        {
            return Traducir(resultado);
        }

        var nombre = agrupado.Value ? "InformeAgrupado.pdf" : "InformeDetallado.pdf";
        Response.Headers.ContentDisposition = $"inline; filename=\"{nombre}\"";
        return File(resultado.Valor!, "application/pdf");
    }

    [HttpGet("{numero:int}")]
    [ProducesResponseType(typeof(ActaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtener(int numero, CancellationToken cancellationToken)
    {
        if (LeerUsuarioId() is not int usuarioId)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.ObtenerAsync(numero, usuarioId, cancellationToken);
        return Traducir(resultado);
    }

    [HttpGet("{numero:int}/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Pdf(int numero, CancellationToken cancellationToken)
    {
        if (LeerUsuarioId() is not int usuarioId)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.GenerarPdfAsync(numero, usuarioId, cancellationToken);
        if (resultado.Estado != EstadoActa.Ok)
        {
            return Traducir(resultado);
        }

        Response.Headers.ContentDisposition = $"inline; filename=\"ImpresionActa-{numero}.pdf\"";
        return File(resultado.Valor!, "application/pdf");
    }

    [Authorize(Policy = Politicas.SoloAdministrador)]
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

    [HttpPut("{numero:int}")]
    [ProducesResponseType(typeof(ActaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Actualizar(
        int numero,
        CrearActaSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        var validacion = await _validador.ValidateAsync(solicitud, cancellationToken);
        if (!validacion.IsValid)
        {
            return Problem(
                title: "Solicitud inválida",
                detail: validacion.Errors[0].ErrorMessage,
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (LeerUsuarioId() is not int usuarioId)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.ActualizarAsync(numero, solicitud, usuarioId, cancellationToken);
        return Traducir(resultado);
    }

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpPut("{numero:int}/cierre")]
    [ProducesResponseType(typeof(ActaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CambiarCierre(
        int numero,
        CierreActaSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        if (LeerUsuarioId() is not int usuarioId)
        {
            return Unauthorized();
        }

        var resultado = await _servicio.CambiarCierreAsync(
            numero,
            solicitud.Cerrada,
            usuarioId,
            cancellationToken,
            solicitud.RowVersion);
        return Traducir(resultado);
    }

    private static bool VistaAgrupada(string? vista) =>
        !string.Equals(vista?.Trim(), "detallado", StringComparison.OrdinalIgnoreCase);

    private static bool? Cerrada(string? estado) =>
        estado?.Trim().ToLowerInvariant() switch
        {
            "cerrada" => true,
            "abierta" => false,
            _ => null
        };

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
            EstadoActa.Conflicto => Problem(
                title: "Conflicto",
                detail: resultado.Mensaje,
                statusCode: StatusCodes.Status409Conflict),
            _ => Problem(
                title: "Solicitud inválida",
                detail: resultado.Mensaje,
                statusCode: StatusCodes.Status400BadRequest)
        };
}
