using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arquitectos.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ArquitectosDbContext))]
    [Migration("20261110233000_RevisionActa")]
    public class RevisionActa : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET ANSI_NULLS ON;
                SET QUOTED_IDENTIFIER ON;

                IF COL_LENGTH('ActaDetalles', 'Aprobado') IS NULL
                    ALTER TABLE ActaDetalles ADD Aprobado bit NOT NULL
                        CONSTRAINT DF_ActaDetalles_Aprobado DEFAULT 1;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('ActaDetalles', 'Aprobado') IS NOT NULL
                BEGIN
                    ALTER TABLE ActaDetalles DROP CONSTRAINT DF_ActaDetalles_Aprobado;
                    ALTER TABLE ActaDetalles DROP COLUMN Aprobado;
                END
                """);
        }
    }
}
