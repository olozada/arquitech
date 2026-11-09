using Microsoft.AspNetCore.Diagnostics;

namespace Arquitectos.Api.Configuracion;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            exception,
            "Error no controlado en {Metodo} {Ruta}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        await Results.Problem(
            title: "Error interno",
            detail: "Ocurrió un error inesperado.",
            statusCode: StatusCodes.Status500InternalServerError,
            instance: httpContext.Request.Path
        ).ExecuteAsync(httpContext);

        return true;
    }
}
