using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arquitectos.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ArquitectosDbContext))]
    [Migration("20261111200000_PresupuestoCreacionWeb")]
    public class PresupuestoCreacionWeb : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET QUOTED_IDENTIFIER ON;
                SET ANSI_NULLS ON;

                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Presupuestos_Aprobado' AND object_id = OBJECT_ID('dbo.Presupuestos'))
                    DROP INDEX IX_Presupuestos_Aprobado ON dbo.Presupuestos;

                ALTER TABLE dbo.Presupuestos ALTER COLUMN ConstruccionId int NULL;

                IF COL_LENGTH('dbo.Presupuestos', 'EsPlantilla') IS NULL
                    ALTER TABLE dbo.Presupuestos ADD EsPlantilla bit NOT NULL CONSTRAINT DF_Presupuestos_EsPlantilla DEFAULT 0;
                IF COL_LENGTH('dbo.Presupuestos', 'PresupuestoOrigenId') IS NULL
                    ALTER TABLE dbo.Presupuestos ADD PresupuestoOrigenId int NULL;
                """);

            migrationBuilder.Sql(
                """
                SET QUOTED_IDENTIFIER ON;
                SET ANSI_NULLS ON;

                IF COL_LENGTH('dbo.Presupuestos', 'RowVersion') IS NULL
                    ALTER TABLE dbo.Presupuestos ADD RowVersion rowversion NOT NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Presupuestos_Plantilla')
                    ALTER TABLE dbo.Presupuestos ADD CONSTRAINT CK_Presupuestos_Plantilla CHECK (EsPlantilla = 1 OR ConstruccionId IS NOT NULL);
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Presupuestos_Presupuestos_PresupuestoOrigenId')
                    ALTER TABLE dbo.Presupuestos ADD CONSTRAINT FK_Presupuestos_Presupuestos_PresupuestoOrigenId
                        FOREIGN KEY (PresupuestoOrigenId) REFERENCES dbo.Presupuestos (Id);
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Presupuestos_Aprobado' AND object_id = OBJECT_ID('dbo.Presupuestos'))
                    EXEC(N'CREATE UNIQUE INDEX IX_Presupuestos_Aprobado ON dbo.Presupuestos (ConstruccionId) WHERE [Estado] = 2 AND [EsPlantilla] = 0;');
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS IX_Presupuestos_Aprobado ON dbo.Presupuestos;
                ALTER TABLE dbo.Presupuestos DROP CONSTRAINT IF EXISTS FK_Presupuestos_Presupuestos_PresupuestoOrigenId;
                ALTER TABLE dbo.Presupuestos DROP CONSTRAINT IF EXISTS CK_Presupuestos_Plantilla;
                ALTER TABLE dbo.Presupuestos DROP CONSTRAINT IF EXISTS DF_Presupuestos_EsPlantilla;
                ALTER TABLE dbo.Presupuestos DROP COLUMN IF EXISTS RowVersion;
                ALTER TABLE dbo.Presupuestos DROP COLUMN IF EXISTS PresupuestoOrigenId;
                ALTER TABLE dbo.Presupuestos DROP COLUMN IF EXISTS EsPlantilla;
                DELETE FROM dbo.Presupuestos WHERE ConstruccionId IS NULL;
                ALTER TABLE dbo.Presupuestos ALTER COLUMN ConstruccionId int NOT NULL;
                EXEC(N'CREATE UNIQUE INDEX IX_Presupuestos_Aprobado ON dbo.Presupuestos (ConstruccionId) WHERE [Estado] = 2;');
                """);
        }
    }
}
