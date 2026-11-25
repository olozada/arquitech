using Arquitectos.Application.Facturas;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Facturas;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

namespace Arquitectos.Tests;

public sealed class FacturaServicioTests : IDisposable
{
    private readonly SqliteConnection _conexion;
    private readonly ArquitectosDbContext _context;
    private readonly FacturaServicio _servicio;

    public FacturaServicioTests()
    {
        _conexion = new SqliteConnection("Data Source=:memory:");
        _conexion.Open();
        var opciones = new DbContextOptionsBuilder<ArquitectosDbContext>()
            .UseSqlite(_conexion)
            .Options;
        _context = new ArquitectosDbContext(opciones);
        _context.Database.EnsureCreated();
        _servicio = new FacturaServicio(_context);
    }

    [Fact]
    public async Task El_total_es_la_suma_de_cantidad_por_precio()
    {
        var catalogos = await SembrarAsync();
        var creada = await _servicio.CrearAsync(Solicitud(catalogos), SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var factura = await _servicio.ObtenerAsync(creada.Valor!.Id, CancellationToken.None);

        Assert.Equal(EstadoFactura.Ok, creada.Estado);
        Assert.Equal(250m, factura.Valor!.Total);
        Assert.Equal(catalogos.UnidadId, factura.Valor.Detalles[0].UnidadMedidaId);
        Assert.Equal("UND", factura.Valor.Detalles[0].UnidadMedida);
        Assert.Equal(200m, factura.Valor.Detalles[0].Subtotal);
        Assert.Equal(50m, factura.Valor.Detalles[1].Subtotal);
    }

    [Fact]
    public async Task Rechaza_el_mismo_numero_del_mismo_proveedor()
    {
        var catalogos = await SembrarAsync();
        await _servicio.CrearAsync(Solicitud(catalogos), SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var repetida = await _servicio.CrearAsync(Solicitud(catalogos), SemillaInicial.UsuarioAdminId, CancellationToken.None);

        Assert.Equal(EstadoFactura.Invalido, repetida.Estado);
        Assert.Equal(CrearFacturaValidador.MensajeDuplicada, repetida.Mensaje);
        Assert.Equal(1, await _context.Facturas.CountAsync());
    }

    [Fact]
    public async Task Permite_el_mismo_numero_en_otro_proveedor()
    {
        var catalogos = await SembrarAsync();
        await _servicio.CrearAsync(Solicitud(catalogos), SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var otra = Solicitud(catalogos);
        otra.ProveedorId = catalogos.OtroProveedorId;
        var creada = await _servicio.CrearAsync(otra, SemillaInicial.UsuarioAdminId, CancellationToken.None);

        Assert.Equal(EstadoFactura.Ok, creada.Estado);
        Assert.Equal(2, await _context.Facturas.CountAsync());
    }

    [Fact]
    public async Task Eliminar_borra_la_factura_y_sus_lineas()
    {
        var catalogos = await SembrarAsync();
        var creada = await _servicio.CrearAsync(Solicitud(catalogos), SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var borrada = await _servicio.EliminarAsync(creada.Valor!.Id, CancellationToken.None);

        Assert.Equal(EstadoFactura.Ok, borrada.Estado);
        Assert.Equal(0, await _context.Facturas.CountAsync());
        Assert.Equal(0, await _context.FacturaDetalles.CountAsync());
    }

    [Fact]
    public async Task El_acumulado_suma_el_mismo_insumo_de_varias_facturas()
    {
        var catalogos = await SembrarAsync();
        await _servicio.CrearAsync(Solicitud(catalogos), SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var segunda = Solicitud(catalogos);
        segunda.Numero = "F-2";
        segunda.Detalles = [segunda.Detalles[0]];
        await _servicio.CrearAsync(segunda, SemillaInicial.UsuarioAdminId, CancellationToken.None);

        var informe = await _servicio.ConsultarItemsAsync(
            null, null, catalogos.ProveedorId, catalogos.ConstruccionId, true, 1, 20, false, CancellationToken.None);
        var ladrillo = informe.Valor!.Items.Single(item => item.Insumo == "Ladrillo");

        Assert.Equal(4m, ladrillo.Cantidad);
        Assert.Equal(400m, ladrillo.Subtotal);
        Assert.Equal(450m, informe.Valor.Total);
    }

    [Fact]
    public async Task El_pdf_de_la_factura_tiene_contenido()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var catalogos = await SembrarAsync();
        var creada = await _servicio.CrearAsync(Solicitud(catalogos), SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var pdf = await _servicio.PdfAsync(creada.Valor!.Id, CancellationToken.None);

        Assert.Equal(EstadoFactura.Ok, pdf.Estado);
        Assert.True(pdf.Valor!.Length > 100);
    }

    [Fact]
    public async Task La_unidad_sale_del_material_aunque_el_renglon_traiga_otra()
    {
        var catalogos = await SembrarAsync();
        var otra = new UnidadMedida { Nombre = "M2", Activo = true };
        _context.UnidadesMedida.Add(otra);
        await _context.SaveChangesAsync();
        var solicitud = Solicitud(catalogos);
        solicitud.Detalles[0].UnidadMedidaId = otra.Id;

        var creada = await _servicio.CrearAsync(solicitud, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var factura = await _servicio.ObtenerAsync(creada.Valor!.Id, CancellationToken.None);

        Assert.Equal(catalogos.UnidadId, factura.Valor!.Detalles[0].UnidadMedidaId);
        Assert.Equal("UND", factura.Valor.Detalles[0].UnidadMedida);
    }

    public void Dispose()
    {
        _context.Dispose();
        _conexion.Dispose();
    }

    private async Task<Catalogos> SembrarAsync()
    {
        var proveedor = new Proveedor { Nombre = "Ladrillera", Activo = true };
        var otro = new Proveedor { Nombre = "Ferretería", Activo = true };
        var obra = new Construccion { Descripcion = "CASA 1", Activo = true };
        var unidad = new UnidadMedida { Nombre = "UND", Activo = true };
        var ladrillo = Insumo("LAD", "Ladrillo");
        var cemento = Insumo("CEM", "Cemento");
        _context.AddRange(proveedor, otro, obra, unidad, ladrillo, cemento);
        await _context.SaveChangesAsync();
        return new Catalogos(proveedor.Id, otro.Id, obra.Id, unidad.Id, ladrillo.Id, cemento.Id);
    }

    private static Insumo Insumo(string codigo, string descripcion) =>
        new()
        {
            Codigo = codigo,
            Descripcion = descripcion,
            Tipo = TipoInsumo.Material,
            UnidadCompra = "UND",
            PrecioBase = 100,
            PorcentajeIva = 0,
            CantidadPorUnidad = 1,
            UnidadUso = "UND",
            PrecioUnitario = 100,
            FechaPrecio = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            Activo = true
        };

    private static CrearFacturaSolicitud Solicitud(Catalogos catalogos) =>
        new()
        {
            Numero = "F-1",
            Fecha = new DateOnly(2026, 11, 24),
            ProveedorId = catalogos.ProveedorId,
            OrdenCompra = "OC-10",
            Observacion = "Ladrillo de la casa 1",
            Detalles =
            [
                new LineaFacturaSolicitud
                {
                    ConstruccionId = catalogos.ConstruccionId,
                    InsumoId = catalogos.LadrilloId,
                    UnidadMedidaId = catalogos.UnidadId,
                    Cantidad = 2,
                    Precio = 100,
                    Descripcion = "Ladrillo tolete"
                },
                new LineaFacturaSolicitud
                {
                    ConstruccionId = catalogos.ConstruccionId,
                    InsumoId = catalogos.CementoId,
                    UnidadMedidaId = catalogos.UnidadId,
                    Cantidad = 1,
                    Precio = 50,
                    Descripcion = "Cemento"
                }
            ]
        };

    private sealed record Catalogos(
        int ProveedorId,
        int OtroProveedorId,
        int ConstruccionId,
        int UnidadId,
        int LadrilloId,
        int CementoId);
}
