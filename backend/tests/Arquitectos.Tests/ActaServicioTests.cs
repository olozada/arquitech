using Arquitectos.Application.Actas;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Actas;
using Arquitectos.Infrastructure.Persistence;
using QuestPDF.Infrastructure;
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
    public async Task Una_linea_puede_omitir_unidad_material_y_descripcion()
    {
        var catalogos = await SembrarCatalogosAsync();
        var solicitud = Solicitud(catalogos);
        solicitud.Detalles[0].UnidadMedidaId = null;
        solicitud.Detalles[0].InsumoId = null;
        solicitud.Detalles[0].Descripcion = "";

        var creada = await _servicio.CrearAsync(solicitud, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var acta = await _servicio.ObtenerAsync(creada.Valor!.NumeroActa, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var linea = acta.Valor!.Detalles.Single();

        Assert.Null(linea.UnidadMedidaId);
        Assert.Equal("", linea.UnidadMedida);
        Assert.Null(linea.InsumoId);
        Assert.Equal("", linea.Descripcion);
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
            null,
            null,
            null,
            null,
            SemillaInicial.UsuarioAdminId,
            CancellationToken.None);
        var acta = await _servicio.ObtenerAsync(creada.Valor!.NumeroActa, SemillaInicial.UsuarioAdminId, CancellationToken.None);

        Assert.Equal(EstadoActa.Ok, creada.Estado);
        Assert.Equal(250m, consulta.Valor!.TotalGeneral);
        Assert.Equal(250m, consulta.Valor.Items.Single().Total);
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
            null,
            null,
            null,
            null,
            SemillaInicial.UsuarioAdminId,
            CancellationToken.None);

        Assert.Equal(EstadoActa.Invalido, creada.Estado);
        Assert.Equal(CrearActaValidador.MensajeFechas, creada.Mensaje);
        Assert.Equal(EstadoActa.Invalido, consulta.Estado);
        Assert.Equal(CrearActaValidador.MensajeFechas, consulta.Mensaje);
        Assert.Equal(0, await _context.Actas.CountAsync());
    }

    [Fact]
    public async Task Crea_un_acta_sin_items()
    {
        var catalogos = await SembrarCatalogosAsync();
        var solicitud = Solicitud(catalogos);
        solicitud.Detalles = [];

        var creada = await _servicio.CrearAsync(solicitud, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var acta = await _servicio.ObtenerAsync(creada.Valor!.NumeroActa, SemillaInicial.UsuarioAdminId, CancellationToken.None);

        Assert.Equal(EstadoActa.Ok, creada.Estado);
        Assert.Empty(acta.Valor!.Detalles);
        Assert.Equal(0m, acta.Valor.Total);
    }

    [Fact]
    public async Task Actualizar_conserva_el_numero_y_agrega_items()
    {
        var catalogos = await SembrarCatalogosAsync();
        var solicitud = Solicitud(catalogos);
        var creada = await _servicio.CrearAsync(solicitud, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var original = await _servicio.ObtenerAsync(creada.Valor!.NumeroActa, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var linea = original.Valor!.Detalles.Single();

        solicitud.Observacion = "Se agregó un ítem";
        solicitud.Detalles =
        [
            new LineaActaSolicitud
            {
                Id = linea.Id,
                ConstruccionId = linea.ConstruccionId,
                ConceptoId = linea.ConceptoId,
                UnidadMedidaId = linea.UnidadMedidaId,
                Cantidad = 3,
                Precio = linea.Precio,
                Descripcion = linea.Descripcion
            },
            new LineaActaSolicitud
            {
                ConstruccionId = catalogos.ConstruccionId,
                ConceptoId = catalogos.ConceptoId,
                UnidadMedidaId = catalogos.UnidadMedidaId,
                Cantidad = 4,
                Precio = 25,
                Descripcion = "Piso"
            }
        ];

        var actualizada = await _servicio.ActualizarAsync(
            creada.Valor.NumeroActa,
            solicitud,
            SemillaInicial.UsuarioAdminId,
            CancellationToken.None);

        Assert.Equal(EstadoActa.Ok, actualizada.Estado);
        Assert.Equal(creada.Valor.NumeroActa, actualizada.Valor!.NumeroActa);
        Assert.Equal(original.Valor.FechaRegistro, actualizada.Valor.FechaRegistro);
        Assert.Equal(2, actualizada.Valor.Detalles.Count);
        Assert.Equal(400m, actualizada.Valor.Total);
        Assert.Contains(actualizada.Valor.Detalles, detalle => detalle.Id == linea.Id && detalle.Cantidad == 3);
        Assert.Contains(actualizada.Valor.Detalles, detalle => detalle.Descripcion == "Piso");
    }

    [Fact]
    public async Task Dos_actualizaciones_con_la_misma_version_la_segunda_responde_conflicto()
    {
        var catalogos = await SembrarCatalogosAsync();
        var creada = await _servicio.CrearAsync(Solicitud(catalogos), SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var numero = creada.Valor!.NumeroActa;
        var original = await _servicio.ObtenerAsync(numero, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var linea = original.Valor!.Detalles.Single();
        var solicitud = Solicitud(catalogos);
        solicitud.RowVersion = original.Valor.RowVersion;
        solicitud.Observacion = "Primera edición";
        solicitud.Detalles[0].Id = linea.Id;

        var primera = await _servicio.ActualizarAsync(numero, solicitud, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        solicitud.Observacion = "Segunda edición";
        var segunda = await _servicio.ActualizarAsync(numero, solicitud, SemillaInicial.UsuarioAdminId, CancellationToken.None);

        Assert.Equal(EstadoActa.Ok, primera.Estado);
        Assert.NotEqual(original.Valor.RowVersion, primera.Valor!.RowVersion);
        Assert.Equal(EstadoActa.Conflicto, segunda.Estado);
    }

    [Fact]
    public async Task Actualizar_rechaza_un_item_de_otra_acta()
    {
        var catalogos = await SembrarCatalogosAsync();
        var primera = await _servicio.CrearAsync(Solicitud(catalogos), null, CancellationToken.None);
        var segunda = await _servicio.CrearAsync(Solicitud(catalogos), null, CancellationToken.None);
        var ajena = (await _servicio.ObtenerAsync(primera.Valor!.NumeroActa, SemillaInicial.UsuarioAdminId, CancellationToken.None)).Valor!.Detalles.Single();
        var solicitud = Solicitud(catalogos);
        solicitud.Detalles[0].Id = ajena.Id;

        var actualizada = await _servicio.ActualizarAsync(
            segunda.Valor!.NumeroActa,
            solicitud,
            SemillaInicial.UsuarioAdminId,
            CancellationToken.None);
        var intacta = await _servicio.ObtenerAsync(segunda.Valor.NumeroActa, SemillaInicial.UsuarioAdminId, CancellationToken.None);

        Assert.Equal(EstadoActa.Invalido, actualizada.Estado);
        var unica = Assert.Single(intacta.Valor!.Detalles);
        Assert.NotEqual(ajena.Id, unica.Id);
    }

    [Fact]
    public async Task El_contratista_agrega_su_trabajo_sin_cambiar_el_del_arquitecto()
    {
        var catalogos = await SembrarCatalogosAsync();
        var creada = await _servicio.CrearAsync(Solicitud(catalogos), SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var numero = creada.Valor!.NumeroActa;
        var usuario = new Usuario
        {
            NombreUsuario = "contratista",
            Nombre = "Usuario contratista",
            PasswordHash = "x",
            TipoUsuarioId = SemillaInicial.TipoContratistaId,
            ContratistaId = catalogos.ContratistaId
        };
        var otro = new Contratista { Nombre = "Otro contratista" };
        _context.AddRange(usuario, otro);
        await _context.SaveChangesAsync();
        var intruso = new Usuario
        {
            NombreUsuario = "intruso",
            Nombre = "Intruso",
            PasswordHash = "x",
            TipoUsuarioId = SemillaInicial.TipoContratistaId,
            ContratistaId = otro.Id
        };
        _context.Usuarios.Add(intruso);
        await _context.SaveChangesAsync();

        var original = await _servicio.ObtenerAsync(numero, usuario.Id, CancellationToken.None);
        var lineaArquitecto = original.Valor!.Detalles.Single();
        var solicitud = Solicitud(catalogos);
        solicitud.Observacion = "No debe guardarse";
        solicitud.FechaInicial = new DateOnly(2026, 1, 1);
        solicitud.Detalles =
        [
            new LineaActaSolicitud
            {
                Id = lineaArquitecto.Id,
                ConstruccionId = lineaArquitecto.ConstruccionId,
                ConceptoId = lineaArquitecto.ConceptoId,
                UnidadMedidaId = lineaArquitecto.UnidadMedidaId,
                Cantidad = 99,
                Precio = 1,
                Descripcion = "Cambio ajeno"
            },
            new LineaActaSolicitud
            {
                ConstruccionId = catalogos.ConstruccionId,
                ConceptoId = catalogos.ConceptoId,
                UnidadMedidaId = catalogos.UnidadMedidaId,
                Cantidad = 2,
                Precio = 10,
                Descripcion = "Trabajo del contratista"
            }
        ];

        var actualizada = await _servicio.ActualizarAsync(numero, solicitud, usuario.Id, CancellationToken.None);
        var listado = await _servicio.ConsultarAsync(null, null, null, null, null, null, null, usuario.Id, CancellationToken.None);
        var ajeno = await _servicio.ObtenerAsync(numero, intruso.Id, CancellationToken.None);

        Assert.Equal(EstadoActa.Ok, actualizada.Estado);
        Assert.Equal(original.Valor.Observacion, actualizada.Valor!.Observacion);
        Assert.Equal(original.Valor.FechaInicial, actualizada.Valor.FechaInicial);
        Assert.Contains(actualizada.Valor.Detalles, detalle =>
            detalle.Id == lineaArquitecto.Id &&
            detalle.Cantidad == lineaArquitecto.Cantidad &&
            detalle.TipoUsuarioId == SemillaInicial.TipoArquitectoId);
        Assert.Contains(actualizada.Valor.Detalles, detalle =>
            detalle.Descripcion == "Trabajo del contratista" &&
            detalle.TipoUsuarioId == SemillaInicial.TipoContratistaId);
        Assert.Equal(numero, listado.Valor!.Items.Single().NumeroActa);
        Assert.Equal(EstadoActa.NoEncontrado, ajeno.Estado);
    }

    [Fact]
    public async Task El_total_suma_solo_los_items_aprobados()
    {
        var catalogos = await SembrarCatalogosAsync();
        var solicitud = Solicitud(catalogos);
        solicitud.Detalles.Add(new LineaActaSolicitud
        {
            ConstruccionId = catalogos.ConstruccionId,
            ConceptoId = catalogos.ConceptoId,
            UnidadMedidaId = catalogos.UnidadMedidaId,
            Cantidad = 3,
            Precio = 50,
            Descripcion = "Segunda"
        });
        var creada = await _servicio.CrearAsync(solicitud, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var numero = creada.Valor!.NumeroActa;
        var revisor = new Usuario
        {
            NombreUsuario = "revisor",
            Nombre = "Usuario revisor",
            PasswordHash = "x",
            TipoUsuarioId = SemillaInicial.TipoRevisorId,
            RevisorId = catalogos.RevisorId
        };
        _context.Usuarios.Add(revisor);
        await _context.SaveChangesAsync();

        var original = await _servicio.ObtenerAsync(numero, revisor.Id, CancellationToken.None);
        var primera = original.Valor!.Detalles.Single(detalle => detalle.Descripcion == "Muro");
        var segunda = original.Valor.Detalles.Single(detalle => detalle.Descripcion == "Segunda");
        solicitud.Detalles[0].Id = primera.Id;
        solicitud.Detalles[0].Aprobado = false;
        solicitud.Detalles[1].Id = segunda.Id;
        solicitud.Detalles.Add(new LineaActaSolicitud
        {
            ConstruccionId = catalogos.ConstruccionId,
            ConceptoId = catalogos.ConceptoId,
            UnidadMedidaId = catalogos.UnidadMedidaId,
            Cantidad = 1,
            Precio = 10,
            Descripcion = "No debe agregarse"
        });

        var agregado = await _servicio.ActualizarAsync(numero, solicitud, revisor.Id, CancellationToken.None);
        solicitud.Detalles.RemoveAt(2);
        var revisada = await _servicio.ActualizarAsync(numero, solicitud, revisor.Id, CancellationToken.None);
        solicitud.Detalles[1].Aprobado = false;
        var todoRechazado = await _servicio.ActualizarAsync(numero, solicitud, revisor.Id, CancellationToken.None);
        var listado = await _servicio.ConsultarAsync(null, null, null, null, null, null, null, SemillaInicial.UsuarioAdminId, CancellationToken.None);

        Assert.Equal(CrearActaValidador.MensajeRevisorAgrega, agregado.Mensaje);
        Assert.Equal(150m, revisada.Valor!.Total);
        Assert.Equal(0m, revisada.Valor.Detalles.Single(detalle => detalle.Id == primera.Id).Subtotal);
        Assert.Equal(150m, revisada.Valor.Detalles.Single(detalle => detalle.Id == segunda.Id).Subtotal);
        Assert.Equal(primera.Cantidad, revisada.Valor.Detalles.Single(detalle => detalle.Id == primera.Id).Cantidad);
        Assert.Equal(0m, todoRechazado.Valor!.Total);
        Assert.Equal(0m, listado.Valor!.Items.Single().Total);
        Assert.Equal(0m, listado.Valor.TotalGeneral);
    }

    [Fact]
    public async Task Un_acta_cerrada_no_admite_cambios_hasta_que_el_administrador_la_abre()
    {
        var catalogos = await SembrarCatalogosAsync();
        var creada = await _servicio.CrearAsync(Solicitud(catalogos), SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var numero = creada.Valor!.NumeroActa;
        var arquitecto = new Usuario
        {
            NombreUsuario = "arquitecto",
            Nombre = "Usuario arquitecto",
            PasswordHash = "x",
            TipoUsuarioId = SemillaInicial.TipoArquitectoId,
            ArquitectoId = catalogos.ArquitectoId
        };
        _context.Usuarios.Add(arquitecto);
        await _context.SaveChangesAsync();

        var cerrada = await _servicio.CambiarCierreAsync(numero, true, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var intentoAjeno = await _servicio.CambiarCierreAsync(numero, false, arquitecto.Id, CancellationToken.None);
        var solicitud = Solicitud(catalogos);
        solicitud.Detalles[0].Id = cerrada.Valor!.Detalles.Single().Id;
        solicitud.Detalles.Add(new LineaActaSolicitud
        {
            ConstruccionId = catalogos.ConstruccionId,
            ConceptoId = catalogos.ConceptoId,
            Cantidad = 1,
            Precio = 10,
            Descripcion = "No debe agregarse"
        });
        var bloqueada = await _servicio.ActualizarAsync(numero, solicitud, arquitecto.Id, CancellationToken.None);
        var abierta = await _servicio.CambiarCierreAsync(numero, false, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var permitida = await _servicio.ActualizarAsync(numero, solicitud, arquitecto.Id, CancellationToken.None);

        Assert.True(cerrada.Valor.Cerrada);
        Assert.Equal("Solo el administrador puede cerrar o abrir el acta.", intentoAjeno.Mensaje);
        Assert.Equal(CrearActaValidador.MensajeCerrada, bloqueada.Mensaje);
        Assert.False(abierta.Valor!.Cerrada);
        Assert.Equal(2, permitida.Valor!.Detalles.Count);
    }

    [Fact]
    public async Task La_consulta_filtra_por_construccion_contratista_y_estado()
    {
        var catalogos = await SembrarCatalogosAsync();
        var otraObra = new Construccion { Descripcion = "Otra obra", Propietario = "Otro" };
        var otroContratista = new Contratista { Nombre = "Otro contratista" };
        var otroRevisor = new Revisor { Nombre = "Otro revisor" };
        _context.AddRange(otraObra, otroContratista, otroRevisor);
        await _context.SaveChangesAsync();

        var incluida = Solicitud(catalogos);
        var creada = await _servicio.CrearAsync(incluida, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        await _servicio.CambiarCierreAsync(creada.Valor!.NumeroActa, true, SemillaInicial.UsuarioAdminId, CancellationToken.None);

        var otra = Solicitud(catalogos);
        otra.ContratistaId = otroContratista.Id;
        otra.RevisorId = otroRevisor.Id;
        otra.Detalles[0].ConstruccionId = otraObra.Id;
        otra.Detalles[0].Descripcion = "Otra";
        var creadaOtra = await _servicio.CrearAsync(otra, SemillaInicial.UsuarioAdminId, CancellationToken.None);

        var porObra = await Consultar(incluida, null, null, null, catalogos.ConstruccionId, null);
        var porContratista = await Consultar(incluida, null, null, catalogos.ContratistaId, null, null);
        var porRevisor = await Consultar(incluida, null, catalogos.RevisorId, null, null, null);
        var abiertas = await Consultar(incluida, null, null, null, null, false);
        var cerradas = await Consultar(incluida, null, null, null, null, true);

        Assert.Equal(creada.Valor.NumeroActa, porObra.Items.Single().NumeroActa);
        Assert.Equal(creada.Valor.NumeroActa, porContratista.Items.Single().NumeroActa);
        Assert.Equal(creada.Valor.NumeroActa, porRevisor.Items.Single().NumeroActa);
        Assert.Equal(creadaOtra.Valor!.NumeroActa, abiertas.Items.Single().NumeroActa);
        Assert.Equal(creada.Valor.NumeroActa, cerradas.Items.Single().NumeroActa);
    }

    [Fact]
    public async Task El_informe_detallado_incluye_el_acta_y_el_agrupado_suma_el_item()
    {
        var catalogos = await SembrarCatalogosAsync();
        var primera = Solicitud(catalogos);
        var creada = await _servicio.CrearAsync(primera, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var segunda = Solicitud(catalogos);
        segunda.Detalles[0].Cantidad = 3;
        segunda.Detalles[0].Descripcion = "Muro dos";
        var creadaOtra = await _servicio.CrearAsync(segunda, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var otraObra = new Construccion { Descripcion = "Obra ajena", Propietario = "Otro" };
        _context.Add(otraObra);
        await _context.SaveChangesAsync();
        var acta = await _context.Actas.Include(item => item.Detalles)
            .SingleAsync(item => item.NumeroActa == creada.Valor!.NumeroActa);
        acta.Detalles.Add(new ActaDetalle
        {
            ConstruccionId = catalogos.ConstruccionId,
            ConceptoId = catalogos.ConceptoId,
            UnidadMedidaId = catalogos.UnidadMedidaId,
            Cantidad = 10,
            Precio = 100,
            Descripcion = "Rechazado",
            Aprobado = false,
            TipoUsuarioId = SemillaInicial.TipoArquitectoId
        });
        acta.Detalles.Add(new ActaDetalle
        {
            ConstruccionId = otraObra.Id,
            ConceptoId = catalogos.ConceptoId,
            UnidadMedidaId = catalogos.UnidadMedidaId,
            Cantidad = 7,
            Precio = 100,
            Descripcion = "Otra obra",
            TipoUsuarioId = SemillaInicial.TipoArquitectoId
        });
        await _context.SaveChangesAsync();

        var detallado = await ConsultarItems(primera, catalogos.ConstruccionId, false);
        var agrupado = await ConsultarItems(primera, catalogos.ConstruccionId, true);

        Assert.Equal(3, detallado.Items.Count);
        Assert.Equal(new[] { 300m, 200m, 0m }, detallado.Items.Select(item => item.Subtotal));
        Assert.Equal(creadaOtra.Valor!.NumeroActa, detallado.Items[0].NumeroActa);
        Assert.Equal(creada.Valor!.NumeroActa, detallado.Items[1].NumeroActa);
        Assert.Equal("Rechazado", detallado.Items[2].Descripcion);
        Assert.Contains(detallado.Items, item => item.NumeroActa == creada.Valor!.NumeroActa && item.Cantidad == 2 && item.Aprobado);
        Assert.Contains(detallado.Items, item => item.NumeroActa == creadaOtra.Valor!.NumeroActa && item.Descripcion == "Muro dos");
        Assert.Contains(detallado.Items, item => item.Descripcion == "Rechazado" && !item.Aprobado && item.Subtotal == 0);
        Assert.Equal(500m, detallado.Total);
        var encabezado = detallado.Encabezado ?? [];
        Assert.Contains("Construcción: Obra prueba", encabezado);
        Assert.Contains("Del 01/11/2026 al 09/11/2026", encabezado);
        Assert.True(detallado.OcultarConstruccion);
        var porCantidad = await ConsultarItems(primera, catalogos.ConstruccionId, false, "cantidad", true);
        Assert.Equal(new[] { 10m, 3m, 2m }, porCantidad.Items.Select(item => item.Cantidad));
        Assert.Equal(2, agrupado.Items.Count);
        Assert.All(agrupado.Items, item => Assert.Null(item.NumeroActa));
        var aprobado = Assert.Single(agrupado.Items, item => item.Aprobado);
        var rechazado = Assert.Single(agrupado.Items, item => !item.Aprobado);
        Assert.Equal(5m, aprobado.Cantidad);
        Assert.Equal(500m, aprobado.Subtotal);
        Assert.Equal(10m, rechazado.Cantidad);
        Assert.Equal(0m, rechazado.Subtotal);
        Assert.Equal(500m, agrupado.Total);

        var porActa = await ConsultarItems(primera, catalogos.ConstruccionId, true, romperPorActa: true);
        Assert.Equal(3, porActa.Items.Count);
        Assert.All(porActa.Items, item => Assert.NotNull(item.NumeroActa));
        Assert.Equal(creadaOtra.Valor!.NumeroActa, porActa.Items[0].NumeroActa);
        Assert.Equal(300m, porActa.Items.Where(item => item.NumeroActa == creadaOtra.Valor!.NumeroActa).Sum(item => item.Subtotal));
        Assert.Equal(200m, porActa.Items.Where(item => item.NumeroActa == creada.Valor!.NumeroActa).Sum(item => item.Subtotal));
        Assert.Equal(500m, porActa.Total);

        var porMaterial = await _servicio.ConsultarMaterialesAsync(
            primera.FechaInicial,
            primera.FechaFinal,
            null,
            null,
            null,
            catalogos.ConstruccionId,
            null,
            null,
            SemillaInicial.UsuarioAdminId,
            CancellationToken.None);
        Assert.Equal(EstadoActa.Ok, porMaterial.Estado);
        Assert.Empty(porMaterial.Valor!.Items);
        Assert.Equal(0m, porMaterial.Valor.Total);

        QuestPDF.Settings.License = LicenseType.Community;
        var pdfDetallado = await ImprimirItems(primera, catalogos.ConstruccionId, false);
        var pdfAgrupado = await ImprimirItems(primera, catalogos.ConstruccionId, true);
        var pdfPorActa = await ImprimirItems(primera, catalogos.ConstruccionId, false, true);
        Assert.True(pdfDetallado.Length > 500);
        Assert.True(pdfAgrupado.Length > 500);
        Assert.True(pdfPorActa.Length > 500);
    }

    [Fact]
    public async Task El_informe_por_materiales_suma_cantidades_y_promedia_el_precio()
    {
        var catalogos = await SembrarCatalogosAsync();
        var cemento = new Insumo
        {
            Codigo = "MAT-001",
            Descripcion = "Cemento gris 50 kg",
            Tipo = TipoInsumo.Material,
            UnidadCompra = "und",
            UnidadUso = "und",
            CantidadPorUnidad = 1,
            FechaPrecio = DateTime.UtcNow
        };
        _context.Add(cemento);
        await _context.SaveChangesAsync();
        var solicitud = Solicitud(catalogos);
        solicitud.Detalles =
        [
            new LineaActaSolicitud
            {
                ConstruccionId = catalogos.ConstruccionId,
                ConceptoId = catalogos.ConceptoId,
                UnidadMedidaId = catalogos.UnidadMedidaId,
                InsumoId = cemento.Id,
                Cantidad = 2,
                Precio = 100,
                Descripcion = "Primera"
            },
            new LineaActaSolicitud
            {
                ConstruccionId = catalogos.ConstruccionId,
                ConceptoId = catalogos.ConceptoId,
                UnidadMedidaId = catalogos.UnidadMedidaId,
                InsumoId = cemento.Id,
                Cantidad = 3,
                Precio = 200,
                Descripcion = "Segunda"
            },
            new LineaActaSolicitud
            {
                ConstruccionId = catalogos.ConstruccionId,
                ConceptoId = catalogos.ConceptoId,
                UnidadMedidaId = catalogos.UnidadMedidaId,
                InsumoId = cemento.Id,
                Cantidad = 10,
                Precio = 100,
                Descripcion = "Rechazado",
                Aprobado = false
            }
        ];

        var creada = await _servicio.CrearAsync(solicitud, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        Assert.Equal(EstadoActa.Ok, creada.Estado);
        var rechazada = await _context.ActaDetalles.SingleAsync(detalle => detalle.Descripcion == "Rechazado");
        rechazada.Aprobado = false;
        await _context.SaveChangesAsync();

        var informe = await _servicio.ConsultarMaterialesAsync(
            solicitud.FechaInicial,
            solicitud.FechaFinal,
            null,
            null,
            null,
            catalogos.ConstruccionId,
            null,
            null,
            SemillaInicial.UsuarioAdminId,
            CancellationToken.None);

        Assert.Equal(EstadoActa.Ok, informe.Estado);
        Assert.Equal(2, informe.Valor!.Items.Count);
        var aprobado = Assert.Single(informe.Valor.Items, item => item.Aprobado);
        Assert.Equal("MAT-001", aprobado.Codigo);
        Assert.Equal("Cemento gris 50 kg", aprobado.Material);
        Assert.Equal(5m, aprobado.Cantidad);
        Assert.Equal(160m, aprobado.Precio);
        Assert.Equal(800m, aprobado.Subtotal);
        Assert.Equal(800m, informe.Valor.Total);
        Assert.True(informe.Valor.Agrupado);
        Assert.All(informe.Valor.Items, item => Assert.Null(item.NumeroActa));

        var detallado = await _servicio.ConsultarMaterialesAsync(
            solicitud.FechaInicial,
            solicitud.FechaFinal,
            null,
            null,
            null,
            catalogos.ConstruccionId,
            null,
            null,
            SemillaInicial.UsuarioAdminId,
            CancellationToken.None,
            agrupado: false);
        Assert.Equal(EstadoActa.Ok, detallado.Estado);
        Assert.False(detallado.Valor!.Agrupado);
        Assert.Equal(3, detallado.Valor.Items.Count);
        Assert.All(detallado.Valor.Items, item => Assert.Equal(creada.Valor!.NumeroActa, item.NumeroActa));
        Assert.Contains(detallado.Valor.Items, item => item.Aprobado && item.Cantidad == 2 && item.Precio == 100 && item.Subtotal == 200);
        Assert.Contains(detallado.Valor.Items, item => item.Aprobado && item.Cantidad == 3 && item.Precio == 200 && item.Subtotal == 600);
        Assert.Contains(detallado.Valor.Items, item => !item.Aprobado && item.Cantidad == 10 && item.Subtotal == 0);
        Assert.Equal(800m, detallado.Valor.Total);

        var excel = await _servicio.ExportarMaterialesAsync(
            solicitud.FechaInicial,
            solicitud.FechaFinal,
            null,
            null,
            null,
            null,
            null,
            null,
            SemillaInicial.UsuarioAdminId,
            CancellationToken.None);
        Assert.Equal(EstadoActa.Ok, excel.Estado);
        Assert.True(excel.Valor!.Length > 100);
        Assert.Contains("Construcción: Obra prueba", informe.Valor.Encabezado ?? []);

        var ferreteria = new Proveedor { Nombre = "Ferretería Central" };
        var otro = new Proveedor { Nombre = "Otro proveedor" };
        _context.AddRange(ferreteria, otro);
        await _context.SaveChangesAsync();
        _context.ProveedorMateriales.Add(new ProveedorMaterial { ProveedorId = ferreteria.Id, InsumoId = cemento.Id, Precio = 1 });
        await _context.SaveChangesAsync();

        var delProveedor = await _servicio.ConsultarMaterialesAsync(
            solicitud.FechaInicial,
            solicitud.FechaFinal,
            null,
            null,
            null,
            catalogos.ConstruccionId,
            null,
            ferreteria.Id,
            SemillaInicial.UsuarioAdminId,
            CancellationToken.None);
        Assert.Equal(EstadoActa.Ok, delProveedor.Estado);
        Assert.Equal(800m, delProveedor.Valor!.Total);
        Assert.Contains("Proveedor: Ferretería Central", delProveedor.Valor.Encabezado ?? []);

        var ajeno = await _servicio.ConsultarMaterialesAsync(
            solicitud.FechaInicial,
            solicitud.FechaFinal,
            null,
            null,
            null,
            null,
            null,
            otro.Id,
            SemillaInicial.UsuarioAdminId,
            CancellationToken.None);
        Assert.Equal(EstadoActa.Ok, ajeno.Estado);
        Assert.Empty(ajeno.Valor!.Items);

        QuestPDF.Settings.License = LicenseType.Community;
        var pdf = await _servicio.ImprimirMaterialesAsync(
            solicitud.FechaInicial,
            solicitud.FechaFinal,
            null,
            null,
            null,
            catalogos.ConstruccionId,
            null,
            ferreteria.Id,
            SemillaInicial.UsuarioAdminId,
            "material",
            false,
            CancellationToken.None);
        Assert.Equal(EstadoActa.Ok, pdf.Estado);
        Assert.True(pdf.Valor!.Length > 500);
        var pdfDetallado = await _servicio.ImprimirMaterialesAsync(
            solicitud.FechaInicial,
            solicitud.FechaFinal,
            null,
            null,
            null,
            catalogos.ConstruccionId,
            null,
            ferreteria.Id,
            SemillaInicial.UsuarioAdminId,
            "acta",
            false,
            CancellationToken.None,
            agrupado: false);
        Assert.Equal(EstadoActa.Ok, pdfDetallado.Estado);
        Assert.True(pdfDetallado.Valor!.Length > 500);
    }

    private async Task<byte[]> ImprimirItems(CrearActaSolicitud solicitud, int construccionId, bool agrupado, bool romperPorActa = false)
    {
        var resultado = await _servicio.ImprimirItemsAsync(
            solicitud.FechaInicial,
            solicitud.FechaFinal,
            null,
            null,
            null,
            construccionId,
            null,
            agrupado,
            SemillaInicial.UsuarioAdminId,
            null,
            false,
            romperPorActa,
            CancellationToken.None);
        Assert.Equal(EstadoActa.Ok, resultado.Estado);
        return resultado.Valor!;
    }

    private async Task<InformeItemsDto> ConsultarItems(
        CrearActaSolicitud solicitud,
        int construccionId,
        bool agrupado,
        string? orden = null,
        bool descendente = false,
        bool romperPorActa = false)
    {
        var resultado = await _servicio.ConsultarItemsAsync(
            solicitud.FechaInicial,
            solicitud.FechaFinal,
            null,
            null,
            null,
            construccionId,
            null,
            agrupado,
            SemillaInicial.UsuarioAdminId,
            orden,
            descendente,
            romperPorActa,
            CancellationToken.None);
        Assert.Equal(EstadoActa.Ok, resultado.Estado);
        return resultado.Valor!;
    }

    private async Task<ConsultaActasDto> Consultar(
        CrearActaSolicitud solicitud,
        int? arquitectoId,
        int? revisorId,
        int? contratistaId,
        int? construccionId,
        bool? cerrada)
    {
        var resultado = await _servicio.ConsultarAsync(
            solicitud.FechaInicial,
            solicitud.FechaFinal,
            arquitectoId,
            revisorId,
            contratistaId,
            construccionId,
            cerrada,
            SemillaInicial.UsuarioAdminId,
            CancellationToken.None);
        Assert.Equal(EstadoActa.Ok, resultado.Estado);
        return resultado.Valor!;
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
