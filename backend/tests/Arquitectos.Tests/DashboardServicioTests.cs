using Arquitectos.Application.Dashboard;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Dashboard;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Arquitectos.Tests;

public sealed class DashboardServicioTests : IDisposable
{
    private readonly SqliteConnection _conexion;
    private readonly ArquitectosDbContext _context;
    private readonly DashboardServicio _servicio;

    public DashboardServicioTests()
    {
        _conexion = new SqliteConnection("Data Source=:memory:");
        _conexion.Open();
        var opciones = new DbContextOptionsBuilder<ArquitectosDbContext>()
            .UseSqlite(_conexion)
            .Options;
        _context = new ArquitectosDbContext(opciones);
        _context.Database.EnsureCreated();
        _servicio = new DashboardServicio(
            _context,
            Options.Create(new OpcionesDashboard()),
            new MemoryCache(new MemoryCacheOptions()));
    }

    [Fact]
    public async Task El_periodo_anterior_tiene_la_misma_duracion_y_la_variacion_es_nula_si_no_hay_base()
    {
        var catalogos = await SembrarAsync();
        await AgregarActaAsync(catalogos, new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 20), 200m, 2);

        var filtro = new FiltroDashboard
        {
            Desde = new DateOnly(2026, 3, 1),
            Hasta = new DateOnly(2026, 3, 31)
        };
        var tablero = await _servicio.ObtenerAsync(filtro, CancellationToken.None);
        var valor = tablero.Kpis.Single(kpi => kpi.Codigo == "K1");

        Assert.Equal(new DateOnly(2026, 1, 29), tablero.DesdeAnterior);
        Assert.Equal(new DateOnly(2026, 2, 28), tablero.HastaAnterior);
        Assert.Equal(400m, valor.Valor);
        Assert.Equal(0m, valor.ValorAnterior);
        Assert.Null(valor.VariacionPct);
    }

    [Fact]
    public async Task Los_filtros_recorren_el_tablero_y_el_top_no_supera_el_valor_ejecutado()
    {
        var catalogos = await SembrarAsync();
        await AgregarActaAsync(catalogos, new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 10), 100m, 1);
        await AgregarActaAsync(
            catalogos,
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 12),
            400m,
            1,
            catalogos.OtraConstruccionId,
            catalogos.OtroContratistaId,
            catalogos.OtroArquitectoId);

        var filtro = new FiltroDashboard
        {
            Desde = new DateOnly(2026, 5, 1),
            Hasta = new DateOnly(2026, 5, 31),
            ConstruccionId = catalogos.ConstruccionId,
            ContratistaId = catalogos.ContratistaId,
            ArquitectoId = catalogos.ArquitectoId
        };
        var tablero = await _servicio.ObtenerAsync(filtro, CancellationToken.None);
        var valor = tablero.Kpis.Single(kpi => kpi.Codigo == "K1");

        Assert.Equal(100m, valor.Valor);
        Assert.Equal(1, tablero.Kpis.Single(kpi => kpi.Codigo == "K2").Valor);
        var top = Assert.Single(tablero.TopConstrucciones);
        Assert.Equal(catalogos.ConstruccionId, top.Id);
        Assert.Equal(catalogos.ContratistaId, Assert.Single(tablero.Contratistas).Id);
        Assert.Equal(catalogos.ArquitectoId, Assert.Single(tablero.Arquitectos).Id);
        Assert.True(tablero.TopConstrucciones.Sum(fila => fila.Valor) <= valor.Valor);
    }

    [Fact]
    public void Rechaza_un_rango_invertido()
    {
        var validador = new FiltroDashboardValidador();
        var resultado = validador.Validate(new FiltroDashboard
        {
            Desde = new DateOnly(2026, 6, 10),
            Hasta = new DateOnly(2026, 6, 1)
        });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, error => error.ErrorMessage == FiltroDashboardValidador.MensajeFechas);
    }

    public void Dispose()
    {
        _context.Dispose();
        _conexion.Dispose();
    }

    private async Task<CatalogosDashboard> SembrarAsync()
    {
        var arquitecto = new Arquitecto { Nombre = "Arq uno" };
        var otroArquitecto = new Arquitecto { Nombre = "Arq dos" };
        var revisor = new Revisor { Nombre = "Revisor uno" };
        var contratista = new Contratista { Nombre = "Contra uno" };
        var otroContratista = new Contratista { Nombre = "Contra dos" };
        var obra = new Construccion { Descripcion = "Obra uno", Propietario = "Dueño" };
        var otraObra = new Construccion { Descripcion = "Obra dos", Propietario = "Dueño" };
        var concepto = new Concepto { Descripcion = "Concepto uno", Unidad = "m2" };
        var unidad = new UnidadMedida { Nombre = "m2", Descripcion = "Metro" };
        _context.AddRange(arquitecto, otroArquitecto, revisor, contratista, otroContratista, obra, otraObra, concepto, unidad);
        await _context.SaveChangesAsync();
        return new CatalogosDashboard(
            arquitecto.Id,
            otroArquitecto.Id,
            revisor.Id,
            contratista.Id,
            otroContratista.Id,
            obra.Id,
            otraObra.Id,
            concepto.Id,
            unidad.Id);
    }

    private async Task AgregarActaAsync(
        CatalogosDashboard catalogos,
        DateOnly inicio,
        DateOnly fin,
        decimal precio,
        decimal cantidad,
        int? construccionId = null,
        int? contratistaId = null,
        int? arquitectoId = null)
    {
        _context.Actas.Add(new Acta
        {
            FechaInicial = inicio,
            FechaFinal = fin,
            ArquitectoId = arquitectoId ?? catalogos.ArquitectoId,
            RevisorId = catalogos.RevisorId,
            ContratistaId = contratistaId ?? catalogos.ContratistaId,
            Observacion = "Prueba",
            FechaRegistro = DateTime.SpecifyKind(fin.ToDateTime(new TimeOnly(15, 0)), DateTimeKind.Utc),
            UsuarioRegistroId = SemillaInicial.UsuarioAdminId,
            Detalles =
            [
                new ActaDetalle
                {
                    ConstruccionId = construccionId ?? catalogos.ConstruccionId,
                    ConceptoId = catalogos.ConceptoId,
                    UnidadMedidaId = catalogos.UnidadId,
                    Cantidad = cantidad,
                    Precio = precio,
                    Descripcion = "Línea"
                }
            ]
        });
        await _context.SaveChangesAsync();
    }

    private sealed record CatalogosDashboard(
        int ArquitectoId,
        int OtroArquitectoId,
        int RevisorId,
        int ContratistaId,
        int OtroContratistaId,
        int ConstruccionId,
        int OtraConstruccionId,
        int ConceptoId,
        int UnidadId);
}
