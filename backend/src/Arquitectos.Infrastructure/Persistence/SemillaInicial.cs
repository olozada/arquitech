using Arquitectos.Application.Usuarios;
using Arquitectos.Domain.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Persistence;

public static class SemillaInicial
{
    public const int TipoAdministradorId = PerfilesUsuario.Administrador;

    public const int TipoArquitectoId = PerfilesUsuario.Arquitecto;

    public const int TipoContratistaId = PerfilesUsuario.Contratista;

    public const int TipoRevisorId = PerfilesUsuario.Revisor;

    public const int UsuarioAdminId = 1;

    private const string AdminPasswordHash =
        "AQAAAAIAAYagAAAAEL0ccqOmHJF693FpHgqXvF0RaUM3+mkM0eAUTDbLMZ7ItRQKN6V2h0RqsGcQ0ovuXg==";

    public static void Aplicar(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TipoUsuario>().HasData(
            new TipoUsuario { Id = TipoAdministradorId, Nombre = "Administrador" },
            new TipoUsuario { Id = TipoArquitectoId, Nombre = "Arquitecto" },
            new TipoUsuario { Id = TipoContratistaId, Nombre = "Contratista" },
            new TipoUsuario { Id = TipoRevisorId, Nombre = "Revisor" });

        modelBuilder.Entity<Usuario>().HasData(
            new Usuario
            {
                Id = UsuarioAdminId,
                NombreUsuario = "admin",
                Nombre = "Administrador",
                PasswordHash = AdminPasswordHash,
                TipoUsuarioId = TipoAdministradorId,
                Activo = true,
                DebeCambiarContrasena = true
            });

        var opciones = OpcionesMenu();
        modelBuilder.Entity<MenuOpcion>().HasData(opciones);
        var permisos = opciones
            .Select(opcion => new MenuOpcionTipoUsuario
            {
                MenuOpcionId = opcion.Id,
                TipoUsuarioId = TipoAdministradorId
            })
            .ToList();
        foreach (var tipo in new[] { TipoArquitectoId, TipoContratistaId, TipoRevisorId })
        {
            permisos.Add(new MenuOpcionTipoUsuario { MenuOpcionId = 11, TipoUsuarioId = tipo });
            permisos.Add(new MenuOpcionTipoUsuario { MenuOpcionId = 16, TipoUsuarioId = tipo });
        }

        permisos.Add(new MenuOpcionTipoUsuario { MenuOpcionId = 18, TipoUsuarioId = TipoArquitectoId });
        permisos.Add(new MenuOpcionTipoUsuario { MenuOpcionId = 19, TipoUsuarioId = TipoArquitectoId });

        modelBuilder.Entity<MenuOpcionTipoUsuario>().HasData(permisos);
    }

    private static MenuOpcion[] OpcionesMenu() =>
    [
        new() { Id = 17, Descripcion = "Dashboard", Url = "/", Icono = "layout-dashboard", Orden = 0 },
        new() { Id = 1, Descripcion = "Administración", Icono = "settings", Orden = 1 },
        new() { Id = 2, PadreId = 1, Descripcion = "Arquitectos", Url = "/admin/arquitectos", Icono = "pencil-ruler", Orden = 1 },
        new() { Id = 3, PadreId = 1, Descripcion = "Revisores", Url = "/admin/revisores", Icono = "clipboard-check", Orden = 2 },
        new() { Id = 4, PadreId = 1, Descripcion = "Contratistas", Url = "/admin/contratistas", Icono = "hard-hat", Orden = 3 },
        new() { Id = 5, PadreId = 1, Descripcion = "Proveedores", Url = "/admin/proveedores", Icono = "truck", Orden = 4 },
        new() { Id = 6, PadreId = 1, Descripcion = "Construcciones", Url = "/admin/construcciones", Icono = "building-2", Orden = 5 },
        new() { Id = 7, PadreId = 1, Descripcion = "Conceptos", Url = "/admin/conceptos", Icono = "list-tree", Orden = 6 },
        new() { Id = 8, PadreId = 1, Descripcion = "Insumos", Url = "/presupuesto/insumos", Icono = "package", Orden = 7 },
        new() { Id = 9, PadreId = 1, Descripcion = "Unidades de medida", Url = "/admin/unidades-medida", Icono = "ruler", Orden = 8 },
        new() { Id = 10, PadreId = 1, Descripcion = "Materiales por proveedor", Url = "/admin/proveedores-materiales", Icono = "badge-dollar-sign", Orden = 9 },
        new() { Id = 15, PadreId = 1, Descripcion = "Usuarios", Url = "/admin/usuarios", Icono = "users", Orden = 10 },
        new() { Id = 16, PadreId = 1, Descripcion = "Cambiar contraseña", Url = "/cuenta/contrasena", Icono = "key-round", Orden = 11 },
        new() { Id = 11, Descripcion = "Actas", Url = "/actas", Icono = "file-pen-line", Orden = 2 },
        new() { Id = 13, Descripcion = "Informes", Icono = "chart-column", Orden = 3 },
        new() { Id = 14, PadreId = 13, Descripcion = "Reporte de actas", Url = "/informes/actas", Icono = "table", Orden = 1 },
        new() { Id = 23, PadreId = 13, Descripcion = "Reporte por materiales", Url = "/informes/materiales", Icono = "brick-wall", Orden = 2 },
        new() { Id = 18, Descripcion = "Presupuestos", Icono = "wallet", Orden = 4 },
        new() { Id = 19, PadreId = 18, Descripcion = "Presupuestos de obra", Url = "/presupuestos", Icono = "file-spreadsheet", Orden = 1 },
        new() { Id = 20, PadreId = 18, Descripcion = "Insumos", Url = "/presupuesto/insumos", Icono = "package", Orden = 2 },
        new() { Id = 21, PadreId = 18, Descripcion = "Mano de obra", Url = "/presupuesto/mano-obra", Icono = "hammer", Orden = 3 },
        new() { Id = 22, PadreId = 18, Descripcion = "Análisis de precios", Url = "/presupuesto/apu", Icono = "calculator", Orden = 4 }
    ];
}
