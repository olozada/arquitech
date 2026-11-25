using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arquitectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConceptoUnidadMedidaCatalogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UnidadMedidaId",
                table: "Conceptos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conceptos_UnidadMedidaId",
                table: "Conceptos",
                column: "UnidadMedidaId");

            migrationBuilder.AddForeignKey(
                name: "FK_Conceptos_UnidadesMedida_UnidadMedidaId",
                table: "Conceptos",
                column: "UnidadMedidaId",
                principalTable: "UnidadesMedida",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Conceptos_UnidadesMedida_UnidadMedidaId",
                table: "Conceptos");

            migrationBuilder.DropIndex(
                name: "IX_Conceptos_UnidadMedidaId",
                table: "Conceptos");

            migrationBuilder.DropColumn(
                name: "UnidadMedidaId",
                table: "Conceptos");
        }
    }
}
