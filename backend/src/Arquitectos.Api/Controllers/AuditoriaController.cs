using Arquitectos.Application.Auditoria;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arquitectos.Api.Controllers;

[ApiController]
[Authorize(Roles = "Administrador")]
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
        CancellationToken cancellationToken = default) =>
        await _consulta.ConsultarAsync(entidad, entidadId, desde, hasta, usuarioId, pagina, cancellationToken);
}
