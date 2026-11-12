using System.Text.Json;
using Arquitectos.Api.Configuracion;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arquitectos.Tests;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task El_problema_incluye_el_correlation_id_y_oculta_el_error()
    {
        var servicios = new ServiceCollection();
        servicios.AddProblemDetails();
        servicios.AddLogging();
        var contexto = new DefaultHttpContext
        {
            RequestServices = servicios.BuildServiceProvider()
        };
        contexto.Request.Headers[Correlacion.Encabezado] = "corr-prueba";
        contexto.Response.Body = new MemoryStream();
        var handler = new GlobalExceptionHandler(NullLogger<GlobalExceptionHandler>.Instance);

        var manejado = await handler.TryHandleAsync(
            contexto,
            new InvalidOperationException("contraseña supersecreta"),
            CancellationToken.None);

        contexto.Response.Body.Position = 0;
        var json = await new StreamReader(contexto.Response.Body).ReadToEndAsync();
        using var documento = JsonDocument.Parse(json);

        Assert.True(manejado);
        Assert.Equal("corr-prueba", documento.RootElement.GetProperty("correlationId").GetString());
        Assert.Equal("Ocurrió un error inesperado.", documento.RootElement.GetProperty("detail").GetString());
        Assert.DoesNotContain("supersecreta", json, StringComparison.Ordinal);
    }
}
