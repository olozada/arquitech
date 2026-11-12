using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arquitectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IconosLucide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 1,
                column: "Icono",
                value: "settings");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 2,
                column: "Icono",
                value: "pencil-ruler");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 3,
                column: "Icono",
                value: "clipboard-check");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 4,
                column: "Icono",
                value: "hard-hat");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 5,
                column: "Icono",
                value: "truck");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 6,
                column: "Icono",
                value: "building-2");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 7,
                column: "Icono",
                value: "list-tree");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 8,
                column: "Icono",
                value: "package");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 9,
                column: "Icono",
                value: "ruler");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 10,
                column: "Icono",
                value: "badge-dollar-sign");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 11,
                column: "Icono",
                value: "file-pen-line");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 13,
                column: "Icono",
                value: "chart-column");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 14,
                column: "Icono",
                value: "table");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 15,
                column: "Icono",
                value: "users");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 16,
                column: "Icono",
                value: "key-round");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 17,
                column: "Icono",
                value: "layout-dashboard");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 18,
                column: "Icono",
                value: "wallet");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 19,
                column: "Icono",
                value: "file-spreadsheet");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 20,
                column: "Icono",
                value: "package");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 21,
                column: "Icono",
                value: "hammer");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 22,
                column: "Icono",
                value: "calculator");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 23,
                column: "Icono",
                value: "brick-wall");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 1,
                column: "Icono",
                value: "fa fa-cogs");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 2,
                column: "Icono",
                value: "fa fa-user");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 3,
                column: "Icono",
                value: "fa fa-eye");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 4,
                column: "Icono",
                value: "fa fa-briefcase");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 5,
                column: "Icono",
                value: "fa fa-truck");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 6,
                column: "Icono",
                value: "fa fa-building");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 7,
                column: "Icono",
                value: "fa fa-list");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 8,
                column: "Icono",
                value: "fa fa-cubes");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 9,
                column: "Icono",
                value: "fa fa-balance-scale");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 10,
                column: "Icono",
                value: "fa fa-tags");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 11,
                column: "Icono",
                value: "fa fa-file-text");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 13,
                column: "Icono",
                value: "fa fa-bar-chart");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 14,
                column: "Icono",
                value: "fa fa-table");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 15,
                column: "Icono",
                value: "fa fa-users");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 16,
                column: "Icono",
                value: "fa fa-key");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 17,
                column: "Icono",
                value: "fa fa-tachometer");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 18,
                column: "Icono",
                value: "fa fa-calculator");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 19,
                column: "Icono",
                value: "fa fa-file-text-o");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 20,
                column: "Icono",
                value: "fa fa-cubes");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 21,
                column: "Icono",
                value: "fa fa-users");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 22,
                column: "Icono",
                value: "fa fa-list-alt");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 23,
                column: "Icono",
                value: "fa fa-cubes");
        }
    }
}
