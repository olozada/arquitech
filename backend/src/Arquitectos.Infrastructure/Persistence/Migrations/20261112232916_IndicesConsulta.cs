using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arquitectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IndicesConsulta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActaDetalles_NumeroActa",
                table: "ActaDetalles");

            migrationBuilder.DropIndex(
                name: "IX_ActaDetalles_PresupuestoItemId",
                table: "ActaDetalles");

            migrationBuilder.CreateIndex(
                name: "IX_ActaDetalles_NumeroActa_InsumoId",
                table: "ActaDetalles",
                columns: new[] { "NumeroActa", "InsumoId" });

            migrationBuilder.CreateIndex(
                name: "IX_ActaDetalles_PresupuestoItemId_Aprobado",
                table: "ActaDetalles",
                columns: new[] { "PresupuestoItemId", "Aprobado" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ActaDetalles_NumeroActa_InsumoId",
                table: "ActaDetalles");

            migrationBuilder.DropIndex(
                name: "IX_ActaDetalles_PresupuestoItemId_Aprobado",
                table: "ActaDetalles");

            migrationBuilder.CreateIndex(
                name: "IX_ActaDetalles_NumeroActa",
                table: "ActaDetalles",
                column: "NumeroActa");

            migrationBuilder.CreateIndex(
                name: "IX_ActaDetalles_PresupuestoItemId",
                table: "ActaDetalles",
                column: "PresupuestoItemId");
        }
    }
}
