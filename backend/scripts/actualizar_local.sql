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

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113103515_MovilSincronizacion'
)
BEGIN
    ALTER TABLE [SesionesUsuario] ADD [DispositivoId] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113103515_MovilSincronizacion'
)
BEGIN
    ALTER TABLE [Auditoria] ADD [Origen] nvarchar(20) NOT NULL DEFAULT N'web';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113103515_MovilSincronizacion'
)
BEGIN
    ALTER TABLE [Actas] ADD [ModificadaEn] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113103515_MovilSincronizacion'
)
BEGIN
    ALTER TABLE [ActaDetalles] ADD [IdCliente] uniqueidentifier NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113103515_MovilSincronizacion'
)
BEGIN
    ALTER TABLE [ActaDetalles] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113103515_MovilSincronizacion'
)
BEGIN
    CREATE TABLE [Dispositivos] (
        [Id] uniqueidentifier NOT NULL,
        [UsuarioId] int NOT NULL,
        [Nombre] nvarchar(100) NOT NULL,
        [Plataforma] nvarchar(20) NOT NULL,
        [VersionApp] nvarchar(20) NOT NULL,
        [RegistradoEn] datetime2 NOT NULL,
        [UltimaSincronizacion] datetime2 NULL,
        [RevocadoEn] datetime2 NULL,
        CONSTRAINT [PK_Dispositivos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Dispositivos_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113103515_MovilSincronizacion'
)
BEGIN
    CREATE TABLE [OperacionesMovil] (
        [IdOperacion] uniqueidentifier NOT NULL,
        [DispositivoId] uniqueidentifier NOT NULL,
        [UsuarioId] int NOT NULL,
        [Tipo] nvarchar(30) NOT NULL,
        [NumeroActa] int NOT NULL,
        [Estado] nvarchar(20) NOT NULL,
        [ResultadoJson] nvarchar(max) NULL,
        [RecibidaEn] datetime2 NOT NULL,
        [CreadaEnDispositivo] datetime2 NOT NULL,
        CONSTRAINT [PK_OperacionesMovil] PRIMARY KEY ([IdOperacion]),
        CONSTRAINT [FK_OperacionesMovil_Dispositivos_DispositivoId] FOREIGN KEY ([DispositivoId]) REFERENCES [Dispositivos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OperacionesMovil_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113103515_MovilSincronizacion'
)
BEGIN
    CREATE INDEX [IX_SesionesUsuario_DispositivoId] ON [SesionesUsuario] ([DispositivoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113103515_MovilSincronizacion'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_ActaDetalles_IdCliente] ON [ActaDetalles] ([IdCliente]) WHERE [IdCliente] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113103515_MovilSincronizacion'
)
BEGIN
    CREATE INDEX [IX_Dispositivos_UsuarioId] ON [Dispositivos] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113103515_MovilSincronizacion'
)
BEGIN
    CREATE INDEX [IX_OperacionesMovil_DispositivoId] ON [OperacionesMovil] ([DispositivoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113103515_MovilSincronizacion'
)
BEGIN
    CREATE INDEX [IX_OperacionesMovil_UsuarioId_RecibidaEn] ON [OperacionesMovil] ([UsuarioId], [RecibidaEn]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113103515_MovilSincronizacion'
)
BEGIN
    ALTER TABLE [SesionesUsuario] ADD CONSTRAINT [FK_SesionesUsuario_Dispositivos_DispositivoId] FOREIGN KEY ([DispositivoId]) REFERENCES [Dispositivos] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261113103515_MovilSincronizacion'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261113103515_MovilSincronizacion', N'10.0.15');
END;

COMMIT;
GO

