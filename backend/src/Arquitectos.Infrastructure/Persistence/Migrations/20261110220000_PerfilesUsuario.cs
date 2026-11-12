using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arquitectos.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ArquitectosDbContext))]
    [Migration("20261110220000_PerfilesUsuario")]
    public class PerfilesUsuario : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET ANSI_NULLS ON;
                SET QUOTED_IDENTIFIER ON;

                IF NOT EXISTS (SELECT 1 FROM TiposUsuario WHERE Id = 2)
                BEGIN
                    SET IDENTITY_INSERT TiposUsuario ON;
                    INSERT INTO TiposUsuario (Id, Nombre) VALUES
                        (2, N'Arquitecto'),
                        (3, N'Contratista'),
                        (4, N'Revisor');
                    SET IDENTITY_INSERT TiposUsuario OFF;
                END;

                IF COL_LENGTH('Usuarios', 'ArquitectoId') IS NULL
                    ALTER TABLE Usuarios ADD ArquitectoId int NULL;
                IF COL_LENGTH('Usuarios', 'ContratistaId') IS NULL
                    ALTER TABLE Usuarios ADD ContratistaId int NULL;
                IF COL_LENGTH('Usuarios', 'RevisorId') IS NULL
                    ALTER TABLE Usuarios ADD RevisorId int NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Usuarios_Arquitectos_ArquitectoId')
                    ALTER TABLE Usuarios ADD CONSTRAINT FK_Usuarios_Arquitectos_ArquitectoId
                        FOREIGN KEY (ArquitectoId) REFERENCES Arquitectos (Id);
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Usuarios_Contratistas_ContratistaId')
                    ALTER TABLE Usuarios ADD CONSTRAINT FK_Usuarios_Contratistas_ContratistaId
                        FOREIGN KEY (ContratistaId) REFERENCES Contratistas (Id);
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Usuarios_Revisores_RevisorId')
                    ALTER TABLE Usuarios ADD CONSTRAINT FK_Usuarios_Revisores_RevisorId
                        FOREIGN KEY (RevisorId) REFERENCES Revisores (Id);

                IF COL_LENGTH('ActaDetalles', 'TipoUsuarioId') IS NULL
                    ALTER TABLE ActaDetalles ADD TipoUsuarioId int NOT NULL
                        CONSTRAINT DF_ActaDetalles_TipoUsuario DEFAULT 2;

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ActaDetalles_TiposUsuario_TipoUsuarioId')
                    ALTER TABLE ActaDetalles ADD CONSTRAINT FK_ActaDetalles_TiposUsuario_TipoUsuarioId
                        FOREIGN KEY (TipoUsuarioId) REFERENCES TiposUsuario (Id);

                INSERT INTO MenuOpcionTipoUsuario (MenuOpcionId, TipoUsuarioId)
                SELECT origen.MenuOpcionId, origen.TipoUsuarioId
                FROM (VALUES (11, 2), (16, 2), (11, 3), (16, 3), (11, 4), (16, 4)) AS origen(MenuOpcionId, TipoUsuarioId)
                WHERE NOT EXISTS (
                    SELECT 1 FROM MenuOpcionTipoUsuario actual
                    WHERE actual.MenuOpcionId = origen.MenuOpcionId AND actual.TipoUsuarioId = origen.TipoUsuarioId);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
