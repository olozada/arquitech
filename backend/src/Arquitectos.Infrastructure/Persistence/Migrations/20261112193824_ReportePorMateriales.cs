using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arquitectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReportePorMateriales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MenuOpciones",
                columns: new[] { "Id", "Descripcion", "Icono", "Orden", "PadreId", "Url" },
                values: new object[] { 23, "Reporte por materiales", "fa fa-cubes", 2, 13, "/informes/materiales" });

            migrationBuilder.InsertData(
                table: "MenuOpcionTipoUsuario",
                columns: new[] { "MenuOpcionId", "TipoUsuarioId" },
                values: new object[] { 23, 1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MenuOpcionTipoUsuario",
                keyColumns: new[] { "MenuOpcionId", "TipoUsuarioId" },
                keyValues: new object[] { 23, 1 });

            migrationBuilder.DeleteData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 23);
        }
    }
}
