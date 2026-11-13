using Arquitectos.Application.Actas;
using Arquitectos.Application.Usuarios;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Actas;
using Arquitectos.Infrastructure.Auditoria;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Tests;

public sealed class OperacionesLineaActaTests : IDisposable
{
    private readonly SqliteConnection _conexion = new("Data Source=:memory:");
    private readonly ArquitectosDbContext _context;
    private readonly ActaServicio _servicio;
    private readonly OperacionesLineaActa _operaciones;
    private readonly HttpContextAccessor _acceso = new();

    public OperacionesLineaActaTests()
    {
        _conexion.Open();
        var http = new DefaultHttpContext();
        http.Request.Path = "/api/movil/v1/sincronizar";
        _acceso.HttpContext = http;
        var opciones = new DbContextOptionsBuilder<ArquitectosDbContext>()
            .UseSqlite(_conexion)
            .AddInterceptors(new AuditoriaInterceptor(_acceso))
            .Options;
        _context = new ArquitectosDbContext(opciones);
        _context.Database.EnsureCreated();
        var auditoria = new AuditoriaServicio(_context, _acceso);
        _operaciones = new OperacionesLineaActa(_context, auditoria);
        _servicio = new ActaServicio(_context, auditoria, _operaciones);
    }

    [Theory]
    [InlineData(PerfilesUsuario.Arquitecto)]
    [InlineData(PerfilesUsuario.Contratista)]
    public async Task El_perfil_agrega_edita_y_elimina_su_linea_y_el_revisor_no(int perfil)
    {
        var catalogos = await SembrarAsync();
        var numero = await CrearActaAsync(catalogos);
        var actor = await UsuarioAsync(perfil, catalogos);
        var revisor = await UsuarioAsync(PerfilesUsuario.Revisor, catalogos);
        var linea = await LineaPropiaAsync(numero, actor, catalogos);

        var agregada = await _operaciones.AgregarAsync(numero, Guid.NewGuid(), Linea(catalogos, "Nueva", 2, 15), actor, CancellationToken.None);
        var editar = await _operaciones.EditarAsync(numero, linea.Id, linea.Version, Linea(catalogos, "Corregida", 3, 20), actor, CancellationToken.None);
        var eliminar = await _operaciones.EliminarAsync(numero, agregada.Valor!.Id, agregada.Valor.RowVersion, actor, CancellationToken.None);
        var revisorAgrega = await _operaciones.AgregarAsync(numero, Guid.NewGuid(), Linea(catalogos, "No", 1, 1), revisor, CancellationToken.None);
        var revisorEdita = await _operaciones.EditarAsync(numero, linea.Id, editar.Valor!.RowVersion, Linea(catalogos, "No", 1, 1), revisor, CancellationToken.None);
        var revisorElimina = await _operaciones.EliminarAsync(numero, linea.Id, editar.Valor.RowVersion, revisor, CancellationToken.None);
        var acta = await _servicio.ObtenerAsync(numero, actor.UsuarioId, CancellationToken.None);

        Assert.Equal(EstadoActa.Ok, agregada.Estado);
        Assert.True(agregada.Valor!.Aprobado);
        Assert.Equal(EstadoActa.Ok, editar.Estado);
        Assert.True(editar.Valor!.Aprobado);
        Assert.Equal(EstadoActa.Ok, eliminar.Estado);
        Assert.Equal(CrearActaValidador.MensajeRevisorAgrega, revisorAgrega.Mensaje);
        Assert.Equal(CrearActaValidador.MensajeRevisorNoEdita, revisorEdita.Mensaje);
        Assert.Equal(CrearActaValidador.MensajeRevisorNoEdita, revisorElimina.Mensaje);
        var propia = Assert.Single(acta.Valor!.Detalles, detalle => detalle.TipoUsuarioId == perfil);
        Assert.Equal("Corregida", propia.Descripcion);
        Assert.Equal(3m, propia.Cantidad);
        Assert.DoesNotContain(acta.Valor.Detalles, detalle => detalle.Descripcion == "Nueva");
    }

    [Fact]
    public async Task Solo_el_revisor_aprueba_o_rechaza()
    {
        var catalogos = await SembrarAsync();
        var numero = await CrearActaAsync(catalogos);
        var revisor = await UsuarioAsync(PerfilesUsuario.Revisor, catalogos);
        var arquitecto = await UsuarioAsync(PerfilesUsuario.Arquitecto, catalogos);
        var linea = (await _servicio.ObtenerAsync(numero, revisor.UsuarioId, CancellationToken.None)).Valor!.Detalles.Single();
        var version = await VersionLineaAsync(linea.Id);

        var rechazada = await _operaciones.RevisarAsync(numero, linea.Id, version, false, revisor, CancellationToken.None);
        var arquitectoRevisa = await _operaciones.RevisarAsync(numero, linea.Id, rechazada.Valor!.RowVersion, true, arquitecto, CancellationToken.None);
        var acta = await _servicio.ObtenerAsync(numero, revisor.UsuarioId, CancellationToken.None);

        Assert.Equal(EstadoActa.Ok, rechazada.Estado);
        Assert.False(rechazada.Valor.Aprobado);
        Assert.Equal(CrearActaValidador.MensajeSoloRevisor, arquitectoRevisa.Mensaje);
        Assert.False(acta.Valor!.Detalles.Single().Aprobado);
        Assert.Equal(0m, acta.Valor.Total);
    }

    [Fact]
    public async Task Un_acta_cerrada_rechaza_las_cuatro_operaciones()
    {
        var catalogos = await SembrarAsync();
        var numero = await CrearActaAsync(catalogos);
        var actor = await UsuarioAsync(PerfilesUsuario.Arquitecto, catalogos);
        var revisor = await UsuarioAsync(PerfilesUsuario.Revisor, catalogos);
        var linea = (await _servicio.ObtenerAsync(numero, actor.UsuarioId, CancellationToken.None)).Valor!.Detalles.Single();
        var version = await VersionLineaAsync(linea.Id);
        await _servicio.CambiarCierreAsync(numero, true, SemillaInicial.UsuarioAdminId, CancellationToken.None);

        var agregar = await _operaciones.AgregarAsync(numero, Guid.NewGuid(), Linea(catalogos, "No", 1, 1), actor, CancellationToken.None);
        var editar = await _operaciones.EditarAsync(numero, linea.Id, version, Linea(catalogos, "No", 1, 1), actor, CancellationToken.None);
        var eliminar = await _operaciones.EliminarAsync(numero, linea.Id, version, actor, CancellationToken.None);
        var revisar = await _operaciones.RevisarAsync(numero, linea.Id, version, false, revisor, CancellationToken.None);

        Assert.Equal(CrearActaValidador.MensajeCerrada, agregar.Mensaje);
        Assert.Equal(CrearActaValidador.MensajeCerrada, editar.Mensaje);
        Assert.Equal(CrearActaValidador.MensajeCerrada, eliminar.Mensaje);
        Assert.Equal(CrearActaValidador.MensajeCerrada, revisar.Mensaje);
        Assert.Single((await _servicio.ObtenerAsync(numero, SemillaInicial.UsuarioAdminId, CancellationToken.None)).Valor!.Detalles);
    }

    [Fact]
    public async Task No_se_edita_ni_elimina_una_linea_de_otro_perfil()
    {
        var catalogos = await SembrarAsync();
        var numero = await CrearActaAsync(catalogos);
        var contratista = await UsuarioAsync(PerfilesUsuario.Contratista, catalogos);
        var linea = (await _servicio.ObtenerAsync(numero, contratista.UsuarioId, CancellationToken.None)).Valor!.Detalles.Single();
        var version = await VersionLineaAsync(linea.Id);

        var editar = await _operaciones.EditarAsync(numero, linea.Id, version, Linea(catalogos, "Ajena", 9, 1), contratista, CancellationToken.None);
        var eliminar = await _operaciones.EliminarAsync(numero, linea.Id, version, contratista, CancellationToken.None);
        var acta = await _servicio.ObtenerAsync(numero, SemillaInicial.UsuarioAdminId, CancellationToken.None);

        Assert.Equal(CrearActaValidador.MensajeLineaAjena, editar.Mensaje);
        Assert.Equal(CrearActaValidador.MensajeLineaAjena, eliminar.Mensaje);
        Assert.Equal("Muro", acta.Valor!.Detalles.Single().Descripcion);
        Assert.Equal(2m, acta.Valor.Detalles.Single().Cantidad);
    }

    [Fact]
    public async Task Rechaza_cantidad_precio_descripcion_y_presupuesto()
    {
        var catalogos = await SembrarAsync();
        var numero = await CrearActaAsync(catalogos);
        var actor = await UsuarioAsync(PerfilesUsuario.Arquitecto, catalogos);
        var otraObra = new Construccion { Descripcion = "Otra obra" };
        _context.Add(otraObra);
        await _context.SaveChangesAsync();
        var borrador = PresupuestoDe(catalogos.ConstruccionId, EstadoPresupuesto.Borrador, "Borrador");
        var aprobadoAjeno = PresupuestoDe(otraObra.Id, EstadoPresupuesto.Aprobado, "Ajeno");
        var aprobado = PresupuestoDe(catalogos.ConstruccionId, EstadoPresupuesto.Aprobado, "Valido");
        _context.AddRange(borrador, aprobadoAjeno, aprobado);
        await _context.SaveChangesAsync();
        var itemBorrador = borrador.Capitulos.Single().Items.Single().Id;
        var itemAjeno = aprobadoAjeno.Capitulos.Single().Items.Single().Id;

        var cantidad = await _operaciones.AgregarAsync(numero, Guid.NewGuid(), Linea(catalogos, "Mala", 0, 10), actor, CancellationToken.None);
        var precio = await _operaciones.AgregarAsync(numero, Guid.NewGuid(), Linea(catalogos, "Mala", 1, -5), actor, CancellationToken.None);
        var larga = Linea(catalogos, new string('a', 501), 1, 10);
        var descripcion = await _operaciones.AgregarAsync(numero, Guid.NewGuid(), larga, actor, CancellationToken.None);
        var deBorrador = Linea(catalogos, "Presupuesto", 1, 10);
        deBorrador.PresupuestoItemId = itemBorrador;
        var noAprobado = await _operaciones.AgregarAsync(numero, Guid.NewGuid(), deBorrador, actor, CancellationToken.None);
        var deOtraObra = Linea(catalogos, "Presupuesto", 1, 10);
        deOtraObra.PresupuestoItemId = itemAjeno;
        var otra = await _operaciones.AgregarAsync(numero, Guid.NewGuid(), deOtraObra, actor, CancellationToken.None);

        Assert.Equal(CrearActaValidador.MensajeCantidad, cantidad.Mensaje);
        Assert.Equal(CrearActaValidador.MensajePrecio, precio.Mensaje);
        Assert.Equal(CrearActaValidador.MensajeDescripcionLarga, descripcion.Mensaje);
        Assert.Equal(CrearActaValidador.MensajePresupuesto, noAprobado.Mensaje);
        Assert.Equal(CrearActaValidador.MensajePresupuesto, otra.Mensaje);
    }

    [Fact]
    public async Task La_auditoria_queda_como_movil_y_la_version_del_acta_cambia()
    {
        var catalogos = await SembrarAsync();
        var numero = await CrearActaAsync(catalogos);
        var antes = (await _context.Actas.AsNoTracking().SingleAsync(acta => acta.NumeroActa == numero)).RowVersion.ToArray();
        var actor = await UsuarioAsync(PerfilesUsuario.Arquitecto, catalogos);
        var revisor = await UsuarioAsync(PerfilesUsuario.Revisor, catalogos);
        var linea = (await _servicio.ObtenerAsync(numero, actor.UsuarioId, CancellationToken.None)).Valor!.Detalles.Single();
        var version = await VersionLineaAsync(linea.Id);

        await _operaciones.AgregarAsync(numero, Guid.NewGuid(), Linea(catalogos, "Nueva", 1, 10), actor, CancellationToken.None);
        await _operaciones.RevisarAsync(numero, linea.Id, version, false, revisor, CancellationToken.None);
        var despues = await _context.Actas.AsNoTracking().SingleAsync(acta => acta.NumeroActa == numero);
        var origenes = await _context.Auditorias.Select(registro => registro.Origen).ToListAsync();
        var acciones = await _context.Auditorias.Select(registro => registro.Accion).ToListAsync();

        Assert.False(antes.SequenceEqual(despues.RowVersion));
        Assert.NotNull(despues.ModificadaEn);
        Assert.All(origenes, origen => Assert.Equal(OrigenAuditoria.Movil, origen));
        Assert.Contains(AccionAuditoria.Crear, acciones);
        Assert.Contains(AccionAuditoria.Rechazar, acciones);
    }

    public void Dispose()
    {
        _context.Dispose();
        _conexion.Dispose();
    }

    private async Task<Catalogo> SembrarAsync()
    {
        var arquitecto = new Arquitecto { Nombre = "Arquitecto prueba" };
        var revisor = new Revisor { Nombre = "Revisor prueba" };
        var contratista = new Contratista { Nombre = "Contratista prueba" };
        var construccion = new Construccion { Descripcion = "Obra prueba" };
        var concepto = new Concepto { Descripcion = "Concepto prueba" };
        var unidad = new UnidadMedida { Nombre = "m2", Descripcion = "Metro cuadrado" };
        _context.AddRange(arquitecto, revisor, contratista, construccion, concepto, unidad);
        await _context.SaveChangesAsync();
        return new Catalogo(arquitecto.Id, revisor.Id, contratista.Id, construccion.Id, concepto.Id, unidad.Id);
    }

    private async Task<int> CrearActaAsync(Catalogo catalogos)
    {
        var creada = await _servicio.CrearAsync(Solicitud(catalogos), SemillaInicial.UsuarioAdminId, CancellationToken.None);
        Assert.Equal(EstadoActa.Ok, creada.Estado);
        return creada.Valor!.NumeroActa;
    }

    private async Task<ActorActa> UsuarioAsync(int perfil, Catalogo catalogos)
    {
        var usuario = new Usuario
        {
            NombreUsuario = "u" + perfil + Guid.NewGuid().ToString("N")[..8],
            Nombre = "Usuario " + perfil,
            PasswordHash = "x",
            TipoUsuarioId = perfil,
            ArquitectoId = perfil == PerfilesUsuario.Arquitecto ? catalogos.ArquitectoId : null,
            ContratistaId = perfil == PerfilesUsuario.Contratista ? catalogos.ContratistaId : null,
            RevisorId = perfil == PerfilesUsuario.Revisor ? catalogos.RevisorId : null
        };
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
        return new ActorActa(usuario.Id, perfil, usuario.ArquitectoId, usuario.ContratistaId, usuario.RevisorId);
    }

    private async Task<(int Id, string Version)> LineaPropiaAsync(int numero, ActorActa actor, Catalogo catalogos)
    {
        if (actor.TipoUsuarioId == PerfilesUsuario.Arquitecto)
        {
            var existente = (await _servicio.ObtenerAsync(numero, actor.UsuarioId, CancellationToken.None)).Valor!.Detalles.Single();
            return (existente.Id, await VersionLineaAsync(existente.Id));
        }

        var agregada = await _operaciones.AgregarAsync(numero, Guid.NewGuid(), Linea(catalogos, "Propia", 1, 8), actor, CancellationToken.None);
        Assert.Equal(EstadoActa.Ok, agregada.Estado);
        return (agregada.Valor!.Id, agregada.Valor.RowVersion);
    }

    private async Task<string> VersionLineaAsync(int id) =>
        Convert.ToBase64String((await _context.ActaDetalles.AsNoTracking().SingleAsync(detalle => detalle.Id == id)).RowVersion);

    private static CrearActaSolicitud Solicitud(Catalogo catalogos) => new()
    {
        FechaInicial = new DateOnly(2026, 11, 1),
        FechaFinal = new DateOnly(2026, 11, 9),
        ArquitectoId = catalogos.ArquitectoId,
        RevisorId = catalogos.RevisorId,
        ContratistaId = catalogos.ContratistaId,
        Detalles = [Linea(catalogos, "Muro", 2, 100)]
    };

    private static LineaActaSolicitud Linea(Catalogo catalogos, string descripcion, decimal cantidad, decimal precio) => new()
    {
        ConstruccionId = catalogos.ConstruccionId,
        ConceptoId = catalogos.ConceptoId,
        UnidadMedidaId = catalogos.UnidadId,
        Cantidad = cantidad,
        Precio = precio,
        Descripcion = descripcion
    };

    private static Presupuesto PresupuestoDe(int construccionId, EstadoPresupuesto estado, string nombre) => new()
    {
        Nombre = nombre,
        ConstruccionId = construccionId,
        Estado = estado,
        Fecha = new DateOnly(2026, 11, 1),
        Capitulos =
        [
            new PresupuestoCapitulo
            {
                Nombre = "Capítulo",
                Numero = 1,
                Items = [new PresupuestoItem { Codigo = "1.01", Descripcion = nombre, Cantidad = 1 }]
            }
        ]
    };

    private sealed record Catalogo(int ArquitectoId, int RevisorId, int ContratistaId, int ConstruccionId, int ConceptoId, int UnidadId);
}

public sealed class ParidadWebMovilTests : IDisposable
{
    private readonly SqliteConnection _conexion = new("Data Source=:memory:");
    private readonly ArquitectosDbContext _context;
    private readonly ActaServicio _servicio;
    private readonly OperacionesLineaActa _operaciones;

    public ParidadWebMovilTests()
    {
        _conexion.Open();
        var opciones = new DbContextOptionsBuilder<ArquitectosDbContext>().UseSqlite(_conexion).Options;
        _context = new ArquitectosDbContext(opciones);
        _context.Database.EnsureCreated();
        _operaciones = new OperacionesLineaActa(_context, new AuditoriaServicio(_context, new HttpContextAccessor()));
        _servicio = new ActaServicio(_context, operaciones: _operaciones);
    }

    [Theory]
    [InlineData(PerfilesUsuario.Arquitecto)]
    [InlineData(PerfilesUsuario.Contratista)]
    public async Task Agregar_por_la_web_y_por_la_operacion_dejan_la_misma_acta(int perfil)
    {
        var catalogos = await SembrarAsync();
        var web = await PrepararAsync(catalogos);
        var movil = await PrepararAsync(catalogos);
        var actor = await UsuarioAsync(perfil, catalogos);
        var linea = Linea(catalogos, "Piso", 4, 25);

        var porWeb = await _servicio.ActualizarAsync(web, SolicitudCon(catalogos, web, linea, actor), actor.UsuarioId, CancellationToken.None);
        var porOperacion = await _operaciones.AgregarAsync(movil, Guid.NewGuid(), linea, actor, CancellationToken.None);
        var actaWeb = await _servicio.ObtenerAsync(web, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var actaMovil = await _servicio.ObtenerAsync(movil, SemillaInicial.UsuarioAdminId, CancellationToken.None);

        Assert.Equal(EstadoActa.Ok, porWeb.Estado);
        Assert.Equal(EstadoActa.Ok, porOperacion.Estado);
        Assert.Equal(Foto(actaWeb.Valor!), Foto(actaMovil.Valor!));
    }

    [Fact]
    public async Task Revisar_por_la_web_y_por_la_operacion_dejan_la_misma_acta()
    {
        var catalogos = await SembrarAsync();
        var web = await PrepararAsync(catalogos);
        var movil = await PrepararAsync(catalogos);
        var revisor = await UsuarioAsync(PerfilesUsuario.Revisor, catalogos);

        var porWeb = await _servicio.ActualizarAsync(web, SolicitudRevision(catalogos, web, false), revisor.UsuarioId, CancellationToken.None);
        var linea = (await _servicio.ObtenerAsync(movil, revisor.UsuarioId, CancellationToken.None)).Valor!.Detalles.Single();
        var version = await VersionLineaAsync(linea.Id);
        var porOperacion = await _operaciones.RevisarAsync(movil, linea.Id, version, false, revisor, CancellationToken.None);

        Assert.Equal(EstadoActa.Ok, porWeb.Estado);
        Assert.Equal(EstadoActa.Ok, porOperacion.Estado);
        Assert.Equal(Foto(porWeb.Valor!), Foto((await _servicio.ObtenerAsync(movil, revisor.UsuarioId, CancellationToken.None)).Valor!));
    }

    [Theory]
    [InlineData(PerfilesUsuario.Arquitecto)]
    [InlineData(PerfilesUsuario.Contratista)]
    [InlineData(PerfilesUsuario.Revisor)]
    public async Task Los_errores_de_la_web_y_de_la_operacion_coinciden(int perfil)
    {
        var catalogos = await SembrarAsync();
        var numero = await PrepararAsync(catalogos);
        var actor = await UsuarioAsync(perfil, catalogos);
        await _servicio.CambiarCierreAsync(numero, true, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var cerradaWeb = await _servicio.ActualizarAsync(numero, Solicitud(catalogos), actor.UsuarioId, CancellationToken.None);
        var cerradaMovil = perfil == PerfilesUsuario.Revisor
            ? await _operaciones.RevisarAsync(numero, 1, "version", false, actor, CancellationToken.None)
            : await _operaciones.AgregarAsync(numero, Guid.NewGuid(), Linea(catalogos, "No", 1, 1), actor, CancellationToken.None);
        await _servicio.CambiarCierreAsync(numero, false, SemillaInicial.UsuarioAdminId, CancellationToken.None);

        var cantidad = Linea(catalogos, "Mala", 0, 10);
        var cantidadWeb = await _servicio.ActualizarAsync(numero, SolicitudCon(catalogos, numero, cantidad, actor), actor.UsuarioId, CancellationToken.None);
        var cantidadMovil = await _operaciones.AgregarAsync(numero, Guid.NewGuid(), cantidad, actor, CancellationToken.None);
        var precio = Linea(catalogos, "Mala", 1, -1);
        var precioWeb = await _servicio.ActualizarAsync(numero, SolicitudCon(catalogos, numero, precio, actor), actor.UsuarioId, CancellationToken.None);
        var precioMovil = await _operaciones.AgregarAsync(numero, Guid.NewGuid(), precio, actor, CancellationToken.None);

        Assert.Equal(cerradaWeb.Mensaje, cerradaMovil.Mensaje);
        Assert.Equal(CrearActaValidador.MensajeCerrada, cerradaWeb.Mensaje);
        if (perfil == PerfilesUsuario.Revisor)
        {
            var agregaWeb = await _servicio.ActualizarAsync(numero, SolicitudCon(catalogos, numero, Linea(catalogos, "No", 1, 1), actor), actor.UsuarioId, CancellationToken.None);
            var agregaMovil = await _operaciones.AgregarAsync(numero, Guid.NewGuid(), Linea(catalogos, "No", 1, 1), actor, CancellationToken.None);
            Assert.Equal(CrearActaValidador.MensajeRevisorAgrega, agregaWeb.Mensaje);
            Assert.Equal(agregaWeb.Mensaje, agregaMovil.Mensaje);
        }
        else
        {
            Assert.Equal(cantidadWeb.Mensaje, cantidadMovil.Mensaje);
            Assert.Equal(CrearActaValidador.MensajeCantidad, cantidadWeb.Mensaje);
            Assert.Equal(precioWeb.Mensaje, precioMovil.Mensaje);
            Assert.Equal(CrearActaValidador.MensajePrecio, precioWeb.Mensaje);
        }
    }

    public void Dispose()
    {
        _context.Dispose();
        _conexion.Dispose();
    }

    private async Task<CatalogoOperacion> SembrarAsync()
    {
        var arquitecto = new Arquitecto { Nombre = "Arquitecto prueba" };
        var revisor = new Revisor { Nombre = "Revisor prueba" };
        var contratista = new Contratista { Nombre = "Contratista prueba" };
        var construccion = new Construccion { Descripcion = "Obra prueba" };
        var concepto = new Concepto { Descripcion = "Concepto prueba" };
        var unidad = new UnidadMedida { Nombre = "m2", Descripcion = "Metro cuadrado" };
        _context.AddRange(arquitecto, revisor, contratista, construccion, concepto, unidad);
        await _context.SaveChangesAsync();
        return new CatalogoOperacion(arquitecto.Id, revisor.Id, contratista.Id, construccion.Id, concepto.Id, unidad.Id);
    }

    private async Task<int> PrepararAsync(CatalogoOperacion catalogos)
    {
        var creada = await _servicio.CrearAsync(Solicitud(catalogos), SemillaInicial.UsuarioAdminId, CancellationToken.None);
        return creada.Valor!.NumeroActa;
    }

    private async Task<ActorActa> UsuarioAsync(int perfil, CatalogoOperacion catalogos)
    {
        var usuario = new Usuario
        {
            NombreUsuario = "p" + perfil + Guid.NewGuid().ToString("N")[..8],
            Nombre = "Usuario " + perfil,
            PasswordHash = "x",
            TipoUsuarioId = perfil,
            ArquitectoId = perfil == PerfilesUsuario.Arquitecto ? catalogos.ArquitectoId : null,
            ContratistaId = perfil == PerfilesUsuario.Contratista ? catalogos.ContratistaId : null,
            RevisorId = perfil == PerfilesUsuario.Revisor ? catalogos.RevisorId : null
        };
        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();
        return new ActorActa(usuario.Id, perfil, usuario.ArquitectoId, usuario.ContratistaId, usuario.RevisorId);
    }

    private async Task<string> VersionLineaAsync(int id) =>
        Convert.ToBase64String((await _context.ActaDetalles.AsNoTracking().SingleAsync(detalle => detalle.Id == id)).RowVersion);

    private CrearActaSolicitud SolicitudCon(CatalogoOperacion catalogos, int numero, LineaActaSolicitud extra, ActorActa actor)
    {
        var solicitud = Solicitud(catalogos);
        var actuales = _context.ActaDetalles.AsNoTracking().Where(detalle => detalle.NumeroActa == numero).ToList();
        solicitud.Detalles = actuales.Select(detalle => new LineaActaSolicitud
        {
            Id = detalle.Id,
            ConstruccionId = detalle.ConstruccionId,
            ConceptoId = detalle.ConceptoId,
            UnidadMedidaId = detalle.UnidadMedidaId,
            InsumoId = detalle.InsumoId,
            PresupuestoItemId = detalle.PresupuestoItemId,
            Cantidad = detalle.Cantidad,
            Precio = detalle.Precio,
            Descripcion = detalle.Descripcion,
            Aprobado = detalle.Aprobado
        }).ToList();
        solicitud.Detalles.Add(extra);
        return solicitud;
    }

    private CrearActaSolicitud SolicitudRevision(CatalogoOperacion catalogos, int numero, bool aprobado)
    {
        var solicitud = Solicitud(catalogos);
        var detalle = _context.ActaDetalles.AsNoTracking().Single(item => item.NumeroActa == numero);
        solicitud.Detalles =
        [
            new LineaActaSolicitud
            {
                Id = detalle.Id,
                ConstruccionId = detalle.ConstruccionId,
                ConceptoId = detalle.ConceptoId,
                UnidadMedidaId = detalle.UnidadMedidaId,
                Cantidad = detalle.Cantidad,
                Precio = detalle.Precio,
                Descripcion = detalle.Descripcion,
                Aprobado = aprobado
            }
        ];
        return solicitud;
    }

    private static CrearActaSolicitud Solicitud(CatalogoOperacion catalogos) => new()
    {
        FechaInicial = new DateOnly(2026, 11, 1),
        FechaFinal = new DateOnly(2026, 11, 9),
        ArquitectoId = catalogos.ArquitectoId,
        RevisorId = catalogos.RevisorId,
        ContratistaId = catalogos.ContratistaId,
        Detalles = [Linea(catalogos, "Muro", 2, 100)]
    };

    private static LineaActaSolicitud Linea(CatalogoOperacion catalogos, string descripcion, decimal cantidad, decimal precio) => new()
    {
        ConstruccionId = catalogos.ConstruccionId,
        ConceptoId = catalogos.ConceptoId,
        UnidadMedidaId = catalogos.UnidadId,
        Cantidad = cantidad,
        Precio = precio,
        Descripcion = descripcion
    };

    private static IReadOnlyList<string> Foto(ActaDto acta) =>
        acta.Detalles
            .OrderBy(detalle => detalle.TipoUsuarioId)
            .ThenBy(detalle => detalle.Descripcion)
            .Select(detalle => $"{detalle.TipoUsuarioId}|{detalle.Cantidad}|{detalle.Precio}|{detalle.Descripcion}|{detalle.Aprobado}|{detalle.Subtotal}")
            .ToList();

    private sealed record CatalogoOperacion(int ArquitectoId, int RevisorId, int ContratistaId, int ConstruccionId, int ConceptoId, int UnidadId);
}

public sealed class WebDespuesDeMovilTests : IDisposable
{
    private readonly SqliteConnection _conexion = new("Data Source=:memory:");
    private readonly ArquitectosDbContext _context;
    private readonly ActaServicio _servicio;
    private readonly OperacionesLineaActa _operaciones;

    public WebDespuesDeMovilTests()
    {
        _conexion.Open();
        var opciones = new DbContextOptionsBuilder<ArquitectosDbContext>().UseSqlite(_conexion).Options;
        _context = new ArquitectosDbContext(opciones);
        _context.Database.EnsureCreated();
        _operaciones = new OperacionesLineaActa(_context, new AuditoriaServicio(_context, new HttpContextAccessor()));
        _servicio = new ActaServicio(_context, operaciones: _operaciones);
    }

    [Fact]
    public async Task Un_guardado_web_con_la_version_vieja_no_borra_la_linea_del_movil()
    {
        var arquitecto = new Arquitecto { Nombre = "Arquitecto prueba" };
        var revisor = new Revisor { Nombre = "Revisor prueba" };
        var contratista = new Contratista { Nombre = "Contratista prueba" };
        var construccion = new Construccion { Descripcion = "Obra prueba" };
        var concepto = new Concepto { Descripcion = "Concepto prueba" };
        var unidad = new UnidadMedida { Nombre = "m2", Descripcion = "Metro cuadrado" };
        _context.AddRange(arquitecto, revisor, contratista, construccion, concepto, unidad);
        await _context.SaveChangesAsync();
        var solicitud = new CrearActaSolicitud
        {
            FechaInicial = new DateOnly(2026, 11, 1),
            FechaFinal = new DateOnly(2026, 11, 9),
            ArquitectoId = arquitecto.Id,
            RevisorId = revisor.Id,
            ContratistaId = contratista.Id,
            Detalles =
            [
                new LineaActaSolicitud
                {
                    ConstruccionId = construccion.Id,
                    ConceptoId = concepto.Id,
                    UnidadMedidaId = unidad.Id,
                    Cantidad = 2,
                    Precio = 100,
                    Descripcion = "Muro"
                }
            ]
        };
        var creada = await _servicio.CrearAsync(solicitud, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var numero = creada.Valor!.NumeroActa;
        var original = await _servicio.ObtenerAsync(numero, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var actor = new ActorActa(SemillaInicial.UsuarioAdminId, PerfilesUsuario.Arquitecto, arquitecto.Id, null, null);
        var agregada = await _operaciones.AgregarAsync(
            numero,
            Guid.NewGuid(),
            new LineaActaSolicitud
            {
                ConstruccionId = construccion.Id,
                ConceptoId = concepto.Id,
                UnidadMedidaId = unidad.Id,
                Cantidad = 1,
                Precio = 40,
                Descripcion = "Desde el móvil"
            },
            actor,
            CancellationToken.None);
        solicitud.RowVersion = original.Valor!.RowVersion;
        solicitud.Detalles[0].Id = original.Valor.Detalles.Single().Id;

        var puesta = await _servicio.ActualizarAsync(numero, solicitud, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var acta = await _servicio.ObtenerAsync(numero, SemillaInicial.UsuarioAdminId, CancellationToken.None);

        Assert.Equal(EstadoActa.Ok, agregada.Estado);
        Assert.Equal(EstadoActa.Conflicto, puesta.Estado);
        Assert.Equal(2, acta.Valor!.Detalles.Count);
        Assert.Contains(acta.Valor.Detalles, detalle => detalle.Descripcion == "Desde el móvil");
    }

    public void Dispose()
    {
        _context.Dispose();
        _conexion.Dispose();
    }
}
