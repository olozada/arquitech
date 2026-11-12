BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112212941_SeguridadLogin'
)
BEGIN
    ALTER TABLE [Usuarios] ADD [BloqueadoHasta] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112212941_SeguridadLogin'
)
BEGIN
    ALTER TABLE [Usuarios] ADD [DebeCambiarContrasena] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112212941_SeguridadLogin'
)
BEGIN
    ALTER TABLE [Usuarios] ADD [IntentosFallidos] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112212941_SeguridadLogin'
)
BEGIN
    EXEC(N'UPDATE [Usuarios] SET [BloqueadoHasta] = NULL, [DebeCambiarContrasena] = CAST(1 AS bit)
    WHERE [Id] = 1;
    SELECT @@ROWCOUNT');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112212941_SeguridadLogin'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261112212941_SeguridadLogin', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112215352_SesionesUsuario'
)
BEGIN
    CREATE TABLE [SesionesUsuario] (
        [Id] bigint NOT NULL IDENTITY,
        [UsuarioId] int NOT NULL,
        [HashToken] nvarchar(64) NOT NULL,
        [Expira] datetime2 NOT NULL,
        [CreadoEn] datetime2 NOT NULL,
        [RevocadoEn] datetime2 NULL,
        [Ip] nvarchar(64) NULL,
        [UserAgent] nvarchar(256) NULL,
        CONSTRAINT [PK_SesionesUsuario] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SesionesUsuario_Usuarios_UsuarioId] FOREIGN KEY ([UsuarioId]) REFERENCES [Usuarios] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112215352_SesionesUsuario'
)
BEGIN
    CREATE UNIQUE INDEX [IX_SesionesUsuario_HashToken] ON [SesionesUsuario] ([HashToken]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112215352_SesionesUsuario'
)
BEGIN
    CREATE INDEX [IX_SesionesUsuario_UsuarioId] ON [SesionesUsuario] ([UsuarioId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112215352_SesionesUsuario'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261112215352_SesionesUsuario', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112220615_Auditoria'
)
BEGIN
    CREATE TABLE [Auditoria] (
        [Id] bigint NOT NULL IDENTITY,
        [Fecha] datetime2 NOT NULL,
        [UsuarioId] int NULL,
        [Entidad] nvarchar(64) NOT NULL,
        [EntidadId] nvarchar(64) NOT NULL,
        [Accion] nvarchar(20) NOT NULL,
        [CambiosJson] nvarchar(max) NULL,
        [Ip] nvarchar(64) NULL,
        CONSTRAINT [PK_Auditoria] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112220615_Auditoria'
)
BEGIN
    CREATE INDEX [IX_Auditoria_Entidad_EntidadId] ON [Auditoria] ([Entidad], [EntidadId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112220615_Auditoria'
)
BEGIN
    CREATE INDEX [IX_Auditoria_Fecha] ON [Auditoria] ([Fecha]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112220615_Auditoria'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261112220615_Auditoria', N'10.0.15');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112221024_ConcurrenciaActas'
)
BEGIN
    ALTER TABLE [Actas] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261112221024_ConcurrenciaActas'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261112221024_ConcurrenciaActas', N'10.0.15');
END;

COMMIT;
GO

