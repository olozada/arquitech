using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arquitectos.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ArquitectosDbContext))]
    [Migration("20261111020000_CedulaCatalogo")]
    public class CedulaCatalogo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('Arquitectos', 'Cedula') IS NULL
                    ALTER TABLE Arquitectos ADD Cedula nvarchar(20) NULL;
                IF COL_LENGTH('Revisores', 'Cedula') IS NULL
                    ALTER TABLE Revisores ADD Cedula nvarchar(20) NULL;
                IF COL_LENGTH('Contratistas', 'Cedula') IS NULL
                    ALTER TABLE Contratistas ADD Cedula nvarchar(20) NULL;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Arquitectos_Cedula' AND object_id = OBJECT_ID('dbo.Arquitectos'))
                    EXEC(N'CREATE UNIQUE INDEX IX_Arquitectos_Cedula ON dbo.Arquitectos(Cedula) WHERE Cedula IS NOT NULL;');
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Revisores_Cedula' AND object_id = OBJECT_ID('dbo.Revisores'))
                    EXEC(N'CREATE UNIQUE INDEX IX_Revisores_Cedula ON dbo.Revisores(Cedula) WHERE Cedula IS NOT NULL;');
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Contratistas_Cedula' AND object_id = OBJECT_ID('dbo.Contratistas'))
                    EXEC(N'CREATE UNIQUE INDEX IX_Contratistas_Cedula ON dbo.Contratistas(Cedula) WHERE Cedula IS NOT NULL;');
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS IX_Arquitectos_Cedula ON dbo.Arquitectos;
                DROP INDEX IF EXISTS IX_Revisores_Cedula ON dbo.Revisores;
                DROP INDEX IF EXISTS IX_Contratistas_Cedula ON dbo.Contratistas;
                IF COL_LENGTH('Arquitectos', 'Cedula') IS NOT NULL ALTER TABLE Arquitectos DROP COLUMN Cedula;
                IF COL_LENGTH('Revisores', 'Cedula') IS NOT NULL ALTER TABLE Revisores DROP COLUMN Cedula;
                IF COL_LENGTH('Contratistas', 'Cedula') IS NOT NULL ALTER TABLE Contratistas DROP COLUMN Cedula;
                """);
        }
    }
}
