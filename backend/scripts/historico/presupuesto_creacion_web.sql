SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
SET NOCOUNT ON;

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Presupuestos_Aprobado' AND object_id = OBJECT_ID('dbo.Presupuestos'))
    DROP INDEX IX_Presupuestos_Aprobado ON dbo.Presupuestos;

ALTER TABLE dbo.Presupuestos ALTER COLUMN ConstruccionId int NULL;

IF COL_LENGTH('dbo.Presupuestos', 'EsPlantilla') IS NULL
    ALTER TABLE dbo.Presupuestos ADD EsPlantilla bit NOT NULL CONSTRAINT DF_Presupuestos_EsPlantilla DEFAULT 0;
IF COL_LENGTH('dbo.Presupuestos', 'PresupuestoOrigenId') IS NULL
    ALTER TABLE dbo.Presupuestos ADD PresupuestoOrigenId int NULL;
GO

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
IF COL_LENGTH('dbo.Presupuestos', 'RowVersion') IS NULL
    ALTER TABLE dbo.Presupuestos ADD RowVersion rowversion NOT NULL;
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_Presupuestos_Plantilla')
    ALTER TABLE dbo.Presupuestos ADD CONSTRAINT CK_Presupuestos_Plantilla CHECK (EsPlantilla = 1 OR ConstruccionId IS NOT NULL);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Presupuestos_Presupuestos_PresupuestoOrigenId')
    ALTER TABLE dbo.Presupuestos ADD CONSTRAINT FK_Presupuestos_Presupuestos_PresupuestoOrigenId
                        FOREIGN KEY (PresupuestoOrigenId) REFERENCES dbo.Presupuestos (Id);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Presupuestos_Aprobado' AND object_id = OBJECT_ID('dbo.Presupuestos'))
    EXEC(N'CREATE UNIQUE INDEX IX_Presupuestos_Aprobado ON dbo.Presupuestos (ConstruccionId) WHERE [Estado] = 2 AND [EsPlantilla] = 0;');
GO

SET NOCOUNT ON;
SELECT c.name AS Columna, c.is_nullable AS Nulo
FROM sys.columns c
WHERE c.object_id = OBJECT_ID('dbo.Presupuestos')
  AND c.name IN ('ConstruccionId', 'EsPlantilla', 'PresupuestoOrigenId', 'RowVersion');
SELECT name FROM sys.indexes WHERE name = 'IX_Presupuestos_Aprobado';
SELECT name FROM sys.check_constraints WHERE name = 'CK_Presupuestos_Plantilla';
