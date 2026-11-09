using System.Security.Claims;
using Arquitectos.Application.Auth;
using Arquitectos.Application.Menu;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Arquitectos.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/menu")]
public sealed class MenuController : ControllerBase
{
    private readonly IMenuServicio _menu;

    public MenuController(IMenuServicio menu)
    {
        _menu = menu;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OpcionMenuDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var valor = User.FindFirstValue(ClaimsArquitectos.TipoUsuarioId);
        if (!int.TryParse(valor, out var tipoUsuarioId))
        {
            return Problem(
                title: "Sesión inválida.",
                detail: "El token no incluye el tipo de usuario.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var arbol = await _menu.ObtenerArbol(tipoUsuarioId, cancellationToken);
        return Ok(arbol);
    }
}
