using Arquitectos.Application.Movil;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Arquitectos.Api.Controllers.Movil;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ExigirVersionAppAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext contexto, ActionExecutionDelegate siguiente)
    {
        var opciones = contexto.HttpContext.RequestServices.GetRequiredService<IOptions<OpcionesMovil>>().Value;
        var anunciada = contexto.HttpContext.Request.Headers["X-Version-App"].ToString();
        if (VersionApp.EsMenor(anunciada, opciones.VersionMinimaApp))
        {
            contexto.Result = new ObjectResult(new
            {
                title = "Actualice la aplicación.",
                detail = "Actualice la aplicación."
            })
            {
                StatusCode = StatusCodes.Status426UpgradeRequired
            };
            return;
        }

        await siguiente();
    }
}
