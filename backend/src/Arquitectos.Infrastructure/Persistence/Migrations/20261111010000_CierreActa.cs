using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arquitectos.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ArquitectosDbContext))]
    [Migration("20261111010000_CierreActa")]
    public class CierreActa : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('Actas', 'Cerrada') IS NULL
                    ALTER TABLE Actas ADD Cerrada bit NOT NULL
                        CONSTRAINT DF_Actas_Cerrada DEFAULT 0;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF COL_LENGTH('Actas', 'Cerrada') IS NOT NULL
                BEGIN
                    ALTER TABLE Actas DROP CONSTRAINT DF_Actas_Cerrada;
                    ALTER TABLE Actas DROP COLUMN Cerrada;
                END
                """);
        }
    }
}
