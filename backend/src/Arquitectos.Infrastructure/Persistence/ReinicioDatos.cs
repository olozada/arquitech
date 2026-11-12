using Microsoft.EntityFrameworkCore;

namespace Arquitectos.Infrastructure.Persistence;

public static class ReinicioDatos
{
    public static async Task VaciarYSembrarInicialAsync(
        ArquitectosDbContext context,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            IF OBJECT_ID(N'dbo.Auditoria') IS NOT NULL DELETE FROM Auditoria;
            DELETE FROM ActaDetalles;
            IF OBJECT_ID('PresupuestoMediciones') IS NOT NULL DELETE FROM PresupuestoMediciones;
            IF OBJECT_ID('PresupuestoItems') IS NOT NULL DELETE FROM PresupuestoItems;
            IF OBJECT_ID('PresupuestoCapitulos') IS NOT NULL DELETE FROM PresupuestoCapitulos;
            IF OBJECT_ID('Presupuestos') IS NOT NULL DELETE FROM Presupuestos;
            IF OBJECT_ID('ApuLineas') IS NOT NULL DELETE FROM ApuLineas;
            IF OBJECT_ID('ApuGrupos') IS NOT NULL DELETE FROM ApuGrupos;
            IF OBJECT_ID('Apus') IS NOT NULL DELETE FROM Apus;
            IF OBJECT_ID('CuadrillaCargos') IS NOT NULL DELETE FROM CuadrillaCargos;
            IF OBJECT_ID('Cuadrillas') IS NOT NULL DELETE FROM Cuadrillas;
            IF OBJECT_ID('Cargos') IS NOT NULL DELETE FROM Cargos;
            IF OBJECT_ID('PrestacionesSociales') IS NOT NULL DELETE FROM PrestacionesSociales;
            IF OBJECT_ID('InsumoPreciosHistorial') IS NOT NULL DELETE FROM InsumoPreciosHistorial;
            IF OBJECT_ID('Insumos') IS NOT NULL DELETE FROM Insumos;
            DELETE FROM Actas;
            DELETE FROM ProveedorMateriales;
            DELETE FROM MenuOpcionTipoUsuario;
            DELETE FROM MenuOpciones WHERE PadreId IS NOT NULL;
            DELETE FROM MenuOpciones;
            IF OBJECT_ID(N'dbo.SesionesUsuario') IS NOT NULL DELETE FROM SesionesUsuario;
            DELETE FROM Usuarios;
            DELETE FROM TiposUsuario;
            DELETE FROM Arquitectos;
            DELETE FROM Revisores;
            DELETE FROM Contratistas;
            DELETE FROM Proveedores;
            DELETE FROM Construcciones;
            DELETE FROM Conceptos;
            DELETE FROM UnidadesMedida;

            DBCC CHECKIDENT ('ActaDetalles', RESEED, 0);
            DBCC CHECKIDENT ('Actas', RESEED, 0);
            DBCC CHECKIDENT ('Arquitectos', RESEED, 0);
            DBCC CHECKIDENT ('Revisores', RESEED, 0);
            DBCC CHECKIDENT ('Contratistas', RESEED, 0);
            DBCC CHECKIDENT ('Proveedores', RESEED, 0);
            DBCC CHECKIDENT ('Construcciones', RESEED, 0);
            DBCC CHECKIDENT ('Conceptos', RESEED, 0);
            DBCC CHECKIDENT ('UnidadesMedida', RESEED, 0);

            SET IDENTITY_INSERT TiposUsuario ON;
            INSERT INTO TiposUsuario (Id, Nombre) VALUES
            (1, N'Administrador'),
            (2, N'Arquitecto'),
            (3, N'Contratista'),
            (4, N'Revisor');
            SET IDENTITY_INSERT TiposUsuario OFF;

            SET IDENTITY_INSERT Usuarios ON;
            INSERT INTO Usuarios (Id, Usuario, Nombre, PasswordHash, TipoUsuarioId, Activo, DebeCambiarContrasena)
            VALUES (1, N'admin', N'Administrador', N'AQAAAAIAAYagAAAAEL0ccqOmHJF693FpHgqXvF0RaUM3+mkM0eAUTDbLMZ7ItRQKN6V2h0RqsGcQ0ovuXg==', 1, 1, 1);
            SET IDENTITY_INSERT Usuarios OFF;

            SET IDENTITY_INSERT MenuOpciones ON;
            INSERT INTO MenuOpciones (Id, PadreId, Descripcion, Url, Icono, Orden) VALUES
            (17, NULL, N'Dashboard', N'/', N'layout-dashboard', 0),
            (1, NULL, N'Administración', NULL, N'settings', 1),
            (11, NULL, N'Actas', N'/actas', N'file-pen-line', 2),
            (13, NULL, N'Informes', NULL, N'chart-column', 3),
            (2, 1, N'Arquitectos', N'/admin/arquitectos', N'pencil-ruler', 1),
            (3, 1, N'Revisores', N'/admin/revisores', N'clipboard-check', 2),
            (4, 1, N'Contratistas', N'/admin/contratistas', N'hard-hat', 3),
            (5, 1, N'Proveedores', N'/admin/proveedores', N'truck', 4),
            (6, 1, N'Construcciones', N'/admin/construcciones', N'building-2', 5),
            (7, 1, N'Conceptos', N'/admin/conceptos', N'list-tree', 6),
            (8, 1, N'Insumos', N'/presupuesto/insumos', N'package', 7),
            (9, 1, N'Unidades de medida', N'/admin/unidades-medida', N'ruler', 8),
            (10, 1, N'Materiales por proveedor', N'/admin/proveedores-materiales', N'badge-dollar-sign', 9),
            (15, 1, N'Usuarios', N'/admin/usuarios', N'users', 10),
            (16, 1, N'Cambiar contraseña', N'/cuenta/contrasena', N'key-round', 11),
            (14, 13, N'Reporte de actas', N'/informes/actas', N'table', 1),
            (23, 13, N'Reporte por materiales', N'/informes/materiales', N'brick-wall', 2),
            (18, NULL, N'Presupuestos', NULL, N'wallet', 4),
            (19, 18, N'Presupuestos de obra', N'/presupuestos', N'file-spreadsheet', 1),
            (20, 18, N'Insumos', N'/presupuesto/insumos', N'package', 2),
            (21, 18, N'Mano de obra', N'/presupuesto/mano-obra', N'hammer', 3),
            (22, 18, N'Análisis de precios', N'/presupuesto/apu', N'calculator', 4);
            SET IDENTITY_INSERT MenuOpciones OFF;

            INSERT INTO MenuOpcionTipoUsuario (MenuOpcionId, TipoUsuarioId)
            SELECT Id, 1 FROM MenuOpciones;
            INSERT INTO MenuOpcionTipoUsuario (MenuOpcionId, TipoUsuarioId) VALUES
            (11, 2), (16, 2), (18, 2), (19, 2), (11, 3), (16, 3), (11, 4), (16, 4);

            DBCC CHECKIDENT ('TiposUsuario', RESEED);
            DBCC CHECKIDENT ('Usuarios', RESEED);
            DBCC CHECKIDENT ('MenuOpciones', RESEED);
            """;

        await context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
        context.ChangeTracker.Clear();
    }
}
