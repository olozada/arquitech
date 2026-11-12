using System.Net;
using System.Net.Http.Json;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Arquitectos.Tests;

public sealed class LoginLimiteTests : IAsyncLifetime
{
    private readonly SqliteConnection _conexion = new("Data Source=login-limite;Mode=Memory;Cache=Shared");
    private WebApplicationFactory<Program> _fabrica = null!;

    public async Task InitializeAsync()
    {
        await _conexion.OpenAsync();
        Environment.SetEnvironmentVariable("Jwt__Key", new string('k', 40));
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Arquitectos",
            "Server=127.0.0.1;Database=Arquitectos;User ID=ci;Password=ci;TrustServerCertificate=True");
        _fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
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
            builder.ConfigureServices(servicios =>
            {
                servicios.RemoveAll(typeof(DbContextOptions<ArquitectosDbContext>));
                servicios.RemoveAll(typeof(DbContextOptions));
                servicios.RemoveAll(typeof(ArquitectosDbContext));
                servicios.RemoveAll(typeof(IDbContextOptionsConfiguration<ArquitectosDbContext>));
                servicios.AddDbContext<ArquitectosDbContext>(opciones => opciones.UseSqlite(_conexion));
            });
        });

        using var alcance = _fabrica.Services.CreateScope();
        var contexto = alcance.ServiceProvider.GetRequiredService<ArquitectosDbContext>();
        await contexto.Database.EnsureCreatedAsync();
    }

    [Fact]
    public async Task La_sexta_peticion_de_login_en_un_minuto_responde_429()
    {
        using var cliente = _fabrica.CreateClient();
        HttpResponseMessage? ultima = null;
        for (var intento = 0; intento < 6; intento++)
        {
            ultima = await cliente.PostAsJsonAsync(
                "/api/auth/login",
                new { usuario = "nadie", password = "Clave12345" });
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, ultima!.StatusCode);
    }

    public async Task DisposeAsync()
    {
        await _fabrica.DisposeAsync();
        await _conexion.DisposeAsync();
    }
}
