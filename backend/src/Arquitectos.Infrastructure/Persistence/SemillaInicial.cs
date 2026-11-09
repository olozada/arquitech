using Arquitectos.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Persistence;

public static class SemillaInicial
{
    public const int TipoAdministradorId = 1;

    public const int UsuarioAdminId = 1;

    private const string AdminPasswordHash =
        "AQAAAAIAAYagAAAAEL0ccqOmHJF693FpHgqXvF0RaUM3+mkM0eAUTDbLMZ7ItRQKN6V2h0RqsGcQ0ovuXg==";

    public static void Aplicar(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TipoUsuario>().HasData(
            new TipoUsuario { Id = TipoAdministradorId, Nombre = "Administrador" });

        modelBuilder.Entity<Usuario>().HasData(
            new Usuario
            {
                Id = UsuarioAdminId,
                NombreUsuario = "admin",
                Nombre = "Administrador",
                PasswordHash = AdminPasswordHash,
                TipoUsuarioId = TipoAdministradorId,
                Activo = true
            });

        var opciones = OpcionesMenu();
        modelBuilder.Entity<MenuOpcion>().HasData(opciones);
        modelBuilder.Entity<MenuOpcionTipoUsuario>().HasData(
            opciones.Select(opcion => new MenuOpcionTipoUsuario
            {
                MenuOpcionId = opcion.Id,
                TipoUsuarioId = TipoAdministradorId
            }).ToArray());
    }

    private static MenuOpcion[] OpcionesMenu() =>
    [
        new() { Id = 17, Descripcion = "Dashboard", Url = "/", Icono = "fa fa-tachometer", Orden = 0 },
        new() { Id = 1, Descripcion = "Administración", Icono = "fa fa-cogs", Orden = 1 },
        new() { Id = 2, PadreId = 1, Descripcion = "Arquitectos", Url = "/admin/arquitectos", Icono = "fa fa-user", Orden = 1 },
        new() { Id = 3, PadreId = 1, Descripcion = "Revisores", Url = "/admin/revisores", Icono = "fa fa-eye", Orden = 2 },
        new() { Id = 4, PadreId = 1, Descripcion = "Contratistas", Url = "/admin/contratistas", Icono = "fa fa-briefcase", Orden = 3 },
        new() { Id = 5, PadreId = 1, Descripcion = "Proveedores", Url = "/admin/proveedores", Icono = "fa fa-truck", Orden = 4 },
        new() { Id = 6, PadreId = 1, Descripcion = "Construcciones", Url = "/admin/construcciones", Icono = "fa fa-building", Orden = 5 },
        new() { Id = 7, PadreId = 1, Descripcion = "Conceptos", Url = "/admin/conceptos", Icono = "fa fa-list", Orden = 6 },
        new() { Id = 8, PadreId = 1, Descripcion = "Materiales", Url = "/admin/materiales", Icono = "fa fa-cubes", Orden = 7 },
        new() { Id = 9, PadreId = 1, Descripcion = "Unidades de medida", Url = "/admin/unidades-medida", Icono = "fa fa-balance-scale", Orden = 8 },
        new() { Id = 10, PadreId = 1, Descripcion = "Materiales por proveedor", Url = "/admin/proveedores-materiales", Icono = "fa fa-tags", Orden = 9 },
        new() { Id = 15, PadreId = 1, Descripcion = "Usuarios", Url = "/admin/usuarios", Icono = "fa fa-users", Orden = 10 },
        new() { Id = 16, PadreId = 1, Descripcion = "Cambiar contraseña", Url = "/cuenta/contrasena", Icono = "fa fa-key", Orden = 11 },
        new() { Id = 11, Descripcion = "Actas", Icono = "fa fa-file-text", Orden = 2 },
        new() { Id = 12, PadreId = 11, Descripcion = "Toma de actas", Url = "/actas/nueva", Icono = "fa fa-pencil", Orden = 1 },
        new() { Id = 13, Descripcion = "Informes", Icono = "fa fa-bar-chart", Orden = 3 },
        new() { Id = 14, PadreId = 13, Descripcion = "Reporte de actas", Url = "/informes/actas", Icono = "fa fa-table", Orden = 1 }
    ];
}
