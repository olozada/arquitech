using Arquitectos.Application.Dashboard;
using Arquitectos.Infrastructure.Dashboard;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Arquitectos.Tests;

public sealed class SemillaDemoTests : IDisposable
{
    private readonly SqliteConnection _conexion = new("Data Source=:memory:");

    public SemillaDemoTests() => _conexion.Open();

    public void Dispose() => _conexion.Dispose();

    [Fact]
    public void El_azar_de_python_con_semilla_uno_empieza_en_el_valor_conocido()
    {
        var azar = new AleatorioPython(1);
        Assert.Equal(0.13436424411240122, azar.Siguiente(), 12);
    }

    [Fact]
    public async Task La_demostracion_al_9_de_noviembre_de_2026_cumple_el_tablero_de_referencia()
    {
        var opciones = new DbContextOptionsBuilder<ArquitectosDbContext>().UseSqlite(_conexion).Options;
        await using var context = new ArquitectosDbContext(opciones);
        context.Database.EnsureCreated();
        var hoy = new DateOnly(2026, 11, 9);
        await SemillaDemo.AplicarAsync(context, hoy: hoy);

        Assert.Equal(152, await context.Actas.CountAsync());
        Assert.Equal(0, await context.Actas.CountAsync(acta => acta.Detalles.Any(detalle => detalle.Construccion.Descripcion == "Clínica veterinaria Calarcá")));

        var servicio = new DashboardServicio(
            context,
            // El tablero publica como máximo 100 alertas. Aquí se sube el tope para contar todas las que disparan las reglas.
            Options.Create(new OpcionesDashboard { MaxAlertas = 1000 }),
            new MemoryCache(new MemoryCacheOptions()))
        {
            InstanteUtc = new DateTime(2026, 11, 9, 15, 0, 0, DateTimeKind.Utc),
        };
        var filtro = new FiltroDashboard { Desde = new DateOnly(2025, 11, 1), Hasta = hoy };
        var tablero = await servicio.ObtenerAsync(filtro, CancellationToken.None);

        var valor = tablero.Kpis.Single(kpi => kpi.Codigo == "K1");
        var actas = tablero.Kpis.Single(kpi => kpi.Codigo == "K2");
        Assert.InRange(valor.Valor, 4_010_000_000m * 0.98m, 4_010_000_000m * 1.02m);
        Assert.InRange(valor.VariacionPct ?? 0, 16m, 20m);
        Assert.InRange(actas.Valor, 94m, 98m);
        Assert.InRange(actas.ValorAnterior ?? 0, 54m, 58m);

        var a1 = await servicio.AlertasAsync(filtro, "A1", null, 1, CancellationToken.None);
        var a2 = await servicio.AlertasAsync(filtro, "A2", null, 1, CancellationToken.None);
        var a3 = await servicio.AlertasAsync(filtro, "A3", null, 1, CancellationToken.None);
        var a4 = await servicio.AlertasAsync(filtro, "A4", null, 1, CancellationToken.None);
        var a5 = await servicio.AlertasAsync(filtro, "A5", null, 1, CancellationToken.None);
        Assert.Equal(13, a1.Total);
        var alertasA1 = await TodasAsync(servicio, filtro, "A1");
        var actasRoble = await context.Actas
            .Where(acta => acta.Contratista.Nombre == "Obras y Acabados El Roble")
            .Select(acta => acta.NumeroActa)
            .ToListAsync();
        Assert.Equal(10, alertasA1.Count(alerta => alerta.NumeroActa is int numero && actasRoble.Contains(numero)));
        Assert.InRange(alertasA1.Sum(alerta => alerta.Impacto ?? 0), 30_000_000m * 0.98m, 30_000_000m * 1.02m);
        Assert.InRange(a2.Total, 76, 80);
        var ahorro = tablero.Kpis.Single(kpi => kpi.Codigo == "K6");
        Assert.InRange(ahorro.Valor, 27_000_000m * 0.98m, 27_000_000m * 1.02m);
        Assert.Equal(2, a3.Total);
        Assert.All(a3.Alertas, alerta => Assert.Contains("Redes y Montajes Calarcá", alerta.Detalle));
        Assert.Equal(2, a4.Total);
        Assert.Contains(a4.Alertas, alerta => alerta.Titulo.Contains("Hotel boutique Salento", StringComparison.Ordinal));
        Assert.Contains(a4.Alertas, alerta => alerta.Titulo.Contains("Clínica veterinaria Calarcá", StringComparison.Ordinal));
        Assert.Contains(a4.Alertas, alerta => alerta.Detalle.Contains("2026-06-15", StringComparison.Ordinal));
        Assert.InRange(a5.Total, 8, 10);

        var quindio = tablero.Contratistas.Single(fila => fila.Nombre == "Construcciones Quindío S.A.S.");
        Assert.InRange(quindio.Participacion, 45m, 49m);
        Assert.Equal(3, tablero.Calidad.MaterialesUnProveedor);
        Assert.True(tablero.Calidad.LineasSinMaterial > 0);
        Assert.Equal(0, await context.ProveedorMateriales.CountAsync(precio => precio.Insumo.Descripcion == "Impermeabilizante"));
    }

    private static async Task<List<AlertaDto>> TodasAsync(DashboardServicio servicio, FiltroDashboard filtro, string tipo)
    {
        var alertas = new List<AlertaDto>();
        var pagina = 1;
        while (true)
        {
            var lote = await servicio.AlertasAsync(filtro, tipo, null, pagina, CancellationToken.None);
            alertas.AddRange(lote.Alertas);
            if (alertas.Count >= lote.Total || lote.Alertas.Count == 0)
            {
                return alertas;
            }

            pagina++;
        }
    }
}
