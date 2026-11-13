using Arquitectos.Application.Dashboard;
using Arquitectos.Infrastructure.Dashboard;
using Arquitectos.Infrastructure.Persistence;
using Arquitectos.Infrastructure.Seguridad;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Arquitectos.Tests;

public sealed class CacheLimiteTests : IDisposable
{
    private readonly SqliteConnection _conexion = new("Data Source=:memory:");
    private readonly ArquitectosDbContext _context;
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 200 });

    public CacheLimiteTests()
    {
        _conexion.Open();
        _context = new ArquitectosDbContext(
            new DbContextOptionsBuilder<ArquitectosDbContext>().UseSqlite(_conexion).Options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task El_servicio_guarda_en_la_cache_con_limite_y_expulsa_al_superarlo()
    {
        var tablero = new DashboardServicio(_context, Options.Create(new OpcionesDashboard()), _cache);
        var filtro = new FiltroDashboard
        {
            Desde = new DateOnly(2026, 3, 1),
            Hasta = new DateOnly(2026, 3, 31)
        };

        await tablero.ObtenerAsync(filtro, CancellationToken.None);
        await tablero.PrecioUnitarioAsync(filtro.Desde, filtro.Hasta, null, null, null, CancellationToken.None);

        var validador = new ValidadorSesion(_context, _cache);
        const string primera = "usuario-vigente:1:1:0";
        await validador.VigenteAsync(1, 1, CancellationToken.None);
        Assert.True(_cache.TryGetValue(primera, out bool _));

        for (var sesion = 2; sesion <= 220; sesion++)
        {
            await validador.VigenteAsync(1, sesion, CancellationToken.None);
        }

        var cupo = _cache.Count <= 200;
        for (var intento = 0; intento < 50 && !cupo; intento++)
        {
            await Task.Delay(20);
            cupo = _cache.Count <= 200;
        }

        Assert.True(cupo);
        Assert.False(_cache.TryGetValue(primera, out bool _));
        Assert.True(_cache.TryGetValue("usuario-vigente:1:220:0", out bool _));
    }

    public void Dispose()
    {
        _cache.Dispose();
        _context.Dispose();
        _conexion.Dispose();
    }
}
