using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Arquitectos.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ArquitectosDbContext))]
[Migration("20261126230000_MapeoProductosProveedor")]
public class MapeoProductosProveedor : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "MapeoProductos",
            table: "Proveedores",
            type: "nvarchar(2000)",
            maxLength: 2000,
            nullable: true);

        migrationBuilder.Sql("""
            UPDATE dbo.MenuOpciones
            SET Descripcion = N'Productos por proveedor'
            WHERE Url = N'/admin/proveedores-materiales';
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "MapeoProductos", table: "Proveedores");
        migrationBuilder.Sql("""
            UPDATE dbo.MenuOpciones
            SET Descripcion = N'Materiales por proveedor'
            WHERE Url = N'/admin/proveedores-materiales';
            """);
    }
}
