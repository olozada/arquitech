SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF COL_LENGTH('Actas', 'Cerrada') IS NULL
    ALTER TABLE Actas ADD Cerrada bit NOT NULL
        CONSTRAINT DF_Actas_Cerrada DEFAULT 0;

IF NOT EXISTS (
    SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261111010000_CierreActa'
)
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261111010000_CierreActa', '8.0.11');

COMMIT;
