using Arquitectos.Application;
using Arquitectos.Application.Auditoria;
using Arquitectos.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arquitectos.Api.Controllers;

[ApiController]
[Authorize(Policy = Politicas.SoloAdministrador)]
[Route("api/auditoria")]
public sealed class AuditoriaController : ControllerBase
{
    private readonly IConsultaAuditoria _consulta;

    public AuditoriaController(IConsultaAuditoria consulta)
    {
        _consulta = consulta;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginaAuditoriaDto), StatusCodes.Status200OK)]
    public async Task<PaginaAuditoriaDto> Consultar(
        [FromQuery] string? entidad,
        [FromQuery] string? entidadId,
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] int? usuarioId,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = Paginacion.TamanoDefecto,
        CancellationToken cancellationToken = default) =>
        await _consulta.ConsultarAsync(entidad, entidadId, desde, hasta, usuarioId, pagina, tamano, cancellationToken);
}
