using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Arquitectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FacturasMateriales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Facturas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    OrdenCompra = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    ModificadaEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UsuarioRegistroId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facturas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Facturas_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Facturas_Usuarios_UsuarioRegistroId",
                        column: x => x.UsuarioRegistroId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FacturaDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FacturaId = table.Column<int>(type: "int", nullable: false),
                    ConstruccionId = table.Column<int>(type: "int", nullable: false),
                    InsumoId = table.Column<int>(type: "int", nullable: false),
                    UnidadMedidaId = table.Column<int>(type: "int", nullable: true),
                    Cantidad = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Precio = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(37,6)", precision: 37, scale: 6, nullable: false, computedColumnSql: "[Cantidad] * [Precio]", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturaDetalles", x => x.Id);
                    table.CheckConstraint("CK_FacturaDetalles_Cantidad", "[Cantidad] > 0");
                    table.CheckConstraint("CK_FacturaDetalles_Precio", "[Precio] >= 0");
                    table.ForeignKey(
                        name: "FK_FacturaDetalles_Construcciones_ConstruccionId",
                        column: x => x.ConstruccionId,
                        principalTable: "Construcciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FacturaDetalles_Facturas_FacturaId",
                        column: x => x.FacturaId,
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FacturaDetalles_Insumos_InsumoId",
                        column: x => x.InsumoId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FacturaDetalles_UnidadesMedida_UnidadMedidaId",
                        column: x => x.UnidadMedidaId,
                        principalTable: "UnidadesMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 13,
                column: "Orden",
                value: 4);

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 18,
                column: "Orden",
                value: 5);

            migrationBuilder.InsertData(
                table: "MenuOpciones",
                columns: new[] { "Id", "Descripcion", "Icono", "Orden", "PadreId", "Url" },
                values: new object[,]
                {
                    { 24, "Facturas", "receipt", 3, null, "/facturas" },
                    { 25, "Reporte de facturas", "receipt", 3, 13, "/informes/facturas" }
                });

            migrationBuilder.InsertData(
                table: "MenuOpcionTipoUsuario",
                columns: new[] { "MenuOpcionId", "TipoUsuarioId" },
                values: new object[,]
                {
                    { 24, 1 },
                    { 24, 2 },
                    { 24, 3 },
                    { 24, 4 },
                    { 25, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_FacturaDetalles_ConstruccionId_InsumoId",
                table: "FacturaDetalles",
                columns: new[] { "ConstruccionId", "InsumoId" });

            migrationBuilder.CreateIndex(
                name: "IX_FacturaDetalles_FacturaId",
                table: "FacturaDetalles",
                column: "FacturaId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaDetalles_InsumoId",
                table: "FacturaDetalles",
                column: "InsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_FacturaDetalles_UnidadMedidaId",
                table: "FacturaDetalles",
                column: "UnidadMedidaId");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_Fecha_Id",
                table: "Facturas",
                columns: new[] { "Fecha", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_Proveedor_Numero",
                table: "Facturas",
                columns: new[] { "ProveedorId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_UsuarioRegistroId",
                table: "Facturas",
                column: "UsuarioRegistroId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FacturaDetalles");

            migrationBuilder.DropTable(
                name: "Facturas");

            migrationBuilder.DeleteData(
                table: "MenuOpcionTipoUsuario",
                keyColumns: new[] { "MenuOpcionId", "TipoUsuarioId" },
                keyValues: new object[] { 24, 1 });

            migrationBuilder.DeleteData(
                table: "MenuOpcionTipoUsuario",
                keyColumns: new[] { "MenuOpcionId", "TipoUsuarioId" },
                keyValues: new object[] { 24, 2 });

            migrationBuilder.DeleteData(
                table: "MenuOpcionTipoUsuario",
                keyColumns: new[] { "MenuOpcionId", "TipoUsuarioId" },
                keyValues: new object[] { 24, 3 });

            migrationBuilder.DeleteData(
                table: "MenuOpcionTipoUsuario",
                keyColumns: new[] { "MenuOpcionId", "TipoUsuarioId" },
                keyValues: new object[] { 24, 4 });

            migrationBuilder.DeleteData(
                table: "MenuOpcionTipoUsuario",
                keyColumns: new[] { "MenuOpcionId", "TipoUsuarioId" },
                keyValues: new object[] { 25, 1 });

            migrationBuilder.DeleteData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 13,
                column: "Orden",
                value: 3);

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 18,
                column: "Orden",
                value: 4);
        }
    }
}
