using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Arquitectos.Infrastructure.Persistence;

#nullable disable

namespace Arquitectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ArquitectosDbContext))]
    [Migration("20261125010000_ReporteConceptos")]
    public class ReporteConceptos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 25,
                column: "Orden",
                value: 4);

            migrationBuilder.InsertData(
                table: "MenuOpciones",
                columns: new[] { "Id", "Descripcion", "Icono", "Orden", "PadreId", "Url" },
                values: new object[] { 26, "Reporte por conceptos", "list-tree", 3, 13, "/informes/conceptos" });

            migrationBuilder.InsertData(
                table: "MenuOpcionTipoUsuario",
                columns: new[] { "MenuOpcionId", "TipoUsuarioId" },
                values: new object[] { 26, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MenuOpcionTipoUsuario",
                keyColumns: new[] { "MenuOpcionId", "TipoUsuarioId" },
                keyValues: new object[] { 26, 1 });

            migrationBuilder.DeleteData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 25,
                column: "Orden",
                value: 3);
        }
    }
}
