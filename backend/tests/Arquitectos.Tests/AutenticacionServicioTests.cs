using Arquitectos.Application.Auth;
using Arquitectos.Application.Usuarios;
using Arquitectos.Infrastructure.Persistence;
using Arquitectos.Infrastructure.Seguridad;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Arquitectos.Tests;

public sealed class AutenticacionServicioTests : IDisposable
{
    private const string Clave = "Clave12345";

    private readonly SqliteConnection _conexion;
    private readonly ArquitectosDbContext _context;
    private readonly UsuarioServicio _usuarios;
    private readonly AutenticacionServicio _autenticacion;

    public AutenticacionServicioTests()
    {
        _conexion = new SqliteConnection("Data Source=:memory:");
        _conexion.Open();
        var opciones = new DbContextOptionsBuilder<ArquitectosDbContext>()
            .UseSqlite(_conexion)
            .Options;
        _context = new ArquitectosDbContext(opciones);
        _context.Database.EnsureCreated();
        _autenticacion = new AutenticacionServicio(
            _context,
            Emisor(),
            TimeProvider.System,
            new ValidadorSesion(_context, new MemoryCache(new MemoryCacheOptions())));
        _usuarios = new UsuarioServicio(_context, _autenticacion);
    }

    [Fact]
    public async Task Cinco_fallos_bloquean_la_cuenta_aunque_la_siguiente_clave_sea_correcta()
    {
        var creado = await _usuarios.CrearAsync(Solicitud("ana"), CancellationToken.None);
        for (var intento = 0; intento < 5; intento++)
        {
            var fallo = await _autenticacion.IniciarSesion(Login("ana", "mala123456"), null, null, CancellationToken.None);
            Assert.Null(fallo);
        }

        var bloqueado = await _autenticacion.IniciarSesion(Login("ana", Clave), null, null, CancellationToken.None);
        var usuario = await _context.Usuarios.SingleAsync(item => item.Id == creado.Valor!.Id);

        Assert.Null(bloqueado);
        Assert.Equal(5, usuario.IntentosFallidos);
        Assert.NotNull(usuario.BloqueadoHasta);
    }

    [Fact]
    public async Task Un_ingreso_correcto_reinicia_el_contador_e_informa_el_cambio_obligatorio()
    {
        await _usuarios.CrearAsync(Solicitud("ana"), CancellationToken.None);
        Assert.Null(await _autenticacion.IniciarSesion(Login("ana", "mala123456"), null, null, CancellationToken.None));
        Assert.Null(await _autenticacion.IniciarSesion(Login("ana", "mala123456"), null, null, CancellationToken.None));

        var respuesta = await _autenticacion.IniciarSesion(Login("ana", Clave), null, null, CancellationToken.None);
        var usuario = await _context.Usuarios.SingleAsync(item => item.NombreUsuario == "ana");

        Assert.NotNull(respuesta);
        Assert.True(respuesta.Datos.DebeCambiarContrasena);
        Assert.Equal(0, usuario.IntentosFallidos);
        Assert.Null(usuario.BloqueadoHasta);
        Assert.True(await _context.Usuarios
            .Where(item => item.NombreUsuario == "admin")
            .Select(item => item.DebeCambiarContrasena)
            .SingleAsync());
    }

    public void Dispose()
    {
        _context.Dispose();
        _conexion.Dispose();
    }

    private static EmisorJwt Emisor() =>
        new(Options.Create(new OpcionesJwt
        {
            Issuer = "Arquitectos",
            Audience = "Arquitectos",
            Key = new string('k', 40),
            ExpirationMinutes = 20
        }));

    private static LoginSolicitud Login(string usuario, string password) =>
        new() { Usuario = usuario, Password = password };

    private static CrearUsuarioSolicitud Solicitud(string usuario) => new()
    {
        Usuario = usuario,
        Nombre = "Ana",
        Password = Clave,
        TipoUsuarioId = SemillaInicial.TipoAdministradorId
    };
}
