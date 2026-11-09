using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Arquitectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Arquitectos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Arquitectos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Conceptos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Unidad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conceptos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Construcciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Propietario = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Construcciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Contratistas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contratistas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Materiales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Unidad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materiales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuOpciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PadreId = table.Column<int>(type: "int", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Icono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Orden = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuOpciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuOpciones_MenuOpciones_PadreId",
                        column: x => x.PadreId,
                        principalTable: "MenuOpciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Proveedores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Revisores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Revisores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposUsuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposUsuario", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnidadesMedida",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnidadesMedida", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProveedorMateriales",
                columns: table => new
                {
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: false),
                    Precio = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProveedorMateriales", x => new { x.ProveedorId, x.MaterialId });
                    table.CheckConstraint("CK_ProveedorMateriales_Precio", "[Precio] > 0");
                    table.ForeignKey(
                        name: "FK_ProveedorMateriales_Materiales_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materiales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProveedorMateriales_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MenuOpcionTipoUsuario",
                columns: table => new
                {
                    MenuOpcionId = table.Column<int>(type: "int", nullable: false),
                    TipoUsuarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuOpcionTipoUsuario", x => new { x.MenuOpcionId, x.TipoUsuarioId });
                    table.ForeignKey(
                        name: "FK_MenuOpcionTipoUsuario_MenuOpciones_MenuOpcionId",
                        column: x => x.MenuOpcionId,
                        principalTable: "MenuOpciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MenuOpcionTipoUsuario_TiposUsuario_TipoUsuarioId",
                        column: x => x.TipoUsuarioId,
                        principalTable: "TiposUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Usuario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TipoUsuarioId = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_TiposUsuario_TipoUsuarioId",
                        column: x => x.TipoUsuarioId,
                        principalTable: "TiposUsuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Actas",
                columns: table => new
                {
                    NumeroActa = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaInicial = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaFinal = table.Column<DateOnly>(type: "date", nullable: false),
                    ArquitectoId = table.Column<int>(type: "int", nullable: false),
                    RevisorId = table.Column<int>(type: "int", nullable: false),
                    ContratistaId = table.Column<int>(type: "int", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UsuarioRegistroId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actas", x => x.NumeroActa);
                    table.CheckConstraint("CK_Actas_Fechas", "[FechaFinal] >= [FechaInicial]");
                    table.ForeignKey(
                        name: "FK_Actas_Arquitectos_ArquitectoId",
                        column: x => x.ArquitectoId,
                        principalTable: "Arquitectos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Actas_Contratistas_ContratistaId",
                        column: x => x.ContratistaId,
                        principalTable: "Contratistas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Actas_Revisores_RevisorId",
                        column: x => x.RevisorId,
                        principalTable: "Revisores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Actas_Usuarios_UsuarioRegistroId",
                        column: x => x.UsuarioRegistroId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ActaDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NumeroActa = table.Column<int>(type: "int", nullable: false),
                    ConstruccionId = table.Column<int>(type: "int", nullable: false),
                    ConceptoId = table.Column<int>(type: "int", nullable: false),
                    UnidadMedidaId = table.Column<int>(type: "int", nullable: false),
                    MaterialId = table.Column<int>(type: "int", nullable: true),
                    Cantidad = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Precio = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(37,6)", precision: 37, scale: 6, nullable: false, computedColumnSql: "[Cantidad] * [Precio]", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActaDetalles", x => x.Id);
                    table.CheckConstraint("CK_ActaDetalles_Cantidad", "[Cantidad] > 0");
                    table.CheckConstraint("CK_ActaDetalles_Precio", "[Precio] >= 0");
                    table.ForeignKey(
                        name: "FK_ActaDetalles_Actas_NumeroActa",
                        column: x => x.NumeroActa,
                        principalTable: "Actas",
                        principalColumn: "NumeroActa",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActaDetalles_Conceptos_ConceptoId",
                        column: x => x.ConceptoId,
                        principalTable: "Conceptos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActaDetalles_Construcciones_ConstruccionId",
                        column: x => x.ConstruccionId,
                        principalTable: "Construcciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActaDetalles_Materiales_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materiales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActaDetalles_UnidadesMedida_UnidadMedidaId",
                        column: x => x.UnidadMedidaId,
                        principalTable: "UnidadesMedida",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "MenuOpciones",
                columns: new[] { "Id", "Descripcion", "Icono", "Orden", "PadreId", "Url" },
                values: new object[,]
                {
                    { 1, "Administración", "fa fa-cogs", 1, null, null },
                    { 11, "Actas", "fa fa-file-text", 2, null, null },
                    { 13, "Informes", "fa fa-bar-chart", 3, null, null }
                });

            migrationBuilder.InsertData(
                table: "TiposUsuario",
                columns: new[] { "Id", "Nombre" },
                values: new object[] { 1, "Administrador" });

            migrationBuilder.InsertData(
                table: "MenuOpcionTipoUsuario",
                columns: new[] { "MenuOpcionId", "TipoUsuarioId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 11, 1 },
                    { 13, 1 }
                });

            migrationBuilder.InsertData(
                table: "MenuOpciones",
                columns: new[] { "Id", "Descripcion", "Icono", "Orden", "PadreId", "Url" },
                values: new object[,]
                {
                    { 2, "Arquitectos", "fa fa-user", 1, 1, "/admin/arquitectos" },
                    { 3, "Revisores", "fa fa-eye", 2, 1, "/admin/revisores" },
                    { 4, "Contratistas", "fa fa-briefcase", 3, 1, "/admin/contratistas" },
                    { 5, "Proveedores", "fa fa-truck", 4, 1, "/admin/proveedores" },
                    { 6, "Construcciones", "fa fa-building", 5, 1, "/admin/construcciones" },
                    { 7, "Conceptos", "fa fa-list", 6, 1, "/admin/conceptos" },
                    { 8, "Materiales", "fa fa-cubes", 7, 1, "/admin/materiales" },
                    { 9, "Unidades de medida", "fa fa-balance-scale", 8, 1, "/admin/unidades-medida" },
                    { 10, "Materiales por proveedor", "fa fa-tags", 9, 1, "/admin/proveedores-materiales" },
                    { 12, "Toma de actas", "fa fa-pencil", 1, 11, "/actas/nueva" },
                    { 14, "Reporte de actas", "fa fa-table", 1, 13, "/informes/actas" }
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "Activo", "Nombre", "Usuario", "PasswordHash", "TipoUsuarioId" },
                values: new object[] { 1, true, "Administrador", "admin", "AQAAAAIAAYagAAAAEL0ccqOmHJF693FpHgqXvF0RaUM3+mkM0eAUTDbLMZ7ItRQKN6V2h0RqsGcQ0ovuXg==", 1 });

            migrationBuilder.InsertData(
                table: "MenuOpcionTipoUsuario",
                columns: new[] { "MenuOpcionId", "TipoUsuarioId" },
                values: new object[,]
                {
                    { 2, 1 },
                    { 3, 1 },
                    { 4, 1 },
                    { 5, 1 },
                    { 6, 1 },
                    { 7, 1 },
                    { 8, 1 },
                    { 9, 1 },
                    { 10, 1 },
                    { 12, 1 },
                    { 14, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActaDetalles_ConceptoId",
                table: "ActaDetalles",
                column: "ConceptoId");

            migrationBuilder.CreateIndex(
                name: "IX_ActaDetalles_ConstruccionId",
                table: "ActaDetalles",
                column: "ConstruccionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActaDetalles_MaterialId",
                table: "ActaDetalles",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_ActaDetalles_NumeroActa",
                table: "ActaDetalles",
                column: "NumeroActa");

            migrationBuilder.CreateIndex(
                name: "IX_ActaDetalles_UnidadMedidaId",
                table: "ActaDetalles",
                column: "UnidadMedidaId");

            migrationBuilder.CreateIndex(
                name: "IX_Actas_ArquitectoId",
                table: "Actas",
                column: "ArquitectoId");

            migrationBuilder.CreateIndex(
                name: "IX_Actas_ContratistaId",
                table: "Actas",
                column: "ContratistaId");

            migrationBuilder.CreateIndex(
                name: "IX_Actas_Fecha_Arq",
                table: "Actas",
                columns: new[] { "FechaInicial", "FechaFinal", "ArquitectoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Actas_RevisorId",
                table: "Actas",
                column: "RevisorId");

            migrationBuilder.CreateIndex(
                name: "IX_Actas_UsuarioRegistroId",
                table: "Actas",
                column: "UsuarioRegistroId");

            migrationBuilder.CreateIndex(
                name: "IX_Arquitectos_Nombre",
                table: "Arquitectos",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conceptos_Descripcion",
                table: "Conceptos",
                column: "Descripcion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Construcciones_Descripcion",
                table: "Construcciones",
                column: "Descripcion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contratistas_Nombre",
                table: "Contratistas",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Materiales_Descripcion",
                table: "Materiales",
                column: "Descripcion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuOpciones_PadreId",
                table: "MenuOpciones",
                column: "PadreId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuOpcionTipoUsuario_TipoUsuarioId",
                table: "MenuOpcionTipoUsuario",
                column: "TipoUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_Nombre",
                table: "Proveedores",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProveedorMateriales_MaterialId",
                table: "ProveedorMateriales",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_Revisores_Nombre",
                table: "Revisores",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TiposUsuario_Nombre",
                table: "TiposUsuario",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnidadesMedida_Nombre",
                table: "UnidadesMedida",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_TipoUsuarioId",
                table: "Usuarios",
                column: "TipoUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Usuario",
                table: "Usuarios",
                column: "Usuario",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActaDetalles");

            migrationBuilder.DropTable(
                name: "MenuOpcionTipoUsuario");

            migrationBuilder.DropTable(
                name: "ProveedorMateriales");

            migrationBuilder.DropTable(
                name: "Actas");

            migrationBuilder.DropTable(
                name: "Conceptos");

            migrationBuilder.DropTable(
                name: "Construcciones");

            migrationBuilder.DropTable(
                name: "UnidadesMedida");

            migrationBuilder.DropTable(
                name: "MenuOpciones");

            migrationBuilder.DropTable(
                name: "Materiales");

            migrationBuilder.DropTable(
                name: "Proveedores");

            migrationBuilder.DropTable(
                name: "Arquitectos");

            migrationBuilder.DropTable(
                name: "Contratistas");

            migrationBuilder.DropTable(
                name: "Revisores");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "TiposUsuario");
        }
    }
}
