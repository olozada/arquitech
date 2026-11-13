using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Arquitectos.Application;
using Arquitectos.Application.Auth;
using Arquitectos.Application.Presupuestos;
using Arquitectos.Infrastructure.Presupuestos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Arquitectos.Api.Controllers;

[ApiController]
[Authorize(Policy = Politicas.SoloAdministrador)]
[Route("api/insumos")]
public sealed class InsumosController : ControllerBase
{
    private readonly IInsumoServicio _servicio;

    public InsumosController(IInsumoServicio servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? tipo,
        [FromQuery] bool incluirInactivos,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = Paginacion.TamanoDefecto,
        [FromQuery] string? texto = null,
        CancellationToken cancellationToken = default) =>
        Ok(await _servicio.ListarAsync(tipo, incluirInactivos, cancellationToken, pagina, tamano, texto));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken cancellationToken)
    {
        var insumo = await _servicio.ObtenerAsync(id, cancellationToken);
        return insumo is null ? NotFound() : Ok(insumo);
    }

    [HttpPost]
    public Task<IActionResult> Crear(InsumoSolicitud solicitud, CancellationToken cancellationToken) =>
        Guardar(null, solicitud, cancellationToken);

    [HttpPut("{id:int}")]
    public Task<IActionResult> Actualizar(int id, InsumoSolicitud solicitud, CancellationToken cancellationToken) =>
        Guardar(id, solicitud, cancellationToken);

    [HttpGet("excel")]
    public async Task<IActionResult> Excel([FromQuery] string? tipo, CancellationToken cancellationToken)
    {
        var contenido = await _servicio.ExcelAsync(tipo, cancellationToken);
        return File(contenido, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "insumos.xlsx");
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, [FromQuery] bool confirmar, CancellationToken cancellationToken) =>
        Traducir(await _servicio.EliminarAsync(id, confirmar, cancellationToken));

    [HttpGet("{id:int}/historial")]
    public async Task<IActionResult> Historial(int id, CancellationToken cancellationToken) =>
        Ok(await _servicio.HistorialAsync(id, cancellationToken));

    [HttpGet("{id:int}/precio-proveedor")]
    public async Task<IActionResult> Oferta(int id, CancellationToken cancellationToken)
    {
        var oferta = await _servicio.OfertaProveedorAsync(id, cancellationToken);
        return oferta is null ? NotFound() : Ok(oferta);
    }

    [HttpPost("{id:int}/precio-proveedor")]
    public async Task<IActionResult> TomarPrecio(int id, CancellationToken cancellationToken) =>
        Traducir(await _servicio.TomarPrecioProveedorAsync(id, UsuarioId(), cancellationToken));

    private async Task<IActionResult> Guardar(int? id, InsumoSolicitud solicitud, CancellationToken cancellationToken) =>
        Traducir(await _servicio.GuardarAsync(id, solicitud, UsuarioId(), cancellationToken), id is null);

    private int? UsuarioId() =>
        int.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : null;

    private IActionResult Traducir<T>(ResultadoOperacion<T> resultado, bool creado = false) =>
        ResultadoHttp.Traducir(this, resultado, creado);
}

[ApiController]
[Authorize(Policy = Politicas.SoloAdministrador)]
[Route("api/mano-obra")]
public sealed class ManoObraController : ControllerBase
{
    private readonly IManoObraServicio _servicio;

    public ManoObraController(IManoObraServicio servicio)
    {
        _servicio = servicio;
    }

    [HttpGet("parametros")]
    public async Task<IActionResult> Parametros(CancellationToken cancellationToken) =>
        Ok(await _servicio.ObtenerParametrosAsync(cancellationToken));

    [HttpPut("parametros")]
    public async Task<IActionResult> GuardarParametros(ParametrosManoObraSolicitud solicitud, CancellationToken cancellationToken) =>
        Ok(await _servicio.GuardarParametrosAsync(solicitud, cancellationToken));

    [HttpGet("prestaciones")]
    public async Task<IActionResult> Prestaciones(CancellationToken cancellationToken) =>
        Ok(await _servicio.ListarPrestacionesAsync(cancellationToken));

    [HttpPut("prestaciones")]
    public async Task<IActionResult> GuardarPrestaciones(List<PrestacionSolicitud> prestaciones, CancellationToken cancellationToken) =>
        Ok(await _servicio.GuardarPrestacionesAsync(prestaciones, cancellationToken));
}

[ApiController]
[Authorize(Policy = Politicas.SoloAdministrador)]
[Route("api/cargos")]
public sealed class CargosController : ControllerBase
{
    private readonly IManoObraServicio _servicio;

    public CargosController(IManoObraServicio servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] bool incluirInactivos, CancellationToken cancellationToken) =>
        Ok(await _servicio.ListarCargosAsync(incluirInactivos, cancellationToken));

    [HttpPost]
    public async Task<IActionResult> Crear(CargoSolicitud solicitud, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.GuardarCargoAsync(null, solicitud, cancellationToken), true);

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, CargoSolicitud solicitud, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.GuardarCargoAsync(id, solicitud, cancellationToken));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.EliminarCargoAsync(id, cancellationToken));
}

[ApiController]
[Authorize(Policy = Politicas.SoloAdministrador)]
[Route("api/cuadrillas")]
public sealed class CuadrillasController : ControllerBase
{
    private readonly IManoObraServicio _servicio;

    public CuadrillasController(IManoObraServicio servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] bool incluirInactivos, CancellationToken cancellationToken) =>
        Ok(await _servicio.ListarCuadrillasAsync(incluirInactivos, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken cancellationToken)
    {
        var cuadrilla = await _servicio.ObtenerCuadrillaAsync(id, cancellationToken);
        return cuadrilla is null ? NotFound() : Ok(cuadrilla);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(CuadrillaSolicitud solicitud, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.GuardarCuadrillaAsync(null, solicitud, cancellationToken), true);

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, CuadrillaSolicitud solicitud, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.GuardarCuadrillaAsync(id, solicitud, cancellationToken));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.EliminarCuadrillaAsync(id, cancellationToken));
}

[ApiController]
[Authorize(Policy = Politicas.SoloAdministrador)]
[Route("api/apu")]
public sealed class ApusController : ControllerBase
{
    private readonly IApuServicio _servicio;

    public ApusController(IApuServicio servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? texto,
        [FromQuery] bool incluirInactivos,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = Paginacion.TamanoDefecto,
        [FromQuery] bool? esSubanalisis = null,
        CancellationToken cancellationToken = default) =>
        Ok(await _servicio.ListarAsync(texto, incluirInactivos, cancellationToken, pagina, tamano, esSubanalisis));

    [HttpGet("buscar")]
    public async Task<IActionResult> Buscar([FromQuery] string? texto, [FromQuery] int? unidadId, [FromQuery] int top, CancellationToken cancellationToken) =>
        Ok(await _servicio.BuscarAsync(texto, unidadId, top, cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id, CancellationToken cancellationToken)
    {
        var apu = await _servicio.ObtenerAsync(id, cancellationToken);
        return apu is null ? NotFound() : Ok(apu);
    }

    [HttpPost]
    public async Task<IActionResult> Crear(ApuSolicitud solicitud, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.GuardarAsync(null, solicitud, cancellationToken), true);

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, ApuSolicitud solicitud, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.GuardarAsync(id, solicitud, cancellationToken));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.EliminarAsync(id, cancellationToken));

    [HttpGet("{id:int}/usos")]
    public async Task<IActionResult> Usos(int id, CancellationToken cancellationToken) =>
        Ok(await _servicio.UsosAsync(id, cancellationToken));

    [HttpPost("{id:int}/duplicar")]
    public async Task<IActionResult> Duplicar(int id, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.DuplicarAsync(id, cancellationToken), true);
}

[ApiController]
[Authorize(Policy = Politicas.GestionPresupuesto)]
[Route("api/presupuestos")]
public sealed class PresupuestosController : ControllerBase
{
    private readonly IPresupuestoServicio _servicio;

    public PresupuestosController(IPresupuestoServicio servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PresupuestoResumenDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] int? construccionId, [FromQuery] string? estado, [FromQuery] bool plantillas, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.ListarAsync(construccionId, Usuario(), cancellationToken, estado, plantillas));

    [HttpGet("configuracion")]
    [ProducesResponseType(typeof(OpcionesPresupuesto), StatusCodes.Status200OK)]
    public IActionResult Configuracion([FromServices] IOptions<OpcionesPresupuesto> opciones) =>
        Ok(opciones.Value);

    [HttpGet("plantillas")]
    [ProducesResponseType(typeof(IReadOnlyList<PlantillaResumenDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Plantillas(CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.PlantillasAsync(Usuario(), cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PresupuestoDetalleDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Obtener(int id, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.ObtenerAsync(id, Usuario(), cancellationToken));

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpPost]
    [ProducesResponseType(typeof(PresupuestoCreadoDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Crear(CrearPresupuestoSolicitud solicitud, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.CrearDesdeAsync(solicitud, Usuario(), cancellationToken), true);

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpPost("{id:int}/guardar-como-plantilla")]
    public async Task<IActionResult> GuardarPlantilla(int id, NombrePlantillaSolicitud solicitud, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.GuardarComoPlantillaAsync(id, solicitud.Nombre, Usuario(), cancellationToken), true);

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpPost("{id:int}/capitulos/{capituloId:int}/items/desde-apu")]
    public async Task<IActionResult> DesdeApu(int id, int capituloId, ItemsDesdeApuSolicitud solicitud, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.AgregarDesdeApuAsync(id, capituloId, solicitud, Usuario(), cancellationToken));

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpPost("{id:int}/renumerar")]
    public async Task<IActionResult> Renumerar(int id, VersionSolicitud? solicitud, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.RenumerarAsync(id, solicitud?.RowVersion, Usuario(), cancellationToken));

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpPut("{id:int}/orden")]
    public async Task<IActionResult> Orden(int id, OrdenPresupuestoSolicitud solicitud, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.OrdenarAsync(id, solicitud, Usuario(), cancellationToken));

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpPost("{id:int}/items/{itemId:int}/duplicar")]
    public async Task<IActionResult> DuplicarItem(int id, int itemId, VersionSolicitud? solicitud, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.DuplicarItemAsync(id, itemId, solicitud?.RowVersion, Usuario(), cancellationToken));

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpPost("{id:int}/items/{itemId:int}/mover")]
    public async Task<IActionResult> MoverItem(int id, int itemId, MoverItemSolicitud solicitud, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.MoverItemAsync(id, itemId, solicitud, Usuario(), cancellationToken));

    [HttpGet("{id:int}/validacion")]
    [ProducesResponseType(typeof(IReadOnlyList<ValidacionPresupuestoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Validacion(int id, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.ValidarAsync(id, Usuario(), cancellationToken));

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PresupuestoDetalleDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Actualizar(int id, PresupuestoSolicitud solicitud, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.ActualizarAsync(id, solicitud, Usuario(), cancellationToken));

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, [FromQuery] string? rowVersion, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.EliminarAsync(id, Usuario(), cancellationToken, rowVersion));

    [HttpPost("{id:int}/recalcular")]
    [ProducesResponseType(typeof(RecalculoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Recalcular(int id, [FromQuery] bool aplicar, [FromQuery] string? rowVersion, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.RecalcularAsync(id, aplicar, Usuario(), cancellationToken, rowVersion));

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpPost("{id:int}/aprobar")]
    public async Task<IActionResult> Aprobar(int id, [FromQuery] bool confirmarCantidadesCero, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.AprobarAsync(id, Usuario(), cancellationToken, confirmarCantidadesCero));

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpPost("{id:int}/nueva-version")]
    public async Task<IActionResult> NuevaVersion(int id, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.NuevaVersionAsync(id, Usuario(), cancellationToken));

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpPost("{id:int}/cerrar")]
    public async Task<IActionResult> Cerrar(int id, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.CerrarAsync(id, Usuario(), cancellationToken));

    [HttpGet("{id:int}/seguimiento")]
    [ProducesResponseType(typeof(SeguimientoDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Seguimiento(int id, CancellationToken cancellationToken) =>
        ResultadoHttp.Traducir(this, await _servicio.SeguimientoAsync(id, Usuario(), cancellationToken));

    [HttpGet("{id:int}/excel")]
    public async Task<IActionResult> Excel(int id, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.ExcelAsync(id, Usuario(), cancellationToken);
        return resultado.Estado == EstadoOperacion.Ok
            ? File(resultado.Valor!, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"presupuesto-{id}.xlsx")
            : ResultadoHttp.Traducir(this, resultado);
    }

    [HttpGet("{id:int}/pdf")]
    public async Task<IActionResult> Pdf(int id, CancellationToken cancellationToken)
    {
        var resultado = await _servicio.PdfAsync(id, Usuario(), cancellationToken);
        return resultado.Estado == EstadoOperacion.Ok
            ? File(resultado.Valor!, "application/pdf", $"presupuesto-{id}.pdf")
            : ResultadoHttp.Traducir(this, resultado);
    }

    [Authorize(Policy = Politicas.SoloAdministrador)]
    [HttpPost("importar")]
    [ProducesResponseType(typeof(InformeImportacionDto), StatusCodes.Status201Created)]
    [RequestSizeLimit(10_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 10_000_000)]
    [ProblemaTamanoImportacion]
    public async Task<IActionResult> Importar(
        IFormFile archivo,
        [FromForm] int construccionId,
        [FromServices] IOptions<OpcionesPresupuesto> opciones,
        CancellationToken cancellationToken)
    {
        if (archivo is null || archivo.Length == 0)
        {
            return Problem(title: "Solicitud inválida", detail: "Seleccione el libro de Excel.", statusCode: StatusCodes.Status400BadRequest);
        }

        var maximo = opciones.Value.TamanoMaximoImportacion > 0 ? opciones.Value.TamanoMaximoImportacion : 10_000_000;
        if (archivo.Length > maximo)
        {
            return Problem(title: "Archivo demasiado grande", detail: $"El libro no puede superar {maximo} bytes.", statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        await using var contenido = archivo.OpenReadStream();
        Stream lectura = contenido;
        MemoryStream? copia = null;
        if (!contenido.CanSeek)
        {
            copia = new MemoryStream();
            await contenido.CopyToAsync(copia, cancellationToken);
            copia.Position = 0;
            lectura = copia;
        }

        try
        {
            var error = ValidadorLibroExcel.Revisar(archivo.FileName, lectura);
            if (error is not null)
            {
                return Problem(title: "Solicitud inválida", detail: error, statusCode: StatusCodes.Status400BadRequest);
            }

            return ResultadoHttp.Traducir(this, await _servicio.ImportarAsync(lectura, construccionId, Usuario(), cancellationToken), true);
        }
        finally
        {
            if (copia is not null)
            {
                await copia.DisposeAsync();
            }
        }
    }

    private int Usuario() =>
        int.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id) ? id : 0;
}

[ApiController]
[Authorize]
[Route("api/presupuestos/items-acta")]
public sealed class ItemsActaController : ControllerBase
{
    private readonly IPresupuestoServicio _servicio;

    public ItemsActaController(IPresupuestoServicio servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ItemActaDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Listar([FromQuery] int construccionId, CancellationToken cancellationToken)
    {
        var administrador = User.IsInRole(Perfiles.Administrador);
        return Ok(await _servicio.ItemsParaActaAsync(construccionId, administrador, cancellationToken));
    }
}

public static class ResultadoHttp
{
    public static IActionResult Traducir<T>(ControllerBase controlador, ResultadoOperacion<T> resultado, bool creado = false) =>
        resultado.Estado switch
        {
            EstadoOperacion.Ok when creado => controlador.Created(string.Empty, resultado.Valor),
            EstadoOperacion.Ok => controlador.Ok(resultado.Valor),
            EstadoOperacion.NoEncontrado => controlador.Problem(title: "No encontrado", detail: resultado.Mensaje, statusCode: StatusCodes.Status404NotFound),
            EstadoOperacion.Prohibido => controlador.Problem(title: "No autorizado", detail: resultado.Mensaje, statusCode: StatusCodes.Status403Forbidden),
            EstadoOperacion.Conflicto => controlador.Problem(title: "Conflicto", detail: resultado.Mensaje, statusCode: StatusCodes.Status409Conflict),
            _ => controlador.Problem(title: "Solicitud inválida", detail: resultado.Mensaje, statusCode: StatusCodes.Status400BadRequest)
        };
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class ProblemaTamanoImportacionAttribute : Attribute, IAlwaysRunResultFilter
{
    public void OnResultExecuting(ResultExecutingContext contexto)
    {
        if (contexto.Controller is not ControllerBase controlador)
        {
            return;
        }

        var porAtributo = contexto.Result is StatusCodeResult { StatusCode: StatusCodes.Status413PayloadTooLarge };
        var porFormulario = contexto.Result is ObjectResult { Value: ValidationProblemDetails detalles } &&
            detalles.Errors.Values
                .SelectMany(mensajes => mensajes)
                .Any(mensaje => mensaje.Contains("Multipart body length limit", StringComparison.OrdinalIgnoreCase));
        if (!porAtributo && !porFormulario)
        {
            return;
        }

        var maximo = contexto.HttpContext.RequestServices.GetRequiredService<IOptions<OpcionesPresupuesto>>().Value.TamanoMaximoImportacion;
        if (maximo <= 0)
        {
            maximo = 10_000_000;
        }

        contexto.Result = controlador.Problem(
            title: "Archivo demasiado grande",
            detail: $"El libro no puede superar {maximo} bytes.",
            statusCode: StatusCodes.Status413PayloadTooLarge);
    }

    public void OnResultExecuted(ResultExecutedContext contexto)
    {
    }
}
