using Arquitectos.Application.Actas;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Actas;
using Arquitectos.Infrastructure.Auditoria;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Tests;

public sealed class AuditoriaTests : IDisposable
{
    private readonly SqliteConnection _conexion;
    private readonly ArquitectosDbContext _context;

    public AuditoriaTests()
    {
        _conexion = new SqliteConnection("Data Source=:memory:");
        _conexion.Open();
        var interceptor = new AuditoriaInterceptor(new HttpContextAccessor());
        var opciones = new DbContextOptionsBuilder<ArquitectosDbContext>()
            .UseSqlite(_conexion)
            .AddInterceptors(interceptor)
            .Options;
        _context = new ArquitectosDbContext(opciones);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Modificar_un_acta_registra_el_campo_cambiado()
    {
        var arquitecto = new Arquitecto { Nombre = "Ana" };
        var revisor = new Revisor { Nombre = "Rosa" };
        var contratista = new Contratista { Nombre = "Carlos" };
        _context.AddRange(arquitecto, revisor, contratista);
        await _context.SaveChangesAsync();
        var acta = new Acta
        {
            FechaInicial = new DateOnly(2026, 11, 1),
            FechaFinal = new DateOnly(2026, 11, 9),
            ArquitectoId = arquitecto.Id,
            RevisorId = revisor.Id,
            ContratistaId = contratista.Id,
            Observacion = "Inicial",
            FechaRegistro = DateTime.UtcNow
        };
        _context.Actas.Add(acta);
        await _context.SaveChangesAsync();

        acta.Observacion = "Corregida";
        await _context.SaveChangesAsync();

        var registro = await _context.Auditorias.SingleAsync(item =>
            item.Entidad == "Acta" && item.Accion == AccionAuditoria.Modificar);

        Assert.Equal(acta.NumeroActa.ToString(), registro.EntidadId);
        Assert.Contains("Observacion", registro.CambiosJson);
        Assert.Contains("Corregida", registro.CambiosJson);
    }

    [Fact]
    public async Task Aprobar_un_item_registra_la_accion_Aprobar()
    {
        var catalogos = await SembrarAsync();
        var servicio = new ActaServicio(_context, new AuditoriaServicio(_context, new HttpContextAccessor()));
        var creada = await servicio.CrearAsync(Solicitud(catalogos), SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var numero = creada.Valor!.NumeroActa;
        var revisor = new Usuario
        {
            NombreUsuario = "revisor-aud",
            Nombre = "Revisor",
            PasswordHash = "x",
            TipoUsuarioId = SemillaInicial.TipoRevisorId,
            RevisorId = catalogos.RevisorId
        };
        _context.Usuarios.Add(revisor);
        await _context.SaveChangesAsync();
        var acta = await servicio.ObtenerAsync(numero, revisor.Id, CancellationToken.None);
        var linea = acta.Valor!.Detalles.Single();
        var rechazo = Solicitud(catalogos);
        rechazo.Detalles[0].Id = linea.Id;
        rechazo.Detalles[0].Aprobado = false;
        await servicio.ActualizarAsync(numero, rechazo, revisor.Id, CancellationToken.None);
        var aprobacion = Solicitud(catalogos);
        aprobacion.Detalles[0].Id = linea.Id;
        aprobacion.Detalles[0].Aprobado = true;

        var resultado = await servicio.ActualizarAsync(numero, aprobacion, revisor.Id, CancellationToken.None);
        var registros = await _context.Auditorias
            .Where(item => item.Accion == AccionAuditoria.Aprobar && item.Entidad == "ActaDetalle")
            .ToListAsync();

        Assert.Equal(EstadoActa.Ok, resultado.Estado);
        Assert.Contains(registros, item => item.EntidadId == linea.Id.ToString());
    }

    [Fact]
    public async Task PasswordHash_no_aparece_en_los_cambios()
    {
        var usuario = await _context.Usuarios.SingleAsync(item => item.Id == SemillaInicial.UsuarioAdminId);
        usuario.PasswordHash = "hash-nuevo";
        usuario.Nombre = "Administrador editado";
        await _context.SaveChangesAsync();

        var textos = await _context.Auditorias.Select(item => item.CambiosJson ?? "").ToListAsync();
        var junto = string.Join('\n', textos);

        Assert.Contains("Nombre", junto);
        Assert.DoesNotContain("PasswordHash", junto);
    }

    public void Dispose()
    {
        _context.Dispose();
        _conexion.Dispose();
    }

    private async Task<(int ArquitectoId, int RevisorId, int ContratistaId, int ConstruccionId, int ConceptoId, int UnidadId)> SembrarAsync()
    {
        var arquitecto = new Arquitecto { Nombre = "Ana" };
        var revisor = new Revisor { Nombre = "Rosa" };
        var contratista = new Contratista { Nombre = "Carlos" };
        var obra = new Construccion { Descripcion = "Obra", Propietario = "Dueño" };
        var concepto = new Concepto { Descripcion = "Muro", Unidad = "m2" };
        var unidad = new UnidadMedida { Nombre = "m2", Descripcion = "Metro" };
        _context.AddRange(arquitecto, revisor, contratista, obra, concepto, unidad);
        await _context.SaveChangesAsync();
        return (arquitecto.Id, revisor.Id, contratista.Id, obra.Id, concepto.Id, unidad.Id);
    }

    private static CrearActaSolicitud Solicitud((int ArquitectoId, int RevisorId, int ContratistaId, int ConstruccionId, int ConceptoId, int UnidadId) catalogos) => new()
    {
        FechaInicial = new DateOnly(2026, 11, 1),
        FechaFinal = new DateOnly(2026, 11, 9),
        ArquitectoId = catalogos.ArquitectoId,
        RevisorId = catalogos.RevisorId,
        ContratistaId = catalogos.ContratistaId,
        Detalles =
        [
            new LineaActaSolicitud
            {
                ConstruccionId = catalogos.ConstruccionId,
                ConceptoId = catalogos.ConceptoId,
                UnidadMedidaId = catalogos.UnidadId,
                Cantidad = 2,
                Precio = 100,
                Descripcion = "Muro"
            }
        ]
    };
}
