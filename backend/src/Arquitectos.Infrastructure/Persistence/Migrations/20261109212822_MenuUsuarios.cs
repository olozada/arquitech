using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Arquitectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MenuUsuarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "MenuOpciones",
                columns: new[] { "Id", "Descripcion", "Icono", "Orden", "PadreId", "Url" },
                values: new object[,]
                {
                    { 15, "Usuarios", "fa fa-users", 10, 1, "/admin/usuarios" },
                    { 16, "Cambiar contraseña", "fa fa-key", 11, 1, "/cuenta/contrasena" }
                });

            migrationBuilder.InsertData(
                table: "MenuOpcionTipoUsuario",
                columns: new[] { "MenuOpcionId", "TipoUsuarioId" },
                values: new object[,]
                {
                    { 15, 1 },
                    { 16, 1 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "MenuOpcionTipoUsuario",
                keyColumns: new[] { "MenuOpcionId", "TipoUsuarioId" },
                keyValues: new object[] { 15, 1 });

            migrationBuilder.DeleteData(
                table: "MenuOpcionTipoUsuario",
                keyColumns: new[] { "MenuOpcionId", "TipoUsuarioId" },
                keyValues: new object[] { 16, 1 });

            migrationBuilder.DeleteData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 16);
        }
    }
}
