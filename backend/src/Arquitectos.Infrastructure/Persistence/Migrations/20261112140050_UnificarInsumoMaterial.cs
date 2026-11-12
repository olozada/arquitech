using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arquitectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UnificarInsumoMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'dbo.Materiales', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Insumos', N'MaterialId') IS NOT NULL
                BEGIN
                    INSERT INTO dbo.Insumos (Codigo, Descripcion, Tipo, UnidadCompra, PrecioBase, PorcentajeIva, CantidadPorUnidad, UnidadUso, PrecioUnitario, MaterialId, FechaPrecio, Activo)
                    SELECT
                        CASE WHEN m.Codigo IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.Insumos i WHERE i.Codigo = m.Codigo) THEN NULL ELSE m.Codigo END,
                        LEFT(m.Descripcion, 500),
                        1,
                        ISNULL(m.Unidad, N''),
                        0, 0, 1,
                        ISNULL(m.Unidad, N''),
                        0,
                        m.Id,
                        SYSUTCDATETIME(),
                        m.Activo
                    FROM dbo.Materiales m
                    WHERE NOT EXISTS (SELECT 1 FROM dbo.Insumos i WHERE i.MaterialId = m.Id);

                    IF COL_LENGTH(N'dbo.ActaDetalles', N'InsumoNuevo') IS NULL
                        ALTER TABLE dbo.ActaDetalles ADD InsumoNuevo int NULL;
                    EXEC(N'UPDATE d SET InsumoNuevo = i.Id FROM dbo.ActaDetalles d INNER JOIN dbo.Insumos i ON i.MaterialId = d.MaterialId WHERE d.MaterialId IS NOT NULL;');

                    IF COL_LENGTH(N'dbo.ProveedorMateriales', N'InsumoNuevo') IS NULL
                        ALTER TABLE dbo.ProveedorMateriales ADD InsumoNuevo int NULL;
                    EXEC(N'UPDATE pm SET InsumoNuevo = i.Id FROM dbo.ProveedorMateriales pm INNER JOIN dbo.Insumos i ON i.MaterialId = pm.MaterialId;');
                END
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_ActaDetalles_Materiales_MaterialId",
                table: "ActaDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_Insumos_Materiales_MaterialId",
                table: "Insumos");

            migrationBuilder.DropForeignKey(
                name: "FK_ProveedorMateriales_Materiales_MaterialId",
                table: "ProveedorMateriales");

            migrationBuilder.DropTable(
                name: "Materiales");

            migrationBuilder.DropIndex(
                name: "IX_Insumos_MaterialId",
                table: "Insumos");

            migrationBuilder.DropColumn(
                name: "MaterialId",
                table: "Insumos");

            migrationBuilder.RenameColumn(
                name: "MaterialId",
                table: "ProveedorMateriales",
                newName: "InsumoId");

            migrationBuilder.RenameIndex(
                name: "IX_ProveedorMateriales_MaterialId",
                table: "ProveedorMateriales",
                newName: "IX_ProveedorMateriales_InsumoId");

            migrationBuilder.RenameColumn(
                name: "MaterialId",
                table: "ActaDetalles",
                newName: "InsumoId");

            migrationBuilder.RenameIndex(
                name: "IX_ActaDetalles_MaterialId",
                table: "ActaDetalles",
                newName: "IX_ActaDetalles_InsumoId");

            migrationBuilder.RenameIndex(
                name: "IX_ActaDetalles_ConceptoId_UnidadMedidaId_MaterialId",
                table: "ActaDetalles",
                newName: "IX_ActaDetalles_ConceptoId_UnidadMedidaId_InsumoId");

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Descripcion", "Url" },
                values: new object[] { "Insumos", "/presupuesto/insumos" });

            migrationBuilder.Sql("""
                IF COL_LENGTH(N'dbo.ActaDetalles', N'InsumoNuevo') IS NOT NULL
                BEGIN
                    UPDATE dbo.ActaDetalles SET InsumoId = InsumoNuevo;
                    ALTER TABLE dbo.ActaDetalles DROP COLUMN InsumoNuevo;
                END
                IF COL_LENGTH(N'dbo.ProveedorMateriales', N'InsumoNuevo') IS NOT NULL
                BEGIN
                    UPDATE dbo.ProveedorMateriales SET InsumoId = InsumoNuevo WHERE InsumoNuevo IS NOT NULL;
                    ALTER TABLE dbo.ProveedorMateriales DROP COLUMN InsumoNuevo;
                END
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_ActaDetalles_Insumos_InsumoId",
                table: "ActaDetalles",
                column: "InsumoId",
                principalTable: "Insumos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProveedorMateriales_Insumos_InsumoId",
                table: "ProveedorMateriales",
                column: "InsumoId",
                principalTable: "Insumos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActaDetalles_Insumos_InsumoId",
                table: "ActaDetalles");

            migrationBuilder.DropForeignKey(
                name: "FK_ProveedorMateriales_Insumos_InsumoId",
                table: "ProveedorMateriales");

            migrationBuilder.RenameColumn(
                name: "InsumoId",
                table: "ProveedorMateriales",
                newName: "MaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_ProveedorMateriales_InsumoId",
                table: "ProveedorMateriales",
                newName: "IX_ProveedorMateriales_MaterialId");

            migrationBuilder.RenameColumn(
                name: "InsumoId",
                table: "ActaDetalles",
                newName: "MaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_ActaDetalles_InsumoId",
                table: "ActaDetalles",
                newName: "IX_ActaDetalles_MaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_ActaDetalles_ConceptoId_UnidadMedidaId_InsumoId",
                table: "ActaDetalles",
                newName: "IX_ActaDetalles_ConceptoId_UnidadMedidaId_MaterialId");

            migrationBuilder.AddColumn<int>(
                name: "MaterialId",
                table: "Insumos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Materiales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Unidad = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materiales", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "MenuOpciones",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Descripcion", "Url" },
                values: new object[] { "Materiales", "/admin/materiales" });

            migrationBuilder.CreateIndex(
                name: "IX_Insumos_MaterialId",
                table: "Insumos",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_Materiales_Descripcion",
                table: "Materiales",
                column: "Descripcion",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ActaDetalles_Materiales_MaterialId",
                table: "ActaDetalles",
                column: "MaterialId",
                principalTable: "Materiales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Insumos_Materiales_MaterialId",
                table: "Insumos",
                column: "MaterialId",
                principalTable: "Materiales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProveedorMateriales_Materiales_MaterialId",
                table: "ProveedorMateriales",
                column: "MaterialId",
                principalTable: "Materiales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
