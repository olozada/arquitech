using Arquitectos.Application.Dashboard;
using Arquitectos.Application.Presupuestos;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Dashboard;
using Arquitectos.Infrastructure.Persistence;
using Arquitectos.Infrastructure.Presupuestos;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Arquitectos.Tests;

public sealed class CreacionPresupuestoTests : IDisposable
{
    private readonly SqliteConnection _conexion;
    private readonly ArquitectosDbContext _context;
    private readonly PresupuestoServicio _servicio;
    private readonly int _admin = SemillaInicial.UsuarioAdminId;

    public CreacionPresupuestoTests()
    {
        _conexion = new SqliteConnection("Data Source=:memory:");
        _conexion.Open();
        var opciones = new DbContextOptionsBuilder<ArquitectosDbContext>().UseSqlite(_conexion).Options;
        _context = new ArquitectosDbContext(opciones);
        _context.Database.EnsureCreated();
        _servicio = new PresupuestoServicio(_context, new MotorCostos(_context));
    }

    [Fact]
    public async Task Crear_en_blanco_y_rechaza_un_presupuesto_sin_construccion()
    {
        var obra = await ObraAsync();
        var creado = await _servicio.CrearDesdeAsync(new CrearPresupuestoSolicitud
        {
            ConstruccionId = obra.Id,
            Nombre = "Clínica",
            AreaConstruidaM2 = 80,
            Origen = "vacio"
        }, _admin, CancellationToken.None);
        Assert.Equal(EstadoOperacion.Ok, creado.Estado);
        var detalle = await _servicio.ObtenerAsync(creado.Valor!.Id, _admin, CancellationToken.None);
        Assert.Empty(detalle.Valor!.Capitulos);
        Assert.Equal(0.05m, detalle.Valor.PctImprevistos);
        Assert.Equal(0.19m, detalle.Valor.PctIvaUtilidad);

        var invalido = await _servicio.CrearDesdeAsync(new CrearPresupuestoSolicitud { AreaConstruidaM2 = 10, Origen = "vacio" }, _admin, CancellationToken.None);
        Assert.Equal(EstadoOperacion.Validacion, invalido.Estado);
    }

    [Fact]
    public async Task La_plantilla_copia_el_analisis_sin_cantidades_y_no_se_aprueba()
    {
        var obra = await ObraAsync();
        var concepto = new Concepto { Descripcion = "Muro de bloque" };
        var apu = new Apu { Descripcion = "Muro", Unidad = "M2", DivisorGeneral = 1, Concepto = concepto };
        _context.AddRange(concepto, apu);
        await _context.SaveChangesAsync();
        var origen = await _servicio.CrearAsync(Presupuesto(obra.Id, new ItemPresupuestoSolicitud
        {
            Codigo = "1.01",
            Descripcion = "Muro",
            Unidad = "M2",
            ApuId = apu.Id,
            ConceptoId = concepto.Id,
            Cantidad = 12,
            ValorMaterialUnitario = 10
        }), _admin, CancellationToken.None);
        var plantilla = await _servicio.GuardarComoPlantillaAsync(origen.Valor!.Id, "Vivienda base", _admin, CancellationToken.None);
        var guardada = await _context.Presupuestos.Include(item => item.Capitulos).ThenInclude(item => item.Items).FirstAsync(item => item.Id == plantilla.Valor!.Id);
        Assert.True(guardada.EsPlantilla);
        Assert.Null(guardada.ConstruccionId);
        Assert.Equal(0, guardada.Capitulos.Single().Items.Single().Cantidad);

        var nuevo = await _servicio.CrearDesdeAsync(new CrearPresupuestoSolicitud
        {
            ConstruccionId = obra.Id,
            AreaConstruidaM2 = 40,
            Origen = "plantilla",
            OrigenPresupuestoId = plantilla.Valor.Id
        }, _admin, CancellationToken.None);
        var detalle = (await _servicio.ObtenerAsync(nuevo.Valor!.Id, _admin, CancellationToken.None)).Valor!;
        var item = detalle.Capitulos.Single().Items.Single();
        Assert.Equal(0, item.Cantidad);
        Assert.Equal(apu.Id, item.ApuId);
        Assert.Equal(concepto.Id, item.ConceptoId);
        Assert.Equal(plantilla.Valor.Id, (await _context.Presupuestos.FindAsync(detalle.Id))!.PresupuestoOrigenId);

        Assert.Equal(EstadoOperacion.Validacion, (await _servicio.AprobarAsync(plantilla.Valor.Id, _admin, CancellationToken.None)).Estado);
        Assert.Equal(EstadoOperacion.Validacion, (await _servicio.SeguimientoAsync(plantilla.Valor.Id, _admin, CancellationToken.None)).Estado);
        Assert.Empty(await _servicio.ItemsParaActaAsync(obra.Id, true, CancellationToken.None));
    }

    [Fact]
    public async Task Copiar_otra_obra_conserva_o_limpia_cantidades_y_remapea_el_origen()
    {
        var obra = await ObraAsync();
        var destino = await ObraAsync("Otra obra");
        var creado = await _servicio.CrearAsync(Presupuesto(obra.Id, new ItemPresupuestoSolicitud
        {
            Codigo = "1.01",
            Descripcion = "Base",
            Cantidad = 4,
            ValorMaterialUnitario = 10,
            Mediciones = [new MedicionSolicitud { Veces = 4 }]
        }, new ItemPresupuestoSolicitud
        {
            Codigo = "1.02",
            Descripcion = "Referencia",
            CantidadOrigenFactor = 2,
            ValorManoObraUnitario = 3
        }), _admin, CancellationToken.None);
        var baseId = creado.Valor!.Capitulos[0].Items.Single(item => item.Codigo == "1.01").Id;
        await _servicio.ActualizarAsync(creado.Valor.Id, Presupuesto(obra.Id, new ItemPresupuestoSolicitud
        {
            Id = baseId,
            Codigo = "1.01",
            Descripcion = "Base",
            Cantidad = 4,
            ValorMaterialUnitario = 10,
            Mediciones = [new MedicionSolicitud { Veces = 4 }]
        }, new ItemPresupuestoSolicitud
        {
            Id = creado.Valor.Capitulos[0].Items.Single(item => item.Codigo == "1.02").Id,
            Codigo = "1.02",
            Descripcion = "Referencia",
            CantidadOrigenItemId = baseId,
            CantidadOrigenFactor = 2,
            ValorManoObraUnitario = 3
        }), _admin, CancellationToken.None);

        var sinCantidades = await CopiarAsync(destino.Id, creado.Valor.Id, false);
        var limpio = sinCantidades.Capitulos[0].Items.Single(item => item.Codigo == "1.02");
        Assert.Equal(0, limpio.Cantidad);
        Assert.Null(limpio.CantidadOrigenItemId);
        Assert.Empty(sinCantidades.Capitulos[0].Items.Single(item => item.Codigo == "1.01").Mediciones);

        var conCantidades = await CopiarAsync(destino.Id, creado.Valor.Id, true);
        var referencia = conCantidades.Capitulos[0].Items.Single(item => item.Codigo == "1.02");
        var origenNuevo = conCantidades.Capitulos[0].Items.Single(item => item.Codigo == "1.01");
        Assert.Equal(origenNuevo.Id, referencia.CantidadOrigenItemId);
        Assert.Equal(8, referencia.Cantidad);
        Assert.NotEmpty(origenNuevo.Mediciones);
    }

    [Fact]
    public async Task Una_obra_aprobada_abre_la_siguiente_version_en_borrador()
    {
        var obra = await ObraAsync();
        var primero = await _servicio.CrearAsync(Presupuesto(obra.Id, ItemValido()), _admin, CancellationToken.None);
        Assert.Equal(EstadoOperacion.Ok, (await _servicio.AprobarAsync(primero.Valor!.Id, _admin, CancellationToken.None)).Estado);
        var segundo = await _servicio.CrearDesdeAsync(new CrearPresupuestoSolicitud
        {
            ConstruccionId = obra.Id,
            AreaConstruidaM2 = 10,
            Origen = "vacio"
        }, _admin, CancellationToken.None);
        var detalle = (await _servicio.ObtenerAsync(segundo.Valor!.Id, _admin, CancellationToken.None)).Valor!;
        Assert.Equal(2, detalle.Version);
        Assert.Equal("Borrador", detalle.Estado);
    }

    [Fact]
    public async Task Desde_apu_renumera_ordena_duplica_y_mueve()
    {
        var obra = await ObraAsync();
        var primero = new Apu { Descripcion = "Análisis uno", Unidad = "UND", DivisorGeneral = 1 };
        var segundo = new Apu { Descripcion = "Análisis dos", Unidad = "UND", DivisorGeneral = 1 };
        _context.AddRange(primero, segundo);
        await _context.SaveChangesAsync();
        var creado = await _servicio.CrearAsync(new PresupuestoSolicitud
        {
            ConstruccionId = obra.Id,
            Nombre = "Obra",
            AreaConstruidaM2 = 10,
            Capitulos =
            [
                new CapituloPresupuestoSolicitud { Numero = 1, Nombre = "Uno", Items = [ItemValido()] },
                new CapituloPresupuestoSolicitud { Numero = 4, Nombre = "Dos", Items = [ItemValido("9.09")] }
            ]
        }, _admin, CancellationToken.None);
        var capitulo = creado.Valor!.Capitulos[0];
        var agregados = await _servicio.AgregarDesdeApuAsync(creado.Valor.Id, capitulo.Id, new ItemsDesdeApuSolicitud { ApuIds = [primero.Id, segundo.Id] }, _admin, CancellationToken.None);
        var codigos = agregados.Valor!.Capitulos[0].Items.Select(item => item.Codigo).ToList();
        Assert.Contains("1.02", codigos);
        Assert.Contains("1.03", codigos);

        var renumerado = await _servicio.RenumerarAsync(creado.Valor.Id, null, _admin, CancellationToken.None);
        Assert.Equal("1.01", renumerado.Valor!.Capitulos[0].Items[0].Codigo);
        Assert.Equal("2.01", renumerado.Valor.Capitulos[1].Items[0].Codigo);

        var capitulos = renumerado.Valor.Capitulos;
        var ordenado = await _servicio.OrdenarAsync(creado.Valor.Id, new OrdenPresupuestoSolicitud
        {
            Capitulos =
            [
                new OrdenItemSolicitud { Id = capitulos[1].Id, Orden = 1, Items = capitulos[1].Items.Select(item => item.Id).ToList() },
                new OrdenItemSolicitud { Id = capitulos[0].Id, Orden = 2, Items = capitulos[0].Items.Select(item => item.Id).ToList() }
            ]
        }, _admin, CancellationToken.None);
        Assert.Equal(capitulos[1].Id, ordenado.Valor!.Capitulos[0].Id);

        var duplicado = await _servicio.DuplicarItemAsync(creado.Valor.Id, ordenado.Valor.Capitulos[0].Items[0].Id, null, _admin, CancellationToken.None);
        Assert.Equal(2, duplicado.Valor!.Capitulos[0].Items.Count);

        var movido = await _servicio.MoverItemAsync(creado.Valor.Id, duplicado.Valor.Capitulos[0].Items[0].Id, new MoverItemSolicitud { CapituloDestinoId = duplicado.Valor.Capitulos[1].Id }, _admin, CancellationToken.None);
        Assert.Contains(movido.Valor!.Capitulos[1].Items, item => item.Id == duplicado.Valor.Capitulos[0].Items[0].Id);
    }

    [Fact]
    public async Task El_codigo_repetido_y_la_version_vieja_se_rechazan()
    {
        var obra = await ObraAsync();
        var creado = await _servicio.CrearAsync(Presupuesto(obra.Id, ItemValido()), _admin, CancellationToken.None);
        var repetido = await _servicio.ActualizarAsync(creado.Valor!.Id, Presupuesto(obra.Id, ItemValido(), ItemValido("1.01")), _admin, CancellationToken.None);
        Assert.Equal(EstadoOperacion.Validacion, repetido.Estado);

        var token = creado.Valor.RowVersion;
        var primero = await _servicio.ActualizarAsync(creado.Valor.Id, PresupuestoConVersion(obra.Id, token, ItemValido("1.02")), _admin, CancellationToken.None);
        Assert.Equal(EstadoOperacion.Ok, primero.Estado);
        var conflicto = await _servicio.ActualizarAsync(creado.Valor.Id, PresupuestoConVersion(obra.Id, token, ItemValido("1.03")), _admin, CancellationToken.None);
        Assert.Equal(EstadoOperacion.Conflicto, conflicto.Estado);
        Assert.Equal("El presupuesto fue modificado por otro usuario.", conflicto.Mensaje);
    }

    [Fact]
    public async Task La_validacion_cubre_los_cuatro_tipos_y_bloquea_la_aprobacion()
    {
        var obra = await ObraAsync();
        var apu = new Apu { Descripcion = "Losa", Unidad = "M2", DivisorGeneral = 1 };
        _context.Apus.Add(apu);
        await _context.SaveChangesAsync();
        var creado = await _servicio.CrearAsync(Presupuesto(obra.Id,
            new ItemPresupuestoSolicitud { Codigo = "1.01", Descripcion = "Losa", Unidad = "ML", ApuId = apu.Id, Cantidad = 0, ValorMaterialUnitario = 8 },
            new ItemPresupuestoSolicitud { Codigo = "1.02", Descripcion = "Vacío", Cantidad = 0 }), _admin, CancellationToken.None);
        var vacio = await _context.Set<PresupuestoItem>().SingleAsync(item => item.Codigo == "1.02");
        vacio.Codigo = "1.01";
        await _context.SaveChangesAsync();
        var avisos = await _servicio.ValidarAsync(creado.Valor!.Id, _admin, CancellationToken.None);
        var tipos = avisos.Valor!.Select(aviso => aviso.Tipo).ToHashSet();
        Assert.Contains("UnidadDistintaApu", tipos);
        Assert.Contains("CantidadCero", tipos);
        Assert.Contains("SinApuNiValores", tipos);
        Assert.Contains("CodigoRepetido", tipos);
        Assert.Equal(EstadoOperacion.Validacion, (await _servicio.AprobarAsync(creado.Valor.Id, _admin, CancellationToken.None)).Estado);

        var soloCeros = await _servicio.CrearAsync(Presupuesto(obra.Id, new ItemPresupuestoSolicitud
        {
            Codigo = "2.01",
            Descripcion = "Pendiente",
            Cantidad = 0,
            ValorMaterialUnitario = 5
        }), _admin, CancellationToken.None);
        Assert.Equal(EstadoOperacion.Validacion, (await _servicio.AprobarAsync(soloCeros.Valor!.Id, _admin, CancellationToken.None)).Estado);
        Assert.Equal(EstadoOperacion.Ok, (await _servicio.AprobarAsync(soloCeros.Valor.Id, _admin, CancellationToken.None, true)).Estado);
    }

    [Fact]
    public async Task La_plantilla_aprobada_a_la_fuerza_no_entra_al_tablero_ni_a_las_actas()
    {
        var obra = await ObraAsync();
        var plantilla = new Presupuesto
        {
            EsPlantilla = true,
            ConstruccionId = obra.Id,
            Nombre = "No cuenta",
            Estado = EstadoPresupuesto.Aprobado,
            Fecha = new DateOnly(2026, 1, 1),
            Capitulos =
            [
                new PresupuestoCapitulo
                {
                    Numero = 1,
                    Nombre = "Uno",
                    Items = [new PresupuestoItem { Codigo = "1.01", Descripcion = "X", Cantidad = 5, ValorMaterialUnitario = 100 }]
                }
            ]
        };
        _context.Presupuestos.Add(plantilla);
        await _context.SaveChangesAsync();
        Assert.Empty(await _servicio.ItemsParaActaAsync(obra.Id, true, CancellationToken.None));
        var tablero = new DashboardServicio(_context, Options.Create(new OpcionesDashboard()), new MemoryCache(new MemoryCacheOptions()));
        var datos = await tablero.ObtenerAsync(new FiltroDashboard { Desde = new DateOnly(2026, 1, 1), Hasta = new DateOnly(2026, 12, 31) }, CancellationToken.None);
        Assert.Equal(0, datos.Kpis.Single(kpi => kpi.Codigo == "K9").Valor);
    }

    [Fact]
    public async Task La_semilla_de_vivienda_unifamiliar_es_idempotente()
    {
        Assert.True(File.Exists(SemillaPlantilla.BuscarJson()));
        var primera = await SemillaPlantilla.AplicarAsync(_context);
        var segunda = await SemillaPlantilla.AplicarAsync(_context);
        Assert.Contains("20", primera);
        Assert.Contains("139", primera);
        Assert.Contains("ya existe", segunda);
        var plantilla = await _context.Presupuestos.Include(item => item.Capitulos).ThenInclude(item => item.Items).SingleAsync(item => item.Nombre == SemillaPlantilla.Nombre);
        Assert.Equal(20, plantilla.Capitulos.Count);
        Assert.Equal(139, plantilla.Capitulos.Sum(capitulo => capitulo.Items.Count));
        Assert.All(plantilla.Capitulos.SelectMany(capitulo => capitulo.Items), item => Assert.Equal(0, item.Cantidad));
        Assert.Null(plantilla.ConstruccionId);
    }

    private async Task<PresupuestoDetalleDto> CopiarAsync(int obraId, int origenId, bool conservar)
    {
        var creado = await _servicio.CrearDesdeAsync(new CrearPresupuestoSolicitud
        {
            ConstruccionId = obraId,
            AreaConstruidaM2 = 10,
            Origen = "copia",
            OrigenPresupuestoId = origenId,
            ConservarCantidades = conservar
        }, _admin, CancellationToken.None);
        return (await _servicio.ObtenerAsync(creado.Valor!.Id, _admin, CancellationToken.None)).Valor!;
    }

    private async Task<Construccion> ObraAsync(string nombre = "Obra nueva")
    {
        var obra = new Construccion { Descripcion = nombre };
        _context.Construcciones.Add(obra);
        await _context.SaveChangesAsync();
        return obra;
    }

    private static ItemPresupuestoSolicitud ItemValido(string codigo = "1.01") => new()
    {
        Codigo = codigo,
        Descripcion = "Ítem",
        Cantidad = 2,
        ValorMaterialUnitario = 10
    };

    private static PresupuestoSolicitud Presupuesto(int obraId, params ItemPresupuestoSolicitud[] items) => new()
    {
        ConstruccionId = obraId,
        Nombre = "Prueba",
        AreaConstruidaM2 = 10,
        Capitulos = [new CapituloPresupuestoSolicitud { Numero = 1, Nombre = "Capítulo", Items = items.ToList() }]
    };

    private static PresupuestoSolicitud PresupuestoConVersion(int obraId, string version, ItemPresupuestoSolicitud item)
    {
        var solicitud = Presupuesto(obraId, item);
        solicitud.RowVersion = version;
        return solicitud;
    }

    public void Dispose()
    {
        _context.Dispose();
        _conexion.Dispose();
    }
}
