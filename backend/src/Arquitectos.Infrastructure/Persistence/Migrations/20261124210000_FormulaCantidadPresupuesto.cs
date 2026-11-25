using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arquitectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FormulaCantidadPresupuesto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FormulaCantidad",
                table: "PresupuestoItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE hijo
                SET FormulaCantidad = N'=[' + LTRIM(RTRIM(padre.Codigo)) + N']*' +
                    REPLACE(CONVERT(nvarchar(40), CASE WHEN hijo.CantidadOrigenFactor = 0 THEN 1 ELSE hijo.CantidadOrigenFactor END), ',', '.'),
                    CantidadOrigenItemId = NULL
                FROM PresupuestoItems hijo
                INNER JOIN PresupuestoItems padre ON padre.Id = hijo.CantidadOrigenItemId
                WHERE hijo.CantidadOrigenItemId IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FormulaCantidad",
                table: "PresupuestoItems");
        }
    }
}