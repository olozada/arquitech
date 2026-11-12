using Arquitectos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arquitectos.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ArquitectosDbContext))]
    [Migration("20261111190000_ModuloPresupuesto")]
    public class ModuloPresupuesto : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                SET QUOTED_IDENTIFIER ON;
                SET ANSI_NULLS ON;

                IF OBJECT_ID('dbo.Insumos') IS NULL
                CREATE TABLE dbo.Insumos (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Insumos PRIMARY KEY,
                    Codigo nvarchar(30) NULL,
                    Descripcion nvarchar(500) NOT NULL,
                    Tipo int NOT NULL,
                    UnidadCompra nvarchar(30) NOT NULL CONSTRAINT DF_Insumos_UnidadCompra DEFAULT N'',
                    PrecioBase decimal(19,6) NOT NULL,
                    PorcentajeIva decimal(9,6) NOT NULL,
                    CantidadPorUnidad decimal(19,6) NOT NULL,
                    UnidadUso nvarchar(30) NOT NULL CONSTRAINT DF_Insumos_UnidadUso DEFAULT N'',
                    PrecioUnitario decimal(19,6) NOT NULL,
                    MaterialId int NULL,
                    FechaPrecio datetime2 NOT NULL,
                    Activo bit NOT NULL CONSTRAINT DF_Insumos_Activo DEFAULT 1,
                    CONSTRAINT FK_Insumos_Materiales_MaterialId FOREIGN KEY (MaterialId) REFERENCES dbo.Materiales (Id)
                );
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Insumos_Codigo' AND object_id = OBJECT_ID('dbo.Insumos'))
                    EXEC(N'CREATE UNIQUE INDEX IX_Insumos_Codigo ON dbo.Insumos(Codigo) WHERE [Codigo] IS NOT NULL;');

                IF OBJECT_ID('dbo.InsumoPreciosHistorial') IS NULL
                CREATE TABLE dbo.InsumoPreciosHistorial (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_InsumoPreciosHistorial PRIMARY KEY,
                    InsumoId int NOT NULL,
                    PrecioBase decimal(19,6) NOT NULL,
                    PorcentajeIva decimal(9,6) NOT NULL,
                    Fecha datetime2 NOT NULL,
                    UsuarioId int NULL,
                    CONSTRAINT FK_InsumoPreciosHistorial_Insumos_InsumoId FOREIGN KEY (InsumoId) REFERENCES dbo.Insumos (Id) ON DELETE CASCADE
                );

                IF OBJECT_ID('dbo.PrestacionesSociales') IS NULL
                CREATE TABLE dbo.PrestacionesSociales (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PrestacionesSociales PRIMARY KEY,
                    Concepto nvarchar(120) NOT NULL,
                    Porcentaje decimal(9,6) NOT NULL,
                    Orden int NOT NULL
                );

                IF OBJECT_ID('dbo.ParametrosManoObra') IS NULL
                CREATE TABLE dbo.ParametrosManoObra (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ParametrosManoObra PRIMARY KEY,
                    DiasMes int NOT NULL,
                    IncluirDiasNoHabiles bit NOT NULL,
                    DiasHabilesAnio int NOT NULL
                );
                IF NOT EXISTS (SELECT 1 FROM dbo.ParametrosManoObra)
                    INSERT INTO dbo.ParametrosManoObra (DiasMes, IncluirDiasNoHabiles, DiasHabilesAnio) VALUES (30, 0, 296);

                IF OBJECT_ID('dbo.Cargos') IS NULL
                CREATE TABLE dbo.Cargos (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cargos PRIMARY KEY,
                    Nombre nvarchar(80) NOT NULL,
                    SalarioMensual decimal(19,2) NOT NULL,
                    Activo bit NOT NULL CONSTRAINT DF_Cargos_Activo DEFAULT 1,
                    CONSTRAINT UQ_Cargos_Nombre UNIQUE (Nombre)
                );

                IF OBJECT_ID('dbo.Cuadrillas') IS NULL
                CREATE TABLE dbo.Cuadrillas (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Cuadrillas PRIMARY KEY,
                    Nombre nvarchar(80) NOT NULL,
                    Descripcion nvarchar(200) NULL,
                    Activo bit NOT NULL CONSTRAINT DF_Cuadrillas_Activo DEFAULT 1
                );

                IF OBJECT_ID('dbo.CuadrillaCargos') IS NULL
                CREATE TABLE dbo.CuadrillaCargos (
                    CuadrillaId int NOT NULL,
                    CargoId int NOT NULL,
                    Cantidad decimal(9,4) NOT NULL,
                    CONSTRAINT PK_CuadrillaCargos PRIMARY KEY (CuadrillaId, CargoId),
                    CONSTRAINT FK_CuadrillaCargos_Cuadrillas_CuadrillaId FOREIGN KEY (CuadrillaId) REFERENCES dbo.Cuadrillas (Id) ON DELETE CASCADE,
                    CONSTRAINT FK_CuadrillaCargos_Cargos_CargoId FOREIGN KEY (CargoId) REFERENCES dbo.Cargos (Id)
                );

                IF OBJECT_ID('dbo.Apus') IS NULL
                CREATE TABLE dbo.Apus (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Apus PRIMARY KEY,
                    Codigo nvarchar(30) NULL,
                    Descripcion nvarchar(500) NOT NULL,
                    UnidadMedidaId int NULL,
                    Unidad nvarchar(20) NOT NULL CONSTRAINT DF_Apus_Unidad DEFAULT N'',
                    EsSubanalisis bit NOT NULL,
                    DivisorGeneral decimal(19,6) NOT NULL,
                    ConceptoId int NULL,
                    Activo bit NOT NULL CONSTRAINT DF_Apus_Activo DEFAULT 1,
                    CONSTRAINT FK_Apus_UnidadesMedida_UnidadMedidaId FOREIGN KEY (UnidadMedidaId) REFERENCES dbo.UnidadesMedida (Id),
                    CONSTRAINT FK_Apus_Conceptos_ConceptoId FOREIGN KEY (ConceptoId) REFERENCES dbo.Conceptos (Id)
                );

                IF OBJECT_ID('dbo.ApuGrupos') IS NULL
                CREATE TABLE dbo.ApuGrupos (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ApuGrupos PRIMARY KEY,
                    ApuId int NOT NULL,
                    Grupo int NOT NULL,
                    Divisor decimal(19,6) NOT NULL,
                    Orden int NOT NULL,
                    CONSTRAINT FK_ApuGrupos_Apus_ApuId FOREIGN KEY (ApuId) REFERENCES dbo.Apus (Id) ON DELETE CASCADE
                );

                IF OBJECT_ID('dbo.ApuLineas') IS NULL
                CREATE TABLE dbo.ApuLineas (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ApuLineas PRIMARY KEY,
                    ApuGrupoId int NOT NULL,
                    Orden int NOT NULL,
                    InsumoId int NULL,
                    CuadrillaId int NULL,
                    SubApuId int NULL,
                    Descripcion nvarchar(500) NOT NULL CONSTRAINT DF_ApuLineas_Descripcion DEFAULT N'',
                    Unidad nvarchar(20) NOT NULL CONSTRAINT DF_ApuLineas_Unidad DEFAULT N'',
                    Cantidad decimal(19,6) NOT NULL,
                    PrecioManual decimal(19,6) NULL,
                    Modo int NOT NULL,
                    Factor decimal(19,8) NOT NULL,
                    CONSTRAINT FK_ApuLineas_ApuGrupos_ApuGrupoId FOREIGN KEY (ApuGrupoId) REFERENCES dbo.ApuGrupos (Id) ON DELETE CASCADE,
                    CONSTRAINT FK_ApuLineas_Insumos_InsumoId FOREIGN KEY (InsumoId) REFERENCES dbo.Insumos (Id),
                    CONSTRAINT FK_ApuLineas_Cuadrillas_CuadrillaId FOREIGN KEY (CuadrillaId) REFERENCES dbo.Cuadrillas (Id),
                    CONSTRAINT FK_ApuLineas_Apus_SubApuId FOREIGN KEY (SubApuId) REFERENCES dbo.Apus (Id)
                );

                IF OBJECT_ID('dbo.Presupuestos') IS NULL
                CREATE TABLE dbo.Presupuestos (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Presupuestos PRIMARY KEY,
                    ConstruccionId int NOT NULL,
                    Nombre nvarchar(200) NOT NULL,
                    Version int NOT NULL,
                    Estado int NOT NULL,
                    Fecha date NOT NULL,
                    MesesProyecto int NOT NULL,
                    AreaConstruidaM2 decimal(19,4) NOT NULL,
                    PctImprevistos decimal(9,6) NOT NULL,
                    PctIncremento decimal(9,6) NOT NULL,
                    PctAdministracion decimal(9,6) NOT NULL,
                    PctUtilidad decimal(9,6) NOT NULL,
                    PctIvaUtilidad decimal(9,6) NOT NULL,
                    FechaAprobacion datetime2 NULL,
                    UsuarioAprobacionId int NULL,
                    CONSTRAINT FK_Presupuestos_Construcciones_ConstruccionId FOREIGN KEY (ConstruccionId) REFERENCES dbo.Construcciones (Id)
                );
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Presupuestos_Aprobado' AND object_id = OBJECT_ID('dbo.Presupuestos'))
                    EXEC(N'CREATE UNIQUE INDEX IX_Presupuestos_Aprobado ON dbo.Presupuestos(ConstruccionId) WHERE [Estado] = 2;');

                IF OBJECT_ID('dbo.PresupuestoCapitulos') IS NULL
                CREATE TABLE dbo.PresupuestoCapitulos (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PresupuestoCapitulos PRIMARY KEY,
                    PresupuestoId int NOT NULL,
                    Numero int NOT NULL,
                    Nombre nvarchar(200) NOT NULL,
                    Orden int NOT NULL,
                    CONSTRAINT FK_PresupuestoCapitulos_Presupuestos_PresupuestoId FOREIGN KEY (PresupuestoId) REFERENCES dbo.Presupuestos (Id) ON DELETE CASCADE
                );

                IF OBJECT_ID('dbo.PresupuestoItems') IS NULL
                CREATE TABLE dbo.PresupuestoItems (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PresupuestoItems PRIMARY KEY,
                    CapituloId int NOT NULL,
                    Codigo nvarchar(20) NOT NULL,
                    Descripcion nvarchar(500) NOT NULL,
                    UnidadMedidaId int NULL,
                    Unidad nvarchar(20) NOT NULL CONSTRAINT DF_PresupuestoItems_Unidad DEFAULT N'',
                    ApuId int NULL,
                    FactorConversionApu decimal(19,6) NOT NULL,
                    ConceptoId int NULL,
                    Cantidad decimal(19,4) NOT NULL,
                    CantidadOrigenItemId int NULL,
                    CantidadOrigenFactor decimal(19,6) NOT NULL,
                    ValorMaterialUnitario decimal(19,6) NOT NULL,
                    ValorManoObraUnitario decimal(19,6) NOT NULL,
                    DesgloseApuJson nvarchar(max) NULL,
                    Orden int NOT NULL,
                    CONSTRAINT FK_PresupuestoItems_PresupuestoCapitulos_CapituloId FOREIGN KEY (CapituloId) REFERENCES dbo.PresupuestoCapitulos (Id) ON DELETE CASCADE,
                    CONSTRAINT FK_PresupuestoItems_UnidadesMedida_UnidadMedidaId FOREIGN KEY (UnidadMedidaId) REFERENCES dbo.UnidadesMedida (Id),
                    CONSTRAINT FK_PresupuestoItems_Apus_ApuId FOREIGN KEY (ApuId) REFERENCES dbo.Apus (Id),
                    CONSTRAINT FK_PresupuestoItems_Conceptos_ConceptoId FOREIGN KEY (ConceptoId) REFERENCES dbo.Conceptos (Id)
                );

                IF OBJECT_ID('dbo.PresupuestoMediciones') IS NULL
                CREATE TABLE dbo.PresupuestoMediciones (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PresupuestoMediciones PRIMARY KEY,
                    PresupuestoItemId int NOT NULL,
                    Ubicacion nvarchar(200) NOT NULL CONSTRAINT DF_PresupuestoMediciones_Ubicacion DEFAULT N'',
                    Veces decimal(19,4) NOT NULL,
                    Largo decimal(19,4) NULL,
                    Ancho decimal(19,4) NULL,
                    Alto decimal(19,4) NULL,
                    Parcial decimal(19,4) NOT NULL,
                    CONSTRAINT FK_PresupuestoMediciones_PresupuestoItems_PresupuestoItemId FOREIGN KEY (PresupuestoItemId) REFERENCES dbo.PresupuestoItems (Id) ON DELETE CASCADE
                );

                IF COL_LENGTH('dbo.ActaDetalles', 'PresupuestoItemId') IS NULL
                    ALTER TABLE dbo.ActaDetalles ADD PresupuestoItemId int NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ActaDetalles_PresupuestoItems_PresupuestoItemId')
                    ALTER TABLE dbo.ActaDetalles ADD CONSTRAINT FK_ActaDetalles_PresupuestoItems_PresupuestoItemId
                        FOREIGN KEY (PresupuestoItemId) REFERENCES dbo.PresupuestoItems (Id);

                IF NOT EXISTS (SELECT 1 FROM dbo.MenuOpciones WHERE Id = 18)
                BEGIN
                    SET IDENTITY_INSERT dbo.MenuOpciones ON;
                    INSERT INTO dbo.MenuOpciones (Id, PadreId, Descripcion, Url, Icono, Orden) VALUES
                    (18, NULL, N'Presupuestos', NULL, N'fa fa-calculator', 4),
                    (19, 18, N'Presupuestos de obra', N'/presupuestos', N'fa fa-file-text-o', 1),
                    (20, 18, N'Insumos', N'/presupuesto/insumos', N'fa fa-cubes', 2),
                    (21, 18, N'Mano de obra', N'/presupuesto/mano-obra', N'fa fa-users', 3),
                    (22, 18, N'Análisis de precios', N'/presupuesto/apu', N'fa fa-list-alt', 4);
                    SET IDENTITY_INSERT dbo.MenuOpciones OFF;
                END;

                INSERT INTO dbo.MenuOpcionTipoUsuario (MenuOpcionId, TipoUsuarioId)
                SELECT origen.MenuOpcionId, origen.TipoUsuarioId
                FROM (VALUES (18, 1), (19, 1), (20, 1), (21, 1), (22, 1), (18, 2), (19, 2)) AS origen(MenuOpcionId, TipoUsuarioId)
                WHERE NOT EXISTS (
                    SELECT 1 FROM dbo.MenuOpcionTipoUsuario actual
                    WHERE actual.MenuOpcionId = origen.MenuOpcionId AND actual.TipoUsuarioId = origen.TipoUsuarioId);
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM dbo.MenuOpcionTipoUsuario WHERE MenuOpcionId IN (18, 19, 20, 21, 22);
                DELETE FROM dbo.MenuOpciones WHERE Id IN (18, 19, 20, 21, 22);
                IF COL_LENGTH('dbo.ActaDetalles', 'PresupuestoItemId') IS NOT NULL
                BEGIN
                    ALTER TABLE dbo.ActaDetalles DROP CONSTRAINT IF EXISTS FK_ActaDetalles_PresupuestoItems_PresupuestoItemId;
                    ALTER TABLE dbo.ActaDetalles DROP COLUMN PresupuestoItemId;
                END
                DROP TABLE IF EXISTS dbo.PresupuestoMediciones;
                DROP TABLE IF EXISTS dbo.PresupuestoItems;
                DROP TABLE IF EXISTS dbo.PresupuestoCapitulos;
                DROP TABLE IF EXISTS dbo.Presupuestos;
                DROP TABLE IF EXISTS dbo.ApuLineas;
                DROP TABLE IF EXISTS dbo.ApuGrupos;
                DROP TABLE IF EXISTS dbo.Apus;
                DROP TABLE IF EXISTS dbo.CuadrillaCargos;
                DROP TABLE IF EXISTS dbo.Cuadrillas;
                DROP TABLE IF EXISTS dbo.Cargos;
                DROP TABLE IF EXISTS dbo.PrestacionesSociales;
                DROP TABLE IF EXISTS dbo.ParametrosManoObra;
                DROP TABLE IF EXISTS dbo.InsumoPreciosHistorial;
                DROP TABLE IF EXISTS dbo.Insumos;
                """);
        }
    }
}
