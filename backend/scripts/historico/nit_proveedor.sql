SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF COL_LENGTH('Proveedores', 'Nit') IS NULL
    ALTER TABLE Proveedores ADD Nit nvarchar(20) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Proveedores_Nit' AND object_id = OBJECT_ID('dbo.Proveedores'))
    EXEC(N'CREATE UNIQUE INDEX IX_Proveedores_Nit ON dbo.Proveedores(Nit) WHERE Nit IS NOT NULL;');

IF NOT EXISTS (
    SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261111030000_NitProveedor'
)
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261111030000_NitProveedor', '8.0.11');

COMMIT;
