-- =============================================================================
-- SCHEMA COMPLETO PlateArchive (SQL Server)
--
-- GENERATO da EF Core Migrations (NON modificare a mano).
-- Rigenerare con:
--   dotnet ef migrations script -i \
--     --project PlateArchive.Data/PlateArchive.Data.csproj \
--     --startup-project PlateArchive.Wpf/PlateArchive.Wpf.csproj \
--     -o docs/sql/schema_completo.sql
--
-- Idempotente (-i): crea/applica solo ciò che manca, in base a __EFMigrationsHistory.
-- Riflette tutte le migrazioni fino a 20260709080730_AssociazioneMacchinePiastre.
-- =============================================================================

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    CREATE TABLE [CategoriePiastre] (
        [IdCategoriaPiastra] int NOT NULL IDENTITY,
        [Codice] nvarchar(450) NOT NULL,
        [Descrizione] nvarchar(max) NOT NULL,
        [Ordine] int NOT NULL,
        CONSTRAINT [PK_CategoriePiastre] PRIMARY KEY ([IdCategoriaPiastra])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    CREATE TABLE [Clienti] (
        [IdCliente] int NOT NULL IDENTITY,
        [CodiceClienteGestionale] nvarchar(450) NOT NULL,
        [RagioneSociale] nvarchar(max) NOT NULL,
        [Note] nvarchar(max) NULL,
        CONSTRAINT [PK_Clienti] PRIMARY KEY ([IdCliente])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    CREATE TABLE [MacchineStandard] (
        [IdMacchinaStandard] int NOT NULL IDENTITY,
        [CodiceMacchina] nvarchar(450) NOT NULL,
        [NomeMacchina] nvarchar(max) NOT NULL,
        [Famiglia] nvarchar(max) NULL,
        [Formato] nvarchar(max) NULL,
        [Versione] nvarchar(max) NULL,
        [Produttore] nvarchar(max) NULL,
        [Attiva] bit NOT NULL,
        [Note] nvarchar(max) NULL,
        CONSTRAINT [PK_MacchineStandard] PRIMARY KEY ([IdMacchinaStandard])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    CREATE TABLE [Piastre] (
        [IdPiastra] int NOT NULL IDENTITY,
        [CodicePiastra] nvarchar(450) NOT NULL,
        [CodiceArticoloGestionale] nvarchar(450) NULL,
        [Descrizione] nvarchar(max) NULL,
        [Stato] int NOT NULL,
        [IdCategoriaPiastra] int NULL,
        [IsEliminata] bit NOT NULL,
        [LarghezzaMm] decimal(18,2) NULL,
        [AltezzaMm] decimal(18,2) NULL,
        [SpessoreMm] decimal(18,2) NULL,
        [Durezza] decimal(18,2) NULL,
        [Peso] decimal(18,3) NULL,
        [Note] nvarchar(max) NULL,
        [DataCreazione] datetime2 NOT NULL,
        [DataUltimaModifica] datetime2 NOT NULL,
        CONSTRAINT [PK_Piastre] PRIMARY KEY ([IdPiastra]),
        CONSTRAINT [FK_Piastre_CategoriePiastre_IdCategoriaPiastra] FOREIGN KEY ([IdCategoriaPiastra]) REFERENCES [CategoriePiastre] ([IdCategoriaPiastra]) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    CREATE TABLE [ClientiMacchine] (
        [IdClienteMacchina] int NOT NULL IDENTITY,
        [IdCliente] int NOT NULL,
        [IdMacchinaStandard] int NOT NULL,
        [Matricola] nvarchar(max) NULL,
        [CodiceInternoCliente] nvarchar(max) NULL,
        [DataAssociazione] datetime2 NOT NULL,
        [Attiva] bit NOT NULL,
        [Note] nvarchar(max) NULL,
        CONSTRAINT [PK_ClientiMacchine] PRIMARY KEY ([IdClienteMacchina]),
        CONSTRAINT [FK_ClientiMacchine_Clienti_IdCliente] FOREIGN KEY ([IdCliente]) REFERENCES [Clienti] ([IdCliente]) ON DELETE CASCADE,
        CONSTRAINT [FK_ClientiMacchine_MacchineStandard_IdMacchinaStandard] FOREIGN KEY ([IdMacchinaStandard]) REFERENCES [MacchineStandard] ([IdMacchinaStandard]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    CREATE TABLE [Disegni] (
        [IdDisegno] int NOT NULL IDENTITY,
        [IdPiastra] int NOT NULL,
        [CodiceDisegno] nvarchar(max) NULL,
        [NomeFile] nvarchar(max) NULL,
        [PercorsoFile] nvarchar(max) NULL,
        [VaultId] nvarchar(max) NULL,
        [Revisione] nvarchar(max) NULL,
        [Formato] nvarchar(max) NULL,
        [Stato] int NOT NULL,
        [DataUltimaModificaFile] datetime2 NULL,
        [Note] nvarchar(max) NULL,
        CONSTRAINT [PK_Disegni] PRIMARY KEY ([IdDisegno]),
        CONSTRAINT [FK_Disegni_Piastre_IdPiastra] FOREIGN KEY ([IdPiastra]) REFERENCES [Piastre] ([IdPiastra]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    CREATE TABLE [PiastreMacchineCompatibili] (
        [IdCompatibilita] int NOT NULL IDENTITY,
        [IdPiastra] int NOT NULL,
        [IdMacchinaStandard] int NOT NULL,
        [FonteDato] int NULL,
        [DataVerifica] datetime2 NULL,
        [UtenteVerifica] nvarchar(max) NULL,
        [Attiva] bit NOT NULL,
        [Note] nvarchar(max) NULL,
        CONSTRAINT [PK_PiastreMacchineCompatibili] PRIMARY KEY ([IdCompatibilita]),
        CONSTRAINT [FK_PiastreMacchineCompatibili_MacchineStandard_IdMacchinaStandard] FOREIGN KEY ([IdMacchinaStandard]) REFERENCES [MacchineStandard] ([IdMacchinaStandard]) ON DELETE CASCADE,
        CONSTRAINT [FK_PiastreMacchineCompatibili_Piastre_IdPiastra] FOREIGN KEY ([IdPiastra]) REFERENCES [Piastre] ([IdPiastra]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    CREATE TABLE [ClientiPiastre] (
        [IdClientePiastra] int NOT NULL IDENTITY,
        [IdCliente] int NOT NULL,
        [IdPiastra] int NOT NULL,
        [IdClienteMacchina] int NULL,
        [DataAssociazione] datetime2 NOT NULL,
        [Stato] int NOT NULL,
        [Note] nvarchar(max) NULL,
        CONSTRAINT [PK_ClientiPiastre] PRIMARY KEY ([IdClientePiastra]),
        CONSTRAINT [FK_ClientiPiastre_Clienti_IdCliente] FOREIGN KEY ([IdCliente]) REFERENCES [Clienti] ([IdCliente]) ON DELETE CASCADE,
        CONSTRAINT [FK_ClientiPiastre_ClientiMacchine_IdClienteMacchina] FOREIGN KEY ([IdClienteMacchina]) REFERENCES [ClientiMacchine] ([IdClienteMacchina]),
        CONSTRAINT [FK_ClientiPiastre_Piastre_IdPiastra] FOREIGN KEY ([IdPiastra]) REFERENCES [Piastre] ([IdPiastra]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CategoriePiastre_Codice] ON [CategoriePiastre] ([Codice]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Clienti_CodiceClienteGestionale] ON [Clienti] ([CodiceClienteGestionale]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ClientiMacchine_IdCliente] ON [ClientiMacchine] ([IdCliente]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ClientiMacchine_IdMacchinaStandard] ON [ClientiMacchine] ([IdMacchinaStandard]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ClientiPiastre_IdCliente] ON [ClientiPiastre] ([IdCliente]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ClientiPiastre_IdPiastra] ON [ClientiPiastre] ([IdPiastra]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ClientiPiastre_IdClienteMacchina] ON [ClientiPiastre] ([IdClienteMacchina]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ClientiPiastre_IdCliente_IdPiastra] ON [ClientiPiastre] ([IdCliente], [IdPiastra]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Disegni_IdPiastra] ON [Disegni] ([IdPiastra]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MacchineStandard_CodiceMacchina] ON [MacchineStandard] ([CodiceMacchina]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Piastre_CodiceArticoloGestionale] ON [Piastre] ([CodiceArticoloGestionale]) WHERE [CodiceArticoloGestionale] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Piastre_CodicePiastra] ON [Piastre] ([CodicePiastra]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Piastre_IdCategoriaPiastra] ON [Piastre] ([IdCategoriaPiastra]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PiastreMacchineCompatibili_IdMacchinaStandard] ON [PiastreMacchineCompatibili] ([IdMacchinaStandard]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PiastreMacchineCompatibili_IdPiastra] ON [PiastreMacchineCompatibili] ([IdPiastra]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PiastreMacchineCompatibili_IdPiastra_IdMacchinaStandard] ON [PiastreMacchineCompatibili] ([IdPiastra], [IdMacchinaStandard]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Codice', N'Descrizione', N'Ordine') AND [object_id] = OBJECT_ID(N'[CategoriePiastre]'))
        SET IDENTITY_INSERT [CategoriePiastre] ON;
    EXEC(N'INSERT INTO [CategoriePiastre] ([Codice], [Descrizione], [Ordine])
    VALUES (N''STD'', N''Standard'', 1),
    (N''SPE'', N''Speciale'', 2)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Codice', N'Descrizione', N'Ordine') AND [object_id] = OBJECT_ID(N'[CategoriePiastre]'))
        SET IDENTITY_INSERT [CategoriePiastre] OFF;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625140000_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260625140000_InitialCreate', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625160000_AddProduttoriFamiglieMacchine'
)
BEGIN
    CREATE TABLE [FamiglieMacchine] (
        [IdFamiglia] int NOT NULL IDENTITY,
        [NomeFamiglia] nvarchar(450) NOT NULL,
        [IsEliminata] bit NOT NULL,
        [Note] nvarchar(max) NULL,
        CONSTRAINT [PK_FamiglieMacchine] PRIMARY KEY ([IdFamiglia])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625160000_AddProduttoriFamiglieMacchine'
)
BEGIN
    CREATE TABLE [ProduttoriMacchine] (
        [IdProduttore] int NOT NULL IDENTITY,
        [NomeProduttore] nvarchar(450) NOT NULL,
        [IsEliminata] bit NOT NULL,
        [Note] nvarchar(max) NULL,
        CONSTRAINT [PK_ProduttoriMacchine] PRIMARY KEY ([IdProduttore])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625160000_AddProduttoriFamiglieMacchine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FamiglieMacchine_NomeFamiglia] ON [FamiglieMacchine] ([NomeFamiglia]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625160000_AddProduttoriFamiglieMacchine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ProduttoriMacchine_NomeProduttore] ON [ProduttoriMacchine] ([NomeProduttore]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625160000_AddProduttoriFamiglieMacchine'
)
BEGIN
    ALTER TABLE [MacchineStandard] ADD [IdFamiglia] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625160000_AddProduttoriFamiglieMacchine'
)
BEGIN
    ALTER TABLE [MacchineStandard] ADD [IdProduttore] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625160000_AddProduttoriFamiglieMacchine'
)
BEGIN
    ALTER TABLE [MacchineStandard] ADD [LarghezzaMm] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625160000_AddProduttoriFamiglieMacchine'
)
BEGIN
    ALTER TABLE [MacchineStandard] ADD [AltezzaMm] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625160000_AddProduttoriFamiglieMacchine'
)
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MacchineStandard]') AND [c].[name] = N'Famiglia');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [MacchineStandard] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [MacchineStandard] DROP COLUMN [Famiglia];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625160000_AddProduttoriFamiglieMacchine'
)
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MacchineStandard]') AND [c].[name] = N'Formato');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [MacchineStandard] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [MacchineStandard] DROP COLUMN [Formato];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625160000_AddProduttoriFamiglieMacchine'
)
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MacchineStandard]') AND [c].[name] = N'Produttore');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [MacchineStandard] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [MacchineStandard] DROP COLUMN [Produttore];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625160000_AddProduttoriFamiglieMacchine'
)
BEGIN
    ALTER TABLE [MacchineStandard] ADD CONSTRAINT [FK_MacchineStandard_FamiglieMacchine_IdFamiglia] FOREIGN KEY ([IdFamiglia]) REFERENCES [FamiglieMacchine] ([IdFamiglia]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625160000_AddProduttoriFamiglieMacchine'
)
BEGIN
    ALTER TABLE [MacchineStandard] ADD CONSTRAINT [FK_MacchineStandard_ProduttoriMacchine_IdProduttore] FOREIGN KEY ([IdProduttore]) REFERENCES [ProduttoriMacchine] ([IdProduttore]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625160000_AddProduttoriFamiglieMacchine'
)
BEGIN
    CREATE INDEX [IX_MacchineStandard_IdFamiglia] ON [MacchineStandard] ([IdFamiglia]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625160000_AddProduttoriFamiglieMacchine'
)
BEGIN
    CREATE INDEX [IX_MacchineStandard_IdProduttore] ON [MacchineStandard] ([IdProduttore]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625160000_AddProduttoriFamiglieMacchine'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260625160000_AddProduttoriFamiglieMacchine', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625180000_RinominaFamiglieInFormatiAddFormatoPiastra'
)
BEGIN
    ALTER TABLE [MacchineStandard] DROP CONSTRAINT [FK_MacchineStandard_FamiglieMacchine_IdFamiglia];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625180000_RinominaFamiglieInFormatiAddFormatoPiastra'
)
BEGIN
    DROP INDEX [IX_FamiglieMacchine_NomeFamiglia] ON [FamiglieMacchine];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625180000_RinominaFamiglieInFormatiAddFormatoPiastra'
)
BEGIN
    DROP INDEX [IX_MacchineStandard_IdFamiglia] ON [MacchineStandard];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625180000_RinominaFamiglieInFormatiAddFormatoPiastra'
)
BEGIN
    EXEC sp_rename N'[FamiglieMacchine]', N'FormatiMacchine';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625180000_RinominaFamiglieInFormatiAddFormatoPiastra'
)
BEGIN
    EXEC sp_rename N'[FormatiMacchine].[IdFamiglia]', N'IdFormato', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625180000_RinominaFamiglieInFormatiAddFormatoPiastra'
)
BEGIN
    EXEC sp_rename N'[FormatiMacchine].[NomeFamiglia]', N'NomeFormato', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625180000_RinominaFamiglieInFormatiAddFormatoPiastra'
)
BEGIN
    EXEC sp_rename N'[MacchineStandard].[IdFamiglia]', N'IdFormato', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625180000_RinominaFamiglieInFormatiAddFormatoPiastra'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FormatiMacchine_NomeFormato] ON [FormatiMacchine] ([NomeFormato]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625180000_RinominaFamiglieInFormatiAddFormatoPiastra'
)
BEGIN
    CREATE INDEX [IX_MacchineStandard_IdFormato] ON [MacchineStandard] ([IdFormato]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625180000_RinominaFamiglieInFormatiAddFormatoPiastra'
)
BEGIN
    ALTER TABLE [MacchineStandard] ADD CONSTRAINT [FK_MacchineStandard_FormatiMacchine_IdFormato] FOREIGN KEY ([IdFormato]) REFERENCES [FormatiMacchine] ([IdFormato]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625180000_RinominaFamiglieInFormatiAddFormatoPiastra'
)
BEGIN
    ALTER TABLE [Piastre] ADD [IdFormato] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625180000_RinominaFamiglieInFormatiAddFormatoPiastra'
)
BEGIN
    CREATE INDEX [IX_Piastre_IdFormato] ON [Piastre] ([IdFormato]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625180000_RinominaFamiglieInFormatiAddFormatoPiastra'
)
BEGIN
    ALTER TABLE [Piastre] ADD CONSTRAINT [FK_Piastre_FormatiMacchine_IdFormato] FOREIGN KEY ([IdFormato]) REFERENCES [FormatiMacchine] ([IdFormato]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260625180000_RinominaFamiglieInFormatiAddFormatoPiastra'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260625180000_RinominaFamiglieInFormatiAddFormatoPiastra', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra'
)
BEGIN
    ALTER TABLE [Disegni] DROP CONSTRAINT [FK_Disegni_Piastre_IdPiastra];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra'
)
BEGIN
    ALTER TABLE [MacchineStandard] DROP CONSTRAINT [FK_MacchineStandard_ProduttoriMacchine_IdProduttore];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra'
)
BEGIN
    DROP INDEX [IX_ProduttoriMacchine_NomeProduttore] ON [ProduttoriMacchine];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra'
)
BEGIN
    DROP INDEX [IX_PiastreMacchineCompatibili_IdPiastra] ON [PiastreMacchineCompatibili];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra'
)
BEGIN
    DROP INDEX [IX_FormatiMacchine_NomeFormato] ON [FormatiMacchine];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra'
)
BEGIN
    DROP INDEX [IX_Disegni_IdPiastra] ON [Disegni];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra'
)
BEGIN
    DROP INDEX [IX_ClientiPiastre_IdCliente] ON [ClientiPiastre];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra'
)
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ProduttoriMacchine]') AND [c].[name] = N'NomeProduttore');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [ProduttoriMacchine] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [ProduttoriMacchine] ALTER COLUMN [NomeProduttore] nvarchar(max) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra'
)
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Piastre]') AND [c].[name] = N'Peso');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Piastre] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [Piastre] ALTER COLUMN [Peso] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra'
)
BEGIN
    ALTER TABLE [Piastre] ADD [IdClienteEsclusivo] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra'
)
BEGIN
    ALTER TABLE [Piastre] ADD [TipoPiastra] int NOT NULL DEFAULT 0;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[FormatiMacchine]') AND [c].[name] = N'NomeFormato');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [FormatiMacchine] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [FormatiMacchine] ALTER COLUMN [NomeFormato] nvarchar(max) NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Disegni]') AND [c].[name] = N'IdPiastra');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Disegni] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [Disegni] ALTER COLUMN [IdPiastra] int NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra'
)
BEGIN
    CREATE INDEX [IX_Piastre_IdClienteEsclusivo] ON [Piastre] ([IdClienteEsclusivo]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_Disegni_IdPiastra] ON [Disegni] ([IdPiastra]) WHERE [IdPiastra] IS NOT NULL');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra'
)
BEGIN
    ALTER TABLE [Disegni] ADD CONSTRAINT [FK_Disegni_Piastre_IdPiastra] FOREIGN KEY ([IdPiastra]) REFERENCES [Piastre] ([IdPiastra]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra'
)
BEGIN
    ALTER TABLE [MacchineStandard] ADD CONSTRAINT [FK_MacchineStandard_ProduttoriMacchine_IdProduttore] FOREIGN KEY ([IdProduttore]) REFERENCES [ProduttoriMacchine] ([IdProduttore]) ON DELETE SET NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra'
)
BEGIN
    ALTER TABLE [Piastre] ADD CONSTRAINT [FK_Piastre_Clienti_IdClienteEsclusivo] FOREIGN KEY ([IdClienteEsclusivo]) REFERENCES [Clienti] ([IdCliente]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702064753_AddClienteAttivoGestionale'
)
BEGIN
    ALTER TABLE [Clienti] ADD [AttivoGestionale] bit NOT NULL DEFAULT CAST(1 AS bit);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260702064753_AddClienteAttivoGestionale'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260702064753_AddClienteAttivoGestionale', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703091911_MacchineDimensioniFoglio'
)
BEGIN
    EXEC sp_rename N'[MacchineStandard].[LarghezzaMm]', N'LarghezzaMassimaFoglioMm', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703091911_MacchineDimensioniFoglio'
)
BEGIN
    EXEC sp_rename N'[MacchineStandard].[AltezzaMm]', N'AltezzaMassimaFoglioMm', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703091911_MacchineDimensioniFoglio'
)
BEGIN
    ALTER TABLE [MacchineStandard] ADD [LarghezzaMinimaFoglioMm] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703091911_MacchineDimensioniFoglio'
)
BEGIN
    ALTER TABLE [MacchineStandard] ADD [AltezzaMinimaFoglioMm] decimal(18,2) NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703091911_MacchineDimensioniFoglio'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260703091911_MacchineDimensioniFoglio', N'8.0.0');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709080730_AssociazioneMacchinePiastre'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MacchineStandard]') AND [c].[name] = N'AltezzaMassimaFoglioMm');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [MacchineStandard] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [MacchineStandard] DROP COLUMN [AltezzaMassimaFoglioMm];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709080730_AssociazioneMacchinePiastre'
)
BEGIN
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MacchineStandard]') AND [c].[name] = N'AltezzaMinimaFoglioMm');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [MacchineStandard] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [MacchineStandard] DROP COLUMN [AltezzaMinimaFoglioMm];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709080730_AssociazioneMacchinePiastre'
)
BEGIN
    DECLARE @var9 sysname;
    SELECT @var9 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MacchineStandard]') AND [c].[name] = N'LarghezzaMassimaFoglioMm');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [MacchineStandard] DROP CONSTRAINT [' + @var9 + '];');
    ALTER TABLE [MacchineStandard] DROP COLUMN [LarghezzaMassimaFoglioMm];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709080730_AssociazioneMacchinePiastre'
)
BEGIN
    DECLARE @var10 sysname;
    SELECT @var10 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MacchineStandard]') AND [c].[name] = N'LarghezzaMinimaFoglioMm');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [MacchineStandard] DROP CONSTRAINT [' + @var10 + '];');
    ALTER TABLE [MacchineStandard] DROP COLUMN [LarghezzaMinimaFoglioMm];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709080730_AssociazioneMacchinePiastre'
)
BEGIN
    DECLARE @var11 sysname;
    SELECT @var11 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ClientiMacchine]') AND [c].[name] = N'Matricola');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [ClientiMacchine] DROP CONSTRAINT [' + @var11 + '];');
    ALTER TABLE [ClientiMacchine] DROP COLUMN [Matricola];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260709080730_AssociazioneMacchinePiastre'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260709080730_AssociazioneMacchinePiastre', N'8.0.0');
END;
GO

COMMIT;
GO

