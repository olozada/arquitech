BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112232916_IndicesConsulta'
)
BEGIN
    DROP INDEX [IX_ActaDetalles_NumeroActa] ON [ActaDetalles];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112232916_IndicesConsulta'
)
BEGIN
    DROP INDEX [IX_ActaDetalles_PresupuestoItemId] ON [ActaDetalles];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112232916_IndicesConsulta'
)
BEGIN
    CREATE INDEX [IX_ActaDetalles_NumeroActa_InsumoId] ON [ActaDetalles] ([NumeroActa], [InsumoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112232916_IndicesConsulta'
)
BEGIN
    CREATE INDEX [IX_ActaDetalles_PresupuestoItemId_Aprobado] ON [ActaDetalles] ([PresupuestoItemId], [Aprobado]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112232916_IndicesConsulta'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261112232916_IndicesConsulta', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112234120_IconosLucide'
)
BEGIN
    EXEC(N'UPDATE [MenuOpciones] SET [Icono] = N''settings''
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112234120_IconosLucide'
)
BEGIN
    EXEC(N'UPDATE [MenuOpciones] SET [Icono] = N''pencil-ruler''
    WHERE [Id] = 2;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112234120_IconosLucide'
)
BEGIN
    EXEC(N'UPDATE [MenuOpciones] SET [Icono] = N''clipboard-check''
    WHERE [Id] = 3;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112234120_IconosLucide'
)
BEGIN
    EXEC(N'UPDATE [MenuOpciones] SET [Icono] = N''hard-hat''
    WHERE [Id] = 4;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112234120_IconosLucide'
)
BEGIN
    EXEC(N'UPDATE [MenuOpciones] SET [Icono] = N''truck''
    WHERE [Id] = 5;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112234120_IconosLucide'
)
BEGIN
    EXEC(N'UPDATE [MenuOpciones] SET [Icono] = N''building-2''
    WHERE [Id] = 6;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112234120_IconosLucide'
)
BEGIN
    EXEC(N'UPDATE [MenuOpciones] SET [Icono] = N''list-tree''
    WHERE [Id] = 7;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112234120_IconosLucide'
)
BEGIN
    EXEC(N'UPDATE [MenuOpciones] SET [Icono] = N''package''
    WHERE [Id] = 8;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112234120_IconosLucide'
)
BEGIN
    EXEC(N'UPDATE [MenuOpciones] SET [Icono] = N''ruler''
    WHERE [Id] = 9;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112234120_IconosLucide'
)
BEGIN
    EXEC(N'UPDATE [MenuOpciones] SET [Icono] = N''badge-dollar-sign''
    WHERE [Id] = 10;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112234120_IconosLucide'
)
BEGIN
    EXEC(N'UPDATE [MenuOpciones] SET [Icono] = N''file-pen-line''
    WHERE [Id] = 11;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112234120_IconosLucide'
)
BEGIN
    EXEC(N'UPDATE [MenuOpciones] SET [Icono] = N''chart-column''
    WHERE [Id] = 13;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112234120_IconosLucide'
)
BEGIN
    EXEC(N'UPDATE [MenuOpciones] SET [Icono] = N''table''
    WHERE [Id] = 14;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112234120_IconosLucide'
)
BEGIN
    EXEC(N'UPDATE [MenuOpciones] SET [Icono] = N''users''
    WHERE [Id] = 15;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112234120_IconosLucide'
)
BEGIN
    EXEC(N'UPDATE [MenuOpciones] SET [Icono] = N''key-round''
    WHERE [Id] = 16;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112234120_IconosLucide'
)
BEGIN
    EXEC(N'UPDATE [MenuOpciones] SET [Icono] = N''layout-dashboard''
    WHERE [Id] = 17;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112234120_IconosLucide'
)
BEGIN
    EXEC(N'UPDATE [MenuOpciones] SET [Icono] = N''wallet''
    WHERE [Id] = 18;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112234120_IconosLucide'
)
BEGIN
    EXEC(N'UPDATE [MenuOpciones] SET [Icono] = N''file-spreadsheet''
    WHERE [Id] = 19;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112234120_IconosLucide'
)
BEGIN
    EXEC(N'UPDATE [MenuOpciones] SET [Icono] = N''package''
    WHERE [Id] = 20;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112234120_IconosLucide'
)
BEGIN
    EXEC(N'UPDATE [MenuOpciones] SET [Icono] = N''hammer''
    WHERE [Id] = 21;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112234120_IconosLucide'
)
BEGIN
    EXEC(N'UPDATE [MenuOpciones] SET [Icono] = N''calculator''
    WHERE [Id] = 22;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112234120_IconosLucide'
)
BEGIN
    EXEC(N'UPDATE [MenuOpciones] SET [Icono] = N''brick-wall''
    WHERE [Id] = 23;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112234120_IconosLucide'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261112234120_IconosLucide', N'10.0.15');
END;

COMMIT;
GO

