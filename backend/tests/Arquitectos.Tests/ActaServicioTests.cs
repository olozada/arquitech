using Arquitectos.Application.Actas;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Actas;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Tests;

public sealed class ActaServicioTests : IDisposable
{
    private readonly SqliteConnection _conexion;
    private readonly ArquitectosDbContext _context;
    private readonly ActaServicio _servicio;

    public ActaServicioTests()
    {
        _conexion = new SqliteConnection("Data Source=:memory:");
        _conexion.Open();
        var opciones = new DbContextOptionsBuilder<ArquitectosDbContext>()
            .UseSqlite(_conexion)
            .Options;
        _context = new ArquitectosDbContext(opciones);
        _context.Database.EnsureCreated();
        _servicio = new ActaServicio(_context);
    }

    [Fact]
    public async Task El_total_es_la_suma_de_cantidad_por_precio()
    {
        var catalogos = await SembrarCatalogosAsync();
        var solicitud = Solicitud(catalogos);
        solicitud.Detalles.Add(new LineaActaSolicitud
        {
            ConstruccionId = catalogos.ConstruccionId,
            ConceptoId = catalogos.ConceptoId,
            UnidadMedidaId = catalogos.UnidadMedidaId,
            Cantidad = 1,
            Precio = 50,
            Descripcion = "Repello"
        });

        var creada = await _servicio.CrearAsync(solicitud, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var consulta = await _servicio.ConsultarAsync(
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 30),
            catalogos.ArquitectoId,
            CancellationToken.None);
        var acta = await _servicio.ObtenerAsync(creada.Valor!.NumeroActa, CancellationToken.None);

        Assert.Equal(EstadoActa.Ok, creada.Estado);
        Assert.Equal(250m, consulta.Valor!.TotalGeneral);
        Assert.Equal(250m, consulta.Valor.Actas.Single().Total);
        Assert.Equal(250m, acta.Valor!.Total);
        Assert.Equal(200m, acta.Valor.Detalles[0].Subtotal);
        Assert.Equal(50m, acta.Valor.Detalles[1].Subtotal);
        Assert.Equal(SemillaInicial.UsuarioAdminId, acta.Valor.UsuarioRegistroId);
    }

    [Fact]
    public async Task Rechaza_fechas_invalidas_sin_guardar_el_acta()
    {
        var catalogos = await SembrarCatalogosAsync();
        var solicitud = Solicitud(catalogos);
        solicitud.FechaFinal = solicitud.FechaInicial.AddDays(-1);

        var creada = await _servicio.CrearAsync(solicitud, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var consulta = await _servicio.ConsultarAsync(
            solicitud.FechaInicial,
            solicitud.FechaFinal,
            catalogos.ArquitectoId,
            CancellationToken.None);

        Assert.Equal(EstadoActa.Invalido, creada.Estado);
        Assert.Equal(CrearActaValidador.MensajeFechas, creada.Mensaje);
        Assert.Equal(EstadoActa.Invalido, consulta.Estado);
        Assert.Equal(CrearActaValidador.MensajeFechas, consulta.Mensaje);
        Assert.Equal(0, await _context.Actas.CountAsync());
    }

    [Fact]
    public async Task Rechaza_el_detalle_vacio_sin_guardar_el_acta()
    {
        var catalogos = await SembrarCatalogosAsync();
        var solicitud = Solicitud(catalogos);
        solicitud.Detalles = [];

        var creada = await _servicio.CrearAsync(solicitud, SemillaInicial.UsuarioAdminId, CancellationToken.None);

        Assert.Equal(EstadoActa.Invalido, creada.Estado);
        Assert.Equal(CrearActaValidador.MensajeDetalle, creada.Mensaje);
        Assert.Equal(0, await _context.Actas.CountAsync());
    }

    public void Dispose()
    {
        _context.Dispose();
        _conexion.Dispose();
    }

    private async Task<CatalogosActa> SembrarCatalogosAsync()
    {
        var arquitecto = new Arquitecto { Nombre = "Arquitecto prueba" };
        var revisor = new Revisor { Nombre = "Revisor prueba" };
        var contratista = new Contratista { Nombre = "Contratista prueba" };
        var construccion = new Construccion { Descripcion = "Obra prueba", Propietario = "Dueño" };
        var concepto = new Concepto { Descripcion = "Concepto prueba", Unidad = "m2" };
        var unidad = new UnidadMedida { Nombre = "m2", Descripcion = "Metro cuadrado" };

        _context.AddRange(arquitecto, revisor, contratista, construccion, concepto, unidad);
        await _context.SaveChangesAsync();

        return new CatalogosActa(
            arquitecto.Id,
            revisor.Id,
            contratista.Id,
            construccion.Id,
            concepto.Id,
            unidad.Id);
    }

    private static CrearActaSolicitud Solicitud(CatalogosActa catalogos) => new()
    {
        FechaInicial = new DateOnly(2026, 11, 1),
        FechaFinal = new DateOnly(2026, 11, 9),
        ArquitectoId = catalogos.ArquitectoId,
        RevisorId = catalogos.RevisorId,
        ContratistaId = catalogos.ContratistaId,
        Observacion = "Acta de prueba",
        Detalles =
        [
            new LineaActaSolicitud
            {
                ConstruccionId = catalogos.ConstruccionId,
                ConceptoId = catalogos.ConceptoId,
                UnidadMedidaId = catalogos.UnidadMedidaId,
                Cantidad = 2,
                Precio = 100,
                Descripcion = "Muro"
            }
        ]
    };

    private sealed record CatalogosActa(
        int ArquitectoId,
        int RevisorId,
        int ContratistaId,
        int ConstruccionId,
        int ConceptoId,
        int UnidadMedidaId);
}
