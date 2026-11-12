SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF COL_LENGTH('Arquitectos', 'Cedula') IS NULL
    ALTER TABLE Arquitectos ADD Cedula nvarchar(20) NULL;
IF COL_LENGTH('Revisores', 'Cedula') IS NULL
    ALTER TABLE Revisores ADD Cedula nvarchar(20) NULL;
IF COL_LENGTH('Contratistas', 'Cedula') IS NULL
    ALTER TABLE Contratistas ADD Cedula nvarchar(20) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Arquitectos_Cedula' AND object_id = OBJECT_ID('dbo.Arquitectos'))
    EXEC(N'CREATE UNIQUE INDEX IX_Arquitectos_Cedula ON dbo.Arquitectos(Cedula) WHERE Cedula IS NOT NULL;');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Revisores_Cedula' AND object_id = OBJECT_ID('dbo.Revisores'))
    EXEC(N'CREATE UNIQUE INDEX IX_Revisores_Cedula ON dbo.Revisores(Cedula) WHERE Cedula IS NOT NULL;');
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Contratistas_Cedula' AND object_id = OBJECT_ID('dbo.Contratistas'))
    EXEC(N'CREATE UNIQUE INDEX IX_Contratistas_Cedula ON dbo.Contratistas(Cedula) WHERE Cedula IS NOT NULL;');

IF NOT EXISTS (
    SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261111020000_CedulaCatalogo'
)
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261111020000_CedulaCatalogo', '8.0.11');

COMMIT;
