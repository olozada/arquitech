using Arquitectos.Application.Movil;
using Arquitectos.Application.Usuarios;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Actas;
using Arquitectos.Infrastructure.Auditoria;
using Arquitectos.Infrastructure.Movil;
using Arquitectos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
namespace Arquitectos.Tests;

public sealed class SincronizadorMovilConcurrenciaTests : IAsyncLifetime
{
    private readonly SqliteConnection _conexion = new("Data Source=sincronizador-unidad;Mode=Memory;Cache=Shared");
    private ArquitectosDbContext _contexto = null!;
    private SincronizadorMovil _sincronizador = null!;
    private int _usuarioId;
    private Guid _dispositivoId;
    private int _acta;
    private int _construccionId;
    private int _conceptoId;

    public async Task InitializeAsync()
    {
        await _conexion.OpenAsync();
        var acceso = new HttpContextAccessor();
        var http = new DefaultHttpContext();
        http.Request.Path = "/api/movil/v1/sincronizar";
        acceso.HttpContext = http;
        var opciones = new DbContextOptionsBuilder<ArquitectosDbContext>()
            .UseSqlite(_conexion)
            .AddInterceptors(new AuditoriaInterceptor(acceso))
            .Options;
        _contexto = new ArquitectosDbContext(opciones);
        await _contexto.Database.EnsureCreatedAsync();
        var arquitecto = new Arquitecto { Nombre = "Arq conc" };
        var revisor = new Revisor { Nombre = "Rev conc" };
        var contratista = new Contratista { Nombre = "Con conc" };
        var obra = new Construccion { Descripcion = "Obra" };
        var concepto = new Concepto { Descripcion = "Concepto", Unidad = "m" };
        _contexto.AddRange(arquitecto, revisor, contratista, obra, concepto);
        await _contexto.SaveChangesAsync();
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var acta = new Acta
        {
            FechaInicial = hoy,
            FechaFinal = hoy,
            ArquitectoId = arquitecto.Id,
            RevisorId = revisor.Id,
            ContratistaId = contratista.Id,
            FechaRegistro = DateTime.UtcNow
        };
        _contexto.Actas.Add(acta);
        var usuario = new Usuario
        {
            NombreUsuario = "arq.conc",
            Nombre = "Arq",
            PasswordHash = "x",
            TipoUsuarioId = SemillaInicial.TipoArquitectoId,
            ArquitectoId = arquitecto.Id,
            Activo = true,
        };
        _contexto.Usuarios.Add(usuario);
        await _contexto.SaveChangesAsync();
        _dispositivoId = Guid.NewGuid();
        _contexto.Dispositivos.Add(new Dispositivo
        {
            Id = _dispositivoId,
            UsuarioId = usuario.Id,
            Nombre = "Tel",
            Plataforma = "android",
            VersionApp = "1.0.0",
            RegistradoEn = DateTime.UtcNow,
        });
        await _contexto.SaveChangesAsync();
        _usuarioId = usuario.Id;
        _construccionId = obra.Id;
        _conceptoId = concepto.Id;
        _acta = acta.NumeroActa;
        var auditoria = new AuditoriaServicio(_contexto, acceso);
        var operaciones = new OperacionesLineaActa(_contexto, auditoria);
        _sincronizador = new SincronizadorMovil(
            _contexto,
            operaciones,
            Options.Create(new OpcionesMovil { VersionEsquema = 1, MaxOperacionesLote = 500 }),
            TimeProvider.System,
            NullLogger<SincronizadorMovil>.Instance);
    }

    [Fact]
    public async Task Dos_llamadas_paralelas_con_el_mismo_id_operacion_no_duplican_linea()
    {
        var idOperacion = Guid.NewGuid();
        var idCliente = Guid.NewGuid();
        var solicitud = new SincronizacionMovilSolicitud
        {
            DispositivoId = _dispositivoId,
            VersionEsquema = 1,
            Operaciones =
            [
                new OperacionMovilSolicitud
                {
                    IdOperacion = idOperacion,
                    Secuencia = 1,
                    Tipo = "AgregarLinea",
                    NumeroActa = _acta,
                    IdCliente = idCliente,
                    CreadaEn = DateTime.UtcNow,
                    Linea = new LineaMovilSolicitud
                    {
                        ConstruccionId = _construccionId,
                        ConceptoId = _conceptoId,
                        Cantidad = 1,
                        Precio = 1,
                        Descripcion = "Concurrencia"
                    }
                }
            ]
        };
        var tarea1 = _sincronizador.SincronizarAsync(_usuarioId, null, solicitud, CancellationToken.None);
        var tarea2 = _sincronizador.SincronizarAsync(_usuarioId, null, solicitud, CancellationToken.None);
        var resultados = await Task.WhenAll(tarea1, tarea2);
        Assert.All(resultados, item => Assert.Equal(EstadoSincronizacion.Ok, item.Estado));
        var lineas = await _contexto.ActaDetalles.CountAsync(item => item.Descripcion == "Concurrencia");
        Assert.Equal(1, lineas);
        var operacionesGuardadas = await _contexto.OperacionesMovil.CountAsync(
            item => item.IdOperacion == idOperacion);
        Assert.Equal(1, operacionesGuardadas);
    }

    public async Task DisposeAsync()
    {
        await _contexto.DisposeAsync();
        await _conexion.DisposeAsync();
    }
}
