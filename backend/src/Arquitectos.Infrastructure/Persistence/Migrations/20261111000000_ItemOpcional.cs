using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arquitectos.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ArquitectosDbContext))]
    [Migration("20261111000000_ItemOpcional")]
    public class ItemOpcional : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET ANSI_NULLS ON;
                SET QUOTED_IDENTIFIER ON;

                IF EXISTS (
                    SELECT 1
                    FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'ActaDetalles')
                      AND name = N'UnidadMedidaId'
                      AND is_nullable = 0)
                    ALTER TABLE ActaDetalles ALTER COLUMN UnidadMedidaId int NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET ANSI_NULLS ON;
                SET QUOTED_IDENTIFIER ON;

                UPDATE ActaDetalles SET UnidadMedidaId = 0 WHERE UnidadMedidaId IS NULL;
                ALTER TABLE ActaDetalles ALTER COLUMN UnidadMedidaId int NOT NULL;
                """);
        }
    }
}
