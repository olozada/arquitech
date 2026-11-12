SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'ActaDetalles')
      AND name = N'UnidadMedidaId'
      AND is_nullable = 0)
    ALTER TABLE ActaDetalles ALTER COLUMN UnidadMedidaId int NULL;

IF NOT EXISTS (
    SELECT 1 FROM __EFMigrationsHistory WHERE MigrationId = '20261111000000_ItemOpcional'
)
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('20261111000000_ItemOpcional', '8.0.11');

COMMIT;
