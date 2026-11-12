using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arquitectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SincronizarSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Solo sincroniza el snapshot: las tablas ya existen (ModuloPresupuesto y PresupuestoCreacionWeb).
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Solo sincroniza el snapshot: las tablas ya existen (ModuloPresupuesto y PresupuestoCreacionWeb).
        }
    }
}
