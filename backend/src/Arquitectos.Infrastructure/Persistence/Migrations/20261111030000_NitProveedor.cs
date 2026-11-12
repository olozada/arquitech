using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arquitectos.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ArquitectosDbContext))]
    [Migration("20261111030000_NitProveedor")]
    public class NitProveedor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('Proveedores', 'Nit') IS NULL
                    ALTER TABLE Proveedores ADD Nit nvarchar(20) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Proveedores_Nit' AND object_id = OBJECT_ID('dbo.Proveedores'))
                    EXEC(N'CREATE UNIQUE INDEX IX_Proveedores_Nit ON dbo.Proveedores(Nit) WHERE Nit IS NOT NULL;');
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS IX_Proveedores_Nit ON dbo.Proveedores;
                IF COL_LENGTH('Proveedores', 'Nit') IS NOT NULL ALTER TABLE Proveedores DROP COLUMN Nit;
                """);
        }
    }
}
