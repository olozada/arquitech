SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF COL_LENGTH('ActaDetalles', 'Aprobado') IS NULL
    ALTER TABLE ActaDetalles ADD Aprobado bit NOT NULL
        CONSTRAINT DF_ActaDetalles_Aprobado DEFAULT 1;

IF NOT EXISTS (
    SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261110233000_RevisionActa'
)
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261110233000_RevisionActa', '8.0.11');

COMMIT;
