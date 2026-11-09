using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arquitectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DashboardIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Actas_ContratistaId",
                table: "Actas");

            migrationBuilder.DropIndex(
                name: "IX_ActaDetalles_ConceptoId",
                table: "ActaDetalles");

            migrationBuilder.DropIndex(
                name: "IX_ActaDetalles_ConstruccionId",
                table: "ActaDetalles");

            migrationBuilder.InsertData(
                table: "MenuOpciones",
                columns: new[] { "Id", "Descripcion", "Icono", "Orden", "PadreId", "Url" },
                values: new object[] { 17, "Dashboard", "fa fa-tachometer", 0, null, "/" });

            migrationBuilder.InsertData(
                table: "MenuOpcionTipoUsuario",
                columns: new[] { "MenuOpcionId", "TipoUsuarioId" },
                values: new object[] { 17, 1 });

            migrationBuilder.CreateIndex(
                name: "IX_Actas_ContratistaId_FechaInicial_FechaFinal",
                table: "Actas",
                columns: new[] { "ContratistaId", "FechaInicial", "FechaFinal" });

            migrationBuilder.CreateIndex(
                name: "IX_Actas_FechaFinal_ContratistaId",
                table: "Actas",
                columns: new[] { "FechaFinal", "ContratistaId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActaDetalles_ConceptoId_UnidadMedidaId_MaterialId",
                table: "ActaDetalles",
                columns: new[] { "ConceptoId", "UnidadMedidaId", "MaterialId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActaDetalles_ConstruccionId_ConceptoId",
                table: "ActaDetalles",
                columns: new[] { "ConstruccionId", "ConceptoId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Actas_ContratistaId_FechaInicial_FechaFinal",
                table: "Actas");

            migrationBuilder.DropIndex(
                name: "IX_Actas_FechaFinal_ContratistaId",
                table: "Actas");

            migrationBuilder.DropIndex(
                name: "IX_ActaDetalles_ConceptoId_UnidadMedidaId_MaterialId",
                table: "ActaDetalles");

            migrationBuilder.DropIndex(
                name: "IX_ActaDetalles_ConstruccionId_ConceptoId",
                table: "ActaDetalles");

            migrationBuilder.DeleteData(
                table: "MenuOpcionTipoUsuario",
                keyColumns: new[] { "MenuOpcionId", "TipoUsuarioId" },
                keyValues: new object[] { 17, 1 });

            migrationBuilder.DeleteData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.CreateIndex(
                name: "IX_Actas_ContratistaId",
                table: "Actas",
                column: "ContratistaId");

            migrationBuilder.CreateIndex(
                name: "IX_ActaDetalles_ConceptoId",
                table: "ActaDetalles",
                column: "ConceptoId");

            migrationBuilder.CreateIndex(
                name: "IX_ActaDetalles_ConstruccionId",
                table: "ActaDetalles",
                column: "ConstruccionId");
        }
    }
}
