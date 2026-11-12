using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Arquitectos.Tests;

public sealed class CabecerasSeguridadTests : IDisposable
{
    private const string Politica =
        "default-src 'self'; img-src 'self' data: blob:; style-src 'self' 'unsafe-inline'; script-src 'self'; frame-ancestors 'none'";

    private readonly WebApplicationFactory<Program> _desarrollo;
    private readonly WebApplicationFactory<Program> _staging;
    private readonly HttpClient _cliente;
    private readonly HttpClient _clienteHttps;

    public CabecerasSeguridadTests()
    {
        Environment.SetEnvironmentVariable("Jwt__Key", new string('k', 40));
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Arquitectos",
            "Server=127.0.0.1;Database=Arquitectos;User ID=ci;Password=ci;TrustServerCertificate=True");
        _desarrollo = Fabrica(null);
        _staging = Fabrica("Staging");
        _cliente = _desarrollo.CreateClient();
        _clienteHttps = _staging.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://arquitectos.test")
        });
    }

    [Fact]
    public async Task Health_incluye_las_cabeceras_de_seguridad()
    {
        var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        peticion.Headers.TryAddWithoutValidation("Origin", "http://localhost:5173");
        var respuesta = await _cliente.SendAsync(peticion);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("nosniff", Cabecera(respuesta, "X-Content-Type-Options"));
        Assert.Equal("DENY", Cabecera(respuesta, "X-Frame-Options"));
        Assert.Equal("strict-origin-when-cross-origin", Cabecera(respuesta, "Referrer-Policy"));
        Assert.Equal("camera=(), microphone=(), geolocation=()", Cabecera(respuesta, "Permissions-Policy"));
        Assert.Equal(Politica, Cabecera(respuesta, "Content-Security-Policy"));
        Assert.Contains("blob:", Cabecera(respuesta, "Content-Security-Policy"), StringComparison.Ordinal);
        Assert.DoesNotContain("Strict-Transport-Security", respuesta.Headers.Select(cabecera => cabecera.Key));
        Assert.Equal("http://localhost:5173", Cabecera(respuesta, "Access-Control-Allow-Origin"));
        Assert.False(string.IsNullOrWhiteSpace(Cabecera(respuesta, "X-Correlation-Id")));
    }

    [Fact]
    public async Task Devuelve_el_id_de_correlacion_recibido()
    {
        var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        peticion.Headers.TryAddWithoutValidation("X-Correlation-Id", "corr-prueba");
        var respuesta = await _cliente.SendAsync(peticion);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("corr-prueba", Cabecera(respuesta, "X-Correlation-Id"));
    }

    [Fact]
    public async Task Salud_de_la_base_no_revela_detalles()
    {
        var respuesta = await _cliente.GetAsync("/api/health/ready");
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.True(
            respuesta.StatusCode is HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable);
        Assert.Contains("estado", cuerpo, StringComparison.Ordinal);
        Assert.DoesNotContain("password", cuerpo, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exception", cuerpo, StringComparison.Ordinal);
        Assert.DoesNotContain("Server=", cuerpo, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Fuera_de_desarrollo_envia_HSTS_y_no_abre_CORS()
    {
        var peticion = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        peticion.Headers.TryAddWithoutValidation("Origin", "http://localhost:5173");
        var respuesta = await _clienteHttps.SendAsync(peticion);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("nosniff", Cabecera(respuesta, "X-Content-Type-Options"));
        Assert.Contains("max-age=", Cabecera(respuesta, "Strict-Transport-Security"), StringComparison.Ordinal);
        Assert.False(respuesta.Headers.Contains("Access-Control-Allow-Origin"));
    }

    public void Dispose()
    {
        _cliente.Dispose();
        _clienteHttps.Dispose();
        _desarrollo.Dispose();
        _staging.Dispose();
    }

    private static WebApplicationFactory<Program> Fabrica(string? entorno)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            if (entorno is not null)
            {
                builder.UseSetting(WebHostDefaults.EnvironmentKey, entorno);
            }

            builder.ConfigureAppConfiguration((_, configuracion) =>
            {
                configuracion.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = new string('k', 40),
                    ["Jwt:Issuer"] = "Arquitectos",
                    ["Jwt:Audience"] = "Arquitectos",
                    ["ConnectionStrings:Arquitectos"] = "Server=127.0.0.1;Database=Arquitectos;Trusted_Connection=True;TrustServerCertificate=True"
                });
            });
        });
    }

    private static string Cabecera(HttpResponseMessage respuesta, string nombre) =>
        respuesta.Headers.GetValues(nombre).Single();
}
