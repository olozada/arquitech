using Arquitectos.Application.Auth;
using Arquitectos.Application.Usuarios;
using Arquitectos.Domain.Entidades;
using Arquitectos.Infrastructure.Persistence;
using Arquitectos.Infrastructure.Seguridad;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Tests;

public sealed class UsuarioServicioTests : IDisposable
{
    private const string ClaveInicial = "Clave12345";
    private const string ClaveNueva = "Otra123456";

    private readonly SqliteConnection _conexion;
    private readonly ArquitectosDbContext _context;
    private readonly UsuarioServicio _servicio;

    public UsuarioServicioTests()
    {
        _conexion = new SqliteConnection("Data Source=:memory:");
        _conexion.Open();
        var opciones = new DbContextOptionsBuilder<ArquitectosDbContext>()
            .UseSqlite(_conexion)
            .Options;
        _context = new ArquitectosDbContext(opciones);
        _context.Database.EnsureCreated();
        _servicio = new UsuarioServicio(_context, new RevocacionNula());
    }

    [Fact]
    public async Task Cambiar_contrasena_rechaza_la_actual_y_acepta_una_nueva()
    {
        var creado = await _servicio.CrearAsync(Solicitud("ana"), CancellationToken.None);
        var incorrecta = await _servicio.CambiarContrasenaAsync(
            creado.Valor!.Id,
            new CambiarContrasenaSolicitud { ContrasenaActual = "mal", ContrasenaNueva = ClaveNueva },
            CancellationToken.None);
        var igual = await _servicio.CambiarContrasenaAsync(
            creado.Valor.Id,
            new CambiarContrasenaSolicitud { ContrasenaActual = ClaveInicial, ContrasenaNueva = ClaveInicial },
            CancellationToken.None);
        var cambiada = await _servicio.CambiarContrasenaAsync(
            creado.Valor.Id,
            new CambiarContrasenaSolicitud { ContrasenaActual = ClaveInicial, ContrasenaNueva = ClaveNueva },
            CancellationToken.None);
        var hash = await _context.Usuarios.Where(usuario => usuario.Id == creado.Valor.Id)
            .Select(usuario => usuario.PasswordHash)
            .SingleAsync();

        Assert.Equal(EstadoUsuario.Invalido, incorrecta.Estado);
        Assert.Equal(MensajesUsuario.ActualIncorrecta, incorrecta.Mensaje);
        Assert.Equal(MensajesUsuario.Distinta, igual.Mensaje);
        Assert.Equal(EstadoUsuario.Ok, cambiada.Estado);
        Assert.Equal(
            PasswordVerificationResult.Success,
            new PasswordHasher<string>().VerifyHashedPassword("ana", hash, ClaveNueva));
    }

    [Fact]
    public async Task Eliminar_y_volver_a_crear_reactiva_el_usuario()
    {
        var creado = await _servicio.CrearAsync(Solicitud("ana"), CancellationToken.None);
        var duplicado = await _servicio.CrearAsync(Solicitud("ana"), CancellationToken.None);
        var eliminado = await _servicio.EliminarAsync(creado.Valor!.Id, SemillaInicial.UsuarioAdminId, CancellationToken.None);
        var recreado = await _servicio.CrearAsync(
            new CrearUsuarioSolicitud
            {
                Usuario = "ana",
                Nombre = "Ana Ruiz",
                Password = ClaveNueva,
                TipoUsuarioId = SemillaInicial.TipoAdministradorId
            },
            CancellationToken.None);
        var listado = await _servicio.ListarAsync(incluirInactivos: false, CancellationToken.None);

        Assert.Equal(EstadoUsuario.Conflicto, duplicado.Estado);
        Assert.Equal(EstadoUsuario.Ok, eliminado.Estado);
        Assert.Equal(creado.Valor.Id, recreado.Valor!.Id);
        Assert.Equal("Ana Ruiz", recreado.Valor.Nombre);
        Assert.True(recreado.Valor.Activo);
        Assert.Contains(listado, usuario => usuario.Id == creado.Valor.Id && usuario.Usuario == "ana");
        Assert.Equal(2, await _context.Usuarios.CountAsync());
    }

    [Fact]
    public async Task No_permite_desactivar_el_usuario_que_inicio_sesion()
    {
        var resultado = await _servicio.EliminarAsync(
            SemillaInicial.UsuarioAdminId,
            SemillaInicial.UsuarioAdminId,
            CancellationToken.None);

        Assert.Equal(EstadoUsuario.Invalido, resultado.Estado);
        Assert.Equal(MensajesUsuario.Propio, resultado.Mensaje);
        Assert.True(await _context.Usuarios.Where(usuario => usuario.Id == SemillaInicial.UsuarioAdminId)
            .Select(usuario => usuario.Activo)
            .SingleAsync());
    }

    public void Dispose()
    {
        _context.Dispose();
        _conexion.Dispose();
    }

    private sealed class RevocacionNula : IRevocacionSesiones
    {
        public Task RevocarUsuarioAsync(int usuarioId, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static CrearUsuarioSolicitud Solicitud(string usuario) => new()
    {
        Usuario = usuario,
        Nombre = "Ana",
        Password = ClaveInicial,
        TipoUsuarioId = SemillaInicial.TipoAdministradorId
    };
}
