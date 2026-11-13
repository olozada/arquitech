using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arquitectos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MovilSincronizacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DispositivoId",
                table: "SesionesUsuario",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Origen",
                table: "Auditoria",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "web");

            migrationBuilder.AddColumn<DateTime>(
                name: "ModificadaEn",
                table: "Actas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IdCliente",
                table: "ActaDetalles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ActaDetalles",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "Dispositivos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Plataforma = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VersionApp = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RegistradoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UltimaSincronizacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevocadoEn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dispositivos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Dispositivos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OperacionesMovil",
                columns: table => new
                {
                    IdOperacion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DispositivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NumeroActa = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResultadoJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecibidaEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadaEnDispositivo = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperacionesMovil", x => x.IdOperacion);
                    table.ForeignKey(
                        name: "FK_OperacionesMovil_Dispositivos_DispositivoId",
                        column: x => x.DispositivoId,
                        principalTable: "Dispositivos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperacionesMovil_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SesionesUsuario_DispositivoId",
                table: "SesionesUsuario",
                column: "DispositivoId");

            migrationBuilder.CreateIndex(
                name: "IX_ActaDetalles_IdCliente",
                table: "ActaDetalles",
                column: "IdCliente",
                unique: true,
                filter: "[IdCliente] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Dispositivos_UsuarioId",
                table: "Dispositivos",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_OperacionesMovil_DispositivoId",
                table: "OperacionesMovil",
                column: "DispositivoId");

            migrationBuilder.CreateIndex(
                name: "IX_OperacionesMovil_UsuarioId_RecibidaEn",
                table: "OperacionesMovil",
                columns: new[] { "UsuarioId", "RecibidaEn" });

            migrationBuilder.AddForeignKey(
                name: "FK_SesionesUsuario_Dispositivos_DispositivoId",
                table: "SesionesUsuario",
                column: "DispositivoId",
                principalTable: "Dispositivos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SesionesUsuario_Dispositivos_DispositivoId",
                table: "SesionesUsuario");

            migrationBuilder.DropTable(
                name: "OperacionesMovil");

            migrationBuilder.DropTable(
                name: "Dispositivos");

            migrationBuilder.DropIndex(
                name: "IX_SesionesUsuario_DispositivoId",
                table: "SesionesUsuario");

            migrationBuilder.DropIndex(
                name: "IX_ActaDetalles_IdCliente",
                table: "ActaDetalles");

            migrationBuilder.DropColumn(
                name: "DispositivoId",
                table: "SesionesUsuario");

            migrationBuilder.DropColumn(
                name: "Origen",
                table: "Auditoria");

            migrationBuilder.DropColumn(
                name: "ModificadaEn",
                table: "Actas");

            migrationBuilder.DropColumn(
                name: "IdCliente",
                table: "ActaDetalles");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ActaDetalles");
        }
    }
}
