using Arquitectos.Application.Presupuestos;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using Arquitectos.Infrastructure.Presupuestos;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Tests;

public sealed class PresupuestoServicioTests : IDisposable
{
    private readonly SqliteConnection _conexion;
    private readonly ArquitectosDbContext _context;
    private readonly PresupuestoServicio _servicio;

    public PresupuestoServicioTests()
    {
        _conexion = new SqliteConnection("Data Source=:memory:");
        _conexion.Open();
        var opciones = new DbContextOptionsBuilder<ArquitectosDbContext>()
            .UseSqlite(_conexion)
            .Options;
        _context = new ArquitectosDbContext(opciones);
        _context.Database.EnsureCreated();
        _servicio = new PresupuestoServicio(_context, new MotorCostos(_context));
    }

    [Fact]
    public async Task Las_mediciones_la_cantidad_referenciada_y_el_aiu_cuadran()
    {
        var obra = new Construccion { Descripcion = "Obra presupuesto" };
        _context.Construcciones.Add(obra);
        await _context.SaveChangesAsync();

        var creado = await _servicio.CrearAsync(Solicitud(obra.Id, 0.10m, 0.05m, 0.19m, Medicion()), SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var detalle = creado.Valor!;
        var item = detalle.Capitulos[0].Items[0];
        Assert.Equal(24m, item.Cantidad);

        var actualizado = await _servicio.ActualizarAsync(detalle.Id, Solicitud(
            obra.Id,
            0.10m,
            0.05m,
            0.19m,
            Medicion(),
            new ItemPresupuestoSolicitud
            {
                Codigo = "1.02",
                Descripcion = "Igual al anterior",
                CantidadOrigenItemId = item.Id,
                CantidadOrigenFactor = 2,
                ValorMaterialUnitario = 10
            },
            item.Id), SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var conReferencia = actualizado.Valor!;
        Assert.Equal(48m, conReferencia.Capitulos[0].Items.Single(fila => fila.Codigo == "1.02").Cantidad);
        Assert.Equal(720m, conReferencia.Resumen.Subtotal);
        Assert.Equal(921.24m, conReferencia.Resumen.Total);
        Assert.Equal(92.124m, conReferencia.Resumen.ValorPorM2);

        var sinAiu = await _servicio.ActualizarAsync(detalle.Id, Solicitud(
            obra.Id,
            0,
            0,
            0,
            Medicion(),
            new ItemPresupuestoSolicitud
            {
                Codigo = "1.02",
                Descripcion = "Igual al anterior",
                CantidadOrigenItemId = item.Id,
                CantidadOrigenFactor = 2,
                ValorMaterialUnitario = 10
            },
            item.Id), SemillaInicial.UsuarioAdminId, CancellationToken.None);
        Assert.Equal(806.4m, sinAiu.Valor!.Resumen.Total);
    }

    [Fact]
    public async Task Aprobar_congela_los_precios_y_la_nueva_version_cierra_la_anterior()
    {
        var obra = new Construccion { Descripcion = "Obra congelada" };
        var insumo = new Insumo
        {
            Descripcion = "Cemento",
            Tipo = TipoInsumo.Material,
            PrecioBase = 100,
            CantidadPorUnidad = 1,
            PrecioUnitario = 100,
            FechaPrecio = DateTime.UtcNow
        };
        _context.AddRange(obra, insumo);
        await _context.SaveChangesAsync();
        var apu = new Apu
        {
            Descripcion = "Muro",
            Unidad = "M2",
            Grupos =
            [
                new ApuGrupo
                {
                    Grupo = GrupoApu.Material,
                    Divisor = 1,
                    Lineas = [new ApuLinea { Descripcion = "Cemento", Cantidad = 1, InsumoId = insumo.Id, Modo = ModoLineaApu.Desperdicio }]
                }
            ]
        };
        _context.Apus.Add(apu);
        await _context.SaveChangesAsync();

        var creado = await _servicio.CrearAsync(new PresupuestoSolicitud
        {
            ConstruccionId = obra.Id,
            Nombre = "Base",
            AreaConstruidaM2 = 10,
            PctImprevistos = 0.05m,
            PctIncremento = 0.07m,
            Capitulos =
            [
                new CapituloPresupuestoSolicitud
                {
                    Numero = 1,
                    Nombre = "Obra",
                    Items = [new ItemPresupuestoSolicitud { Codigo = "1.01", Descripcion = "Muro", Unidad = "M2", ApuId = apu.Id, Cantidad = 2 }]
                }
            ]
        }, SemillaInicial.UsuarioAdminId, CancellationToken.None);

        Assert.Equal(100m, creado.Valor!.Capitulos[0].Items[0].ValorMaterialUnitario);
        Assert.Equal(224m, creado.Valor.Resumen.Total);

        var aprobado = await _servicio.AprobarAsync(creado.Valor.Id, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        insumo.PrecioUnitario = 500;
        insumo.PrecioBase = 500;
        await _context.SaveChangesAsync();
        var congelado = await _servicio.ObtenerAsync(creado.Valor.Id, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        Assert.Equal(100m, congelado.Valor!.Capitulos[0].Items[0].ValorMaterialUnitario);
        Assert.Contains("Total", congelado.Valor.Capitulos[0].Items[0].Apu is not null ? (await _context.Set<PresupuestoItem>().Where(item => item.Id == congelado.Valor.Capitulos[0].Items[0].Id).Select(item => item.DesgloseApuJson).SingleAsync())! : "");

        var vista = await _servicio.RecalcularAsync(creado.Valor.Id, false, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        Assert.NotEmpty(vista.Valor!.Diferencias);
        var rechazado = await _servicio.RecalcularAsync(creado.Valor.Id, true, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        Assert.Equal(EstadoOperacion.Validacion, rechazado.Estado);

        var copia = await _servicio.NuevaVersionAsync(creado.Valor.Id, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        Assert.Equal(EstadoPresupuesto.Borrador.ToString(), copia.Valor!.Estado);
        Assert.Equal(aprobado.Valor!.Version + 1, copia.Valor.Version);
        Assert.Equal(100m, copia.Valor.Capitulos[0].Items[0].ValorMaterialUnitario);

        var segunda = await _servicio.AprobarAsync(copia.Valor.Id, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        Assert.Equal(500m, segunda.Valor!.Capitulos[0].Items[0].ValorMaterialUnitario);
        var primera = await _servicio.ObtenerAsync(creado.Valor.Id, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        Assert.Equal(EstadoPresupuesto.Aprobado.ToString(), segunda.Valor!.Estado);
        Assert.Equal(EstadoPresupuesto.Cerrado.ToString(), primera.Valor!.Estado);
        Assert.Equal(1, await _context.Presupuestos.CountAsync(item => item.ConstruccionId == obra.Id && item.Estado == EstadoPresupuesto.Aprobado));
        var sinCambios = await _servicio.ObtenerAsync(creado.Valor.Id, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        Assert.Equal(100m, sinCambios.Valor!.Capitulos[0].Items[0].ValorMaterialUnitario);
    }

    [Fact]
    public async Task El_area_cero_no_calcula_valor_por_metro_y_el_factor_de_conversion_quita_el_aviso()
    {
        var obra = new Construccion { Descripcion = "Obra area" };
        _context.Construcciones.Add(obra);
        await _context.SaveChangesAsync();
        var creado = await _servicio.CrearAsync(new PresupuestoSolicitud
        {
            ConstruccionId = obra.Id,
            Nombre = "Sin área",
            AreaConstruidaM2 = 0,
            Capitulos =
            [
                new CapituloPresupuestoSolicitud
                {
                    Numero = 1,
                    Nombre = "Capítulo",
                    Items = [new ItemPresupuestoSolicitud { Codigo = "1.01", Descripcion = "Muro de prueba", Unidad = "ML", Cantidad = 2, ValorMaterialUnitario = 10 }]
                }
            ]
        }, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        Assert.Null(creado.Valor!.Resumen.ValorPorM2);
        Assert.NotNull(creado.Valor.Capitulos[0].Items[0].ConceptoId);

        var apu = new Apu { Descripcion = "Análisis", Unidad = "M2", Grupos = [new ApuGrupo { Grupo = GrupoApu.Material, Divisor = 1 }] };
        _context.Apus.Add(apu);
        await _context.SaveChangesAsync();
        var conFactor = await _servicio.ActualizarAsync(creado.Valor.Id, new PresupuestoSolicitud
        {
            ConstruccionId = obra.Id,
            Nombre = "Sin área",
            AreaConstruidaM2 = 0,
            RowVersion = creado.Valor.RowVersion,
            Capitulos =
            [
                new CapituloPresupuestoSolicitud
                {
                    Id = creado.Valor.Capitulos[0].Id,
                    Numero = 1,
                    Nombre = "Capítulo",
                    Items =
                    [
                        new ItemPresupuestoSolicitud
                        {
                            Id = creado.Valor.Capitulos[0].Items[0].Id,
                            Codigo = "1.01",
                            Descripcion = "Muro de prueba",
                            Unidad = "ML",
                            ApuId = apu.Id,
                            FactorConversionApu = 2,
                            Cantidad = 2,
                            ConceptoId = creado.Valor.Capitulos[0].Items[0].ConceptoId
                        }
                    ]
                }
            ]
        }, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var avisos = await _servicio.ValidarAsync(creado.Valor.Id, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        Assert.DoesNotContain(avisos.Valor!, aviso => aviso.Tipo == "UnidadDistintaApu");
        Assert.Equal(EstadoOperacion.Ok, conFactor.Estado);
    }

    [Fact]
    public async Task Borrar_una_medicion_actualiza_la_cantidad_y_un_ciclo_se_rechaza()
    {
        var obra = new Construccion { Descripcion = "Obra mediciones" };
        _context.Construcciones.Add(obra);
        await _context.SaveChangesAsync();
        var creado = await _servicio.CrearAsync(Solicitud(obra.Id, 0, 0, 0, Medicion()), SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var item = creado.Valor!.Capitulos[0].Items[0];
        Assert.Equal(24m, item.Cantidad);
        var sinMedicion = await _servicio.ActualizarAsync(creado.Valor.Id, new PresupuestoSolicitud
        {
            ConstruccionId = obra.Id,
            Nombre = "Prueba",
            AreaConstruidaM2 = 10,
            PctImprevistos = 0.05m,
            PctIncremento = 0.07m,
            RowVersion = creado.Valor.RowVersion,
            Capitulos =
            [
                new CapituloPresupuestoSolicitud
                {
                    Id = creado.Valor.Capitulos[0].Id,
                    Numero = 1,
                    Nombre = "Capítulo",
                    Items = [new ItemPresupuestoSolicitud { Id = item.Id, Codigo = "1.01", Descripcion = "Muro", Cantidad = 3, ValorMaterialUnitario = 10 }]
                }
            ]
        }, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        Assert.Equal(3m, sinMedicion.Valor!.Capitulos[0].Items[0].Cantidad);

        var segundo = await _servicio.ActualizarAsync(creado.Valor.Id, new PresupuestoSolicitud
        {
            ConstruccionId = obra.Id,
            Nombre = "Prueba",
            AreaConstruidaM2 = 10,
            RowVersion = sinMedicion.Valor.RowVersion,
            Capitulos =
            [
                new CapituloPresupuestoSolicitud
                {
                    Id = creado.Valor.Capitulos[0].Id,
                    Numero = 1,
                    Nombre = "Capítulo",
                    Items =
                    [
                        new ItemPresupuestoSolicitud { Id = item.Id, Codigo = "1.01", Descripcion = "Muro", CantidadOrigenItemId = item.Id + 1, CantidadOrigenFactor = 2 },
                        new ItemPresupuestoSolicitud { Codigo = "1.02", Descripcion = "Destino", CantidadOrigenItemId = item.Id, CantidadOrigenFactor = 2 }
                    ]
                }
            ]
        }, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        Assert.Equal(EstadoOperacion.Ok, segundo.Estado);
        var destino = segundo.Valor!.Capitulos[0].Items.Single(fila => fila.Codigo == "1.02");
        var ciclo = await _servicio.ActualizarAsync(creado.Valor.Id, new PresupuestoSolicitud
        {
            ConstruccionId = obra.Id,
            Nombre = "Prueba",
            AreaConstruidaM2 = 10,
            RowVersion = segundo.Valor.RowVersion,
            Capitulos =
            [
                new CapituloPresupuestoSolicitud
                {
                    Id = creado.Valor.Capitulos[0].Id,
                    Numero = 1,
                    Nombre = "Capítulo",
                    Items =
                    [
                        new ItemPresupuestoSolicitud { Id = item.Id, Codigo = "1.01", Descripcion = "Muro", CantidadOrigenItemId = destino.Id, CantidadOrigenFactor = 2 },
                        new ItemPresupuestoSolicitud { Id = destino.Id, Codigo = "1.02", Descripcion = "Destino", CantidadOrigenItemId = item.Id, CantidadOrigenFactor = 2 }
                    ]
                }
            ]
        }, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        Assert.Equal(EstadoOperacion.Validacion, ciclo.Estado);
    }

    [Fact]
    public async Task Aprobar_un_item_sin_concepto_se_rechaza()
    {
        var obra = new Construccion { Descripcion = "Obra concepto" };
        _context.Construcciones.Add(obra);
        await _context.SaveChangesAsync();
        var creado = await _servicio.CrearAsync(new PresupuestoSolicitud
        {
            ConstruccionId = obra.Id,
            Nombre = "Concepto",
            AreaConstruidaM2 = 1,
            Capitulos =
            [
                new CapituloPresupuestoSolicitud
                {
                    Numero = 1,
                    Nombre = "Capítulo",
                    Items = [new ItemPresupuestoSolicitud { Codigo = "1.01", Descripcion = "", Cantidad = 1, ValorMaterialUnitario = 5 }]
                }
            ]
        }, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var item = await _context.Set<PresupuestoItem>().SingleAsync();
        item.ConceptoId = null;
        await _context.SaveChangesAsync();
        var avisos = await _servicio.ValidarAsync(creado.Valor!.Id, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        Assert.Contains(avisos.Valor!, aviso => aviso.Tipo == "SinConcepto");
        var aprobado = await _servicio.AprobarAsync(creado.Valor.Id, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        Assert.Equal(EstadoOperacion.Validacion, aprobado.Estado);
    }

    private static PresupuestoSolicitud Solicitud(
        int obraId,
        decimal administracion,
        decimal utilidad,
        decimal iva,
        MedicionSolicitud medicion,
        ItemPresupuestoSolicitud? extra = null,
        int? primerId = null)
    {
        var primero = new ItemPresupuestoSolicitud
        {
            Id = primerId,
            Codigo = "1.01",
            Descripcion = "Muro",
            ValorMaterialUnitario = 10,
            Mediciones = [medicion]
        };
        var items = new List<ItemPresupuestoSolicitud> { primero };
        if (extra is not null)
        {
            items.Add(extra);
        }

        return new PresupuestoSolicitud
        {
            ConstruccionId = obraId,
            Nombre = "Prueba",
            AreaConstruidaM2 = 10,
            PctImprevistos = 0.05m,
            PctIncremento = 0.07m,
            PctAdministracion = administracion,
            PctUtilidad = utilidad,
            PctIvaUtilidad = iva,
            Capitulos = [new CapituloPresupuestoSolicitud { Numero = 1, Nombre = "Capítulo", Items = items }]
        };
    }

    private static MedicionSolicitud Medicion() => new() { Ubicacion = "Sala", Veces = 2, Largo = 3, Alto = 4 };

    public void Dispose()
    {
        _context.Dispose();
        _conexion.Dispose();
    }
}
