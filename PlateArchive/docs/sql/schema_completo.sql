-- =============================================================================
-- SCHEMA COMPLETO  PlateArchiveDB  (SQL Server)
-- Generato: 2026-07-16
-- Stato:    riflette tutte le migrazioni EF Core fino a 20260716122907_DurezzaStandard
--
-- IDEMPOTENTE: ogni istruzione è protetta da IF NOT EXISTS / IF EXISTS.
-- Sicuro da rieseguire su un DB già parzialmente configurato.
-- NON inserisce dati nelle tabelle già compilate (FormatiMacchine, ProduttoriMacchine,
-- MacchineStandard): lo schema è strutturale, non sovrascrive l'anagrafica esistente.
-- =============================================================================

USE [PlateArchiveDB];
GO

-- =============================================================================
-- 0. TABELLA MIGRAZIONI EF CORE
-- =============================================================================

IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory] (
        [MigrationId]    nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32)  NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
    PRINT 'Creata __EFMigrationsHistory.';
END
GO

-- =============================================================================
-- 1. TABELLE LOOKUP (senza FK esterne)
-- =============================================================================

-- CategoriePiastre -----------------------------------------------------------
IF OBJECT_ID(N'[dbo].[CategoriePiastre]') IS NULL
BEGIN
    CREATE TABLE [dbo].[CategoriePiastre] (
        [IdCategoriaPiastra] int          NOT NULL IDENTITY(1,1),
        [Codice]             nvarchar(450) NOT NULL,
        [Descrizione]        nvarchar(max) NOT NULL,
        [Ordine]             int           NOT NULL,
        CONSTRAINT [PK_CategoriePiastre] PRIMARY KEY ([IdCategoriaPiastra])
    );
    CREATE UNIQUE INDEX [IX_CategoriePiastre_Codice] ON [dbo].[CategoriePiastre] ([Codice]);

    -- Seed: valori minimi necessari al funzionamento dell'app
    SET IDENTITY_INSERT [dbo].[CategoriePiastre] ON;
    INSERT INTO [dbo].[CategoriePiastre] ([IdCategoriaPiastra], [Codice], [Descrizione], [Ordine])
    VALUES (1, N'STD', N'Standard', 1),
           (2, N'SPE', N'Speciale', 2);
    SET IDENTITY_INSERT [dbo].[CategoriePiastre] OFF;

    PRINT 'Creata CategoriePiastre con seed STD/SPE.';
END
ELSE
    PRINT 'CategoriePiastre già esistente — saltato.';
GO

-- FormatiMacchine -------------------------------------------------------------
IF OBJECT_ID(N'[dbo].[FormatiMacchine]') IS NULL
BEGIN
    CREATE TABLE [dbo].[FormatiMacchine] (
        [IdFormato]    int           NOT NULL IDENTITY(1,1),
        [NomeFormato]  nvarchar(max) NOT NULL,
        [IsEliminata]  bit           NOT NULL DEFAULT 0,
        [Note]         nvarchar(max) NULL,
        CONSTRAINT [PK_FormatiMacchine] PRIMARY KEY ([IdFormato])
    );
    PRINT 'Creata FormatiMacchine.';
END
ELSE
    PRINT 'FormatiMacchine già esistente — saltato (dati esistenti preservati).';
GO

-- ProduttoriMacchine ----------------------------------------------------------
IF OBJECT_ID(N'[dbo].[ProduttoriMacchine]') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProduttoriMacchine] (
        [IdProduttore]   int           NOT NULL IDENTITY(1,1),
        [NomeProduttore] nvarchar(max) NOT NULL,
        [IsEliminata]    bit           NOT NULL DEFAULT 0,
        [Note]           nvarchar(max) NULL,
        CONSTRAINT [PK_ProduttoriMacchine] PRIMARY KEY ([IdProduttore])
    );
    PRINT 'Creata ProduttoriMacchine.';
END
ELSE
    PRINT 'ProduttoriMacchine già esistente — saltato (dati esistenti preservati).';
GO

-- DurezzePiastre --------------------------------------------------------------
IF OBJECT_ID(N'[dbo].[DurezzePiastre]') IS NULL
BEGIN
    CREATE TABLE [dbo].[DurezzePiastre] (
        [IdDurezza]   int           NOT NULL IDENTITY(1,1),
        [Valore]      nvarchar(max) NOT NULL,
        [Note]        nvarchar(max) NULL,
        [IsEliminata] bit           NOT NULL DEFAULT 0,
        CONSTRAINT [PK_DurezzePiastre] PRIMARY KEY ([IdDurezza])
    );
    PRINT 'Creata DurezzePiastre.';
END
ELSE
    PRINT 'DurezzePiastre già esistente — saltato (dati esistenti preservati).';
GO

-- =============================================================================
-- 2. CLIENTI
-- =============================================================================

IF OBJECT_ID(N'[dbo].[Clienti]') IS NULL
BEGIN
    CREATE TABLE [dbo].[Clienti] (
        [IdCliente]                int           NOT NULL IDENTITY(1,1),
        [CodiceClienteGestionale]  nvarchar(450) NOT NULL,
        [RagioneSociale]           nvarchar(max) NOT NULL,
        [Note]                     nvarchar(max) NULL,
        [AttivoGestionale]         bit           NOT NULL DEFAULT 1,
        CONSTRAINT [PK_Clienti] PRIMARY KEY ([IdCliente])
    );
    CREATE UNIQUE INDEX [IX_Clienti_CodiceClienteGestionale]
        ON [dbo].[Clienti] ([CodiceClienteGestionale]);
    PRINT 'Creata Clienti.';
END
ELSE
    PRINT 'Clienti già esistente — saltato.';
GO

-- =============================================================================
-- 3. MACCHINE STANDARD
-- =============================================================================

IF OBJECT_ID(N'[dbo].[MacchineStandard]') IS NULL
BEGIN
    CREATE TABLE [dbo].[MacchineStandard] (
        [IdMacchinaStandard] int           NOT NULL IDENTITY(1,1),
        [CodiceMacchina]     nvarchar(450) NOT NULL,
        [NomeMacchina]       nvarchar(max) NOT NULL,
        [Versione]           nvarchar(max) NULL,
        [Attiva]             bit           NOT NULL DEFAULT 1,
        [Note]               nvarchar(max) NULL,
        [IdFormato]          int           NULL,
        [IdProduttore]       int           NULL,
        CONSTRAINT [PK_MacchineStandard] PRIMARY KEY ([IdMacchinaStandard]),
        CONSTRAINT [FK_MacchineStandard_FormatiMacchine_IdFormato]
            FOREIGN KEY ([IdFormato]) REFERENCES [dbo].[FormatiMacchine] ([IdFormato]),
        CONSTRAINT [FK_MacchineStandard_ProduttoriMacchine_IdProduttore]
            FOREIGN KEY ([IdProduttore]) REFERENCES [dbo].[ProduttoriMacchine] ([IdProduttore])
            ON DELETE SET NULL
    );
    CREATE UNIQUE INDEX [IX_MacchineStandard_CodiceMacchina]
        ON [dbo].[MacchineStandard] ([CodiceMacchina]);
    CREATE INDEX [IX_MacchineStandard_IdFormato]
        ON [dbo].[MacchineStandard] ([IdFormato]);
    CREATE INDEX [IX_MacchineStandard_IdProduttore]
        ON [dbo].[MacchineStandard] ([IdProduttore]);
    PRINT 'Creata MacchineStandard.';
END
ELSE
    PRINT 'MacchineStandard già esistente — saltato (dati esistenti preservati).';
GO

-- =============================================================================
-- 4. PIASTRE
-- =============================================================================

IF OBJECT_ID(N'[dbo].[Piastre]') IS NULL
BEGIN
    CREATE TABLE [dbo].[Piastre] (
        [IdPiastra]                int            NOT NULL IDENTITY(1,1),
        [CodicePiastra]            nvarchar(450)  NOT NULL,
        [CodiceArticoloGestionale] nvarchar(450)  NULL,
        [Descrizione]              nvarchar(max)  NULL,
        [Stato]                    int            NOT NULL DEFAULT 0,
        [TipoPiastra]              int            NOT NULL DEFAULT 0,
        [IdCategoriaPiastra]       int            NULL,
        [IdFormato]                int            NULL,
        [IdClienteEsclusivo]       int            NULL,
        [IdDurezza]                int            NULL,
        [IsEliminata]              bit            NOT NULL DEFAULT 0,
        [LarghezzaMm]              decimal(18,2)  NULL,
        [AltezzaMm]                decimal(18,2)  NULL,
        [SpessoreMm]               decimal(18,2)  NULL,
        [Peso]                     decimal(18,2)  NULL,
        [Note]                     nvarchar(max)  NULL,
        [DataCreazione]            datetime2      NOT NULL DEFAULT SYSUTCDATETIME(),
        [DataUltimaModifica]       datetime2      NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_Piastre] PRIMARY KEY ([IdPiastra]),
        CONSTRAINT [FK_Piastre_CategoriePiastre_IdCategoriaPiastra]
            FOREIGN KEY ([IdCategoriaPiastra]) REFERENCES [dbo].[CategoriePiastre] ([IdCategoriaPiastra])
            ON DELETE SET NULL,
        CONSTRAINT [FK_Piastre_FormatiMacchine_IdFormato]
            FOREIGN KEY ([IdFormato]) REFERENCES [dbo].[FormatiMacchine] ([IdFormato]),
        CONSTRAINT [FK_Piastre_Clienti_IdClienteEsclusivo]
            FOREIGN KEY ([IdClienteEsclusivo]) REFERENCES [dbo].[Clienti] ([IdCliente]),
        CONSTRAINT [FK_Piastre_DurezzePiastre_IdDurezza]
            FOREIGN KEY ([IdDurezza]) REFERENCES [dbo].[DurezzePiastre] ([IdDurezza])
            ON DELETE SET NULL
    );
    CREATE UNIQUE INDEX [IX_Piastre_CodicePiastra]
        ON [dbo].[Piastre] ([CodicePiastra]);
    EXEC(N'CREATE UNIQUE INDEX [IX_Piastre_CodiceArticoloGestionale]
        ON [dbo].[Piastre] ([CodiceArticoloGestionale])
        WHERE [CodiceArticoloGestionale] IS NOT NULL');
    CREATE INDEX [IX_Piastre_IdCategoriaPiastra]
        ON [dbo].[Piastre] ([IdCategoriaPiastra]);
    CREATE INDEX [IX_Piastre_IdFormato]
        ON [dbo].[Piastre] ([IdFormato]);
    CREATE INDEX [IX_Piastre_IdClienteEsclusivo]
        ON [dbo].[Piastre] ([IdClienteEsclusivo]);
    CREATE INDEX [IX_Piastre_IdDurezza]
        ON [dbo].[Piastre] ([IdDurezza]);
    PRINT 'Creata Piastre.';
END
ELSE
    PRINT 'Piastre già esistente — saltato.';
GO

-- =============================================================================
-- 5. DISEGNI  (1:1 con Piastra — FK nullable, UNIQUE WHERE NOT NULL)
-- =============================================================================

IF OBJECT_ID(N'[dbo].[Disegni]') IS NULL
BEGIN
    CREATE TABLE [dbo].[Disegni] (
        [IdDisegno]              int           NOT NULL IDENTITY(1,1),
        [IdPiastra]              int           NULL,
        [CodiceDisegno]          nvarchar(max) NULL,
        [NomeFile]               nvarchar(max) NULL,
        [PercorsoFile]           nvarchar(max) NULL,
        [VaultId]                nvarchar(max) NULL,
        [Revisione]              nvarchar(max) NULL,
        [Formato]                nvarchar(max) NULL,
        [Stato]                  int           NOT NULL DEFAULT 0,
        [DataUltimaModificaFile] datetime2     NULL,
        [Note]                   nvarchar(max) NULL,
        CONSTRAINT [PK_Disegni] PRIMARY KEY ([IdDisegno]),
        CONSTRAINT [FK_Disegni_Piastre_IdPiastra]
            FOREIGN KEY ([IdPiastra]) REFERENCES [dbo].[Piastre] ([IdPiastra])
            ON DELETE SET NULL
    );
    EXEC(N'CREATE UNIQUE INDEX [IX_Disegni_IdPiastra]
        ON [dbo].[Disegni] ([IdPiastra])
        WHERE [IdPiastra] IS NOT NULL');
    PRINT 'Creata Disegni.';
END
ELSE
    PRINT 'Disegni già esistente — saltato.';
GO

-- =============================================================================
-- 6. PIASTRE ↔ MACCHINE COMPATIBILI  (N:N)
-- =============================================================================

IF OBJECT_ID(N'[dbo].[PiastreMacchineCompatibili]') IS NULL
BEGIN
    CREATE TABLE [dbo].[PiastreMacchineCompatibili] (
        [IdCompatibilita]    int           NOT NULL IDENTITY(1,1),
        [IdPiastra]          int           NOT NULL,
        [IdMacchinaStandard] int           NOT NULL,
        [FonteDato]          int           NULL,
        [DataVerifica]       datetime2     NULL,
        [UtenteVerifica]     nvarchar(max) NULL,
        [Attiva]             bit           NOT NULL DEFAULT 1,
        [Note]               nvarchar(max) NULL,
        CONSTRAINT [PK_PiastreMacchineCompatibili] PRIMARY KEY ([IdCompatibilita]),
        CONSTRAINT [FK_PiastreMacchineCompatibili_Piastre_IdPiastra]
            FOREIGN KEY ([IdPiastra]) REFERENCES [dbo].[Piastre] ([IdPiastra])
            ON DELETE CASCADE,
        CONSTRAINT [FK_PiastreMacchineCompatibili_MacchineStandard_IdMacchinaStandard]
            FOREIGN KEY ([IdMacchinaStandard]) REFERENCES [dbo].[MacchineStandard] ([IdMacchinaStandard])
            ON DELETE CASCADE
    );
    CREATE INDEX [IX_PiastreMacchineCompatibili_IdPiastra]
        ON [dbo].[PiastreMacchineCompatibili] ([IdPiastra]);
    CREATE INDEX [IX_PiastreMacchineCompatibili_IdMacchinaStandard]
        ON [dbo].[PiastreMacchineCompatibili] ([IdMacchinaStandard]);
    CREATE UNIQUE INDEX [IX_PiastreMacchineCompatibili_IdPiastra_IdMacchinaStandard]
        ON [dbo].[PiastreMacchineCompatibili] ([IdPiastra], [IdMacchinaStandard]);
    PRINT 'Creata PiastreMacchineCompatibili.';
END
ELSE
    PRINT 'PiastreMacchineCompatibili già esistente — saltato.';
GO

-- =============================================================================
-- 7. CLIENTI ↔ MACCHINE  (anagrafica commerciale)
-- =============================================================================

IF OBJECT_ID(N'[dbo].[ClientiMacchine]') IS NULL
BEGIN
    CREATE TABLE [dbo].[ClientiMacchine] (
        [IdClienteMacchina]    int           NOT NULL IDENTITY(1,1),
        [IdCliente]            int           NOT NULL,
        [IdMacchinaStandard]   int           NOT NULL,
        [CodiceInternoCliente] nvarchar(max) NULL,
        [DataAssociazione]     datetime2     NOT NULL DEFAULT SYSUTCDATETIME(),
        [Attiva]               bit           NOT NULL DEFAULT 1,
        [Note]                 nvarchar(max) NULL,
        CONSTRAINT [PK_ClientiMacchine] PRIMARY KEY ([IdClienteMacchina]),
        CONSTRAINT [FK_ClientiMacchine_Clienti_IdCliente]
            FOREIGN KEY ([IdCliente]) REFERENCES [dbo].[Clienti] ([IdCliente])
            ON DELETE CASCADE,
        CONSTRAINT [FK_ClientiMacchine_MacchineStandard_IdMacchinaStandard]
            FOREIGN KEY ([IdMacchinaStandard]) REFERENCES [dbo].[MacchineStandard] ([IdMacchinaStandard])
            ON DELETE CASCADE
    );
    CREATE INDEX [IX_ClientiMacchine_IdCliente]
        ON [dbo].[ClientiMacchine] ([IdCliente]);
    CREATE INDEX [IX_ClientiMacchine_IdMacchinaStandard]
        ON [dbo].[ClientiMacchine] ([IdMacchinaStandard]);
    PRINT 'Creata ClientiMacchine.';
END
ELSE
    PRINT 'ClientiMacchine già esistente — saltato.';
GO

-- =============================================================================
-- 8. CLIENTI ↔ PIASTRE  (associazione commerciale)
-- =============================================================================

IF OBJECT_ID(N'[dbo].[ClientiPiastre]') IS NULL
BEGIN
    CREATE TABLE [dbo].[ClientiPiastre] (
        [IdClientePiastra]  int           NOT NULL IDENTITY(1,1),
        [IdCliente]         int           NOT NULL,
        [IdPiastra]         int           NOT NULL,
        [IdClienteMacchina] int           NULL,
        [DataAssociazione]  datetime2     NOT NULL DEFAULT SYSUTCDATETIME(),
        [Stato]             int           NOT NULL DEFAULT 0,
        [Note]              nvarchar(max) NULL,
        CONSTRAINT [PK_ClientiPiastre] PRIMARY KEY ([IdClientePiastra]),
        CONSTRAINT [FK_ClientiPiastre_Clienti_IdCliente]
            FOREIGN KEY ([IdCliente]) REFERENCES [dbo].[Clienti] ([IdCliente])
            ON DELETE CASCADE,
        CONSTRAINT [FK_ClientiPiastre_Piastre_IdPiastra]
            FOREIGN KEY ([IdPiastra]) REFERENCES [dbo].[Piastre] ([IdPiastra])
            ON DELETE CASCADE,
        CONSTRAINT [FK_ClientiPiastre_ClientiMacchine_IdClienteMacchina]
            FOREIGN KEY ([IdClienteMacchina]) REFERENCES [dbo].[ClientiMacchine] ([IdClienteMacchina])
    );
    CREATE INDEX [IX_ClientiPiastre_IdCliente]
        ON [dbo].[ClientiPiastre] ([IdCliente]);
    CREATE INDEX [IX_ClientiPiastre_IdPiastra]
        ON [dbo].[ClientiPiastre] ([IdPiastra]);
    CREATE INDEX [IX_ClientiPiastre_IdClienteMacchina]
        ON [dbo].[ClientiPiastre] ([IdClienteMacchina]);
    CREATE UNIQUE INDEX [IX_ClientiPiastre_IdCliente_IdPiastra]
        ON [dbo].[ClientiPiastre] ([IdCliente], [IdPiastra]);
    PRINT 'Creata ClientiPiastre.';
END
ELSE
    PRINT 'ClientiPiastre già esistente — saltato.';
GO

-- =============================================================================
-- 9. ALLEGATI CLIENTE
-- =============================================================================

IF OBJECT_ID(N'[dbo].[AllegatiClienti]') IS NULL
BEGIN
    CREATE TABLE [dbo].[AllegatiClienti] (
        [IdAllegato]       int           NOT NULL IDENTITY(1,1),
        [IdCliente]        int           NOT NULL,
        [NomeFile]         nvarchar(max) NOT NULL,
        [PercorsoFile]     nvarchar(max) NOT NULL,
        [Descrizione]      nvarchar(max) NULL,
        [DimensioneBytes]  bigint        NOT NULL DEFAULT 0,
        [DataCaricamento]  datetime2     NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_AllegatiClienti] PRIMARY KEY ([IdAllegato]),
        CONSTRAINT [FK_AllegatiClienti_Clienti_IdCliente]
            FOREIGN KEY ([IdCliente]) REFERENCES [dbo].[Clienti] ([IdCliente])
            ON DELETE CASCADE
    );
    CREATE INDEX [IX_AllegatiClienti_IdCliente]
        ON [dbo].[AllegatiClienti] ([IdCliente]);
    PRINT 'Creata AllegatiClienti.';
END
ELSE
    PRINT 'AllegatiClienti già esistente — saltato.';
GO

-- =============================================================================
-- 10. NOTE TECNICHE CLIENTE
-- =============================================================================

IF OBJECT_ID(N'[dbo].[NoteTecnicheClienti]') IS NULL
BEGIN
    CREATE TABLE [dbo].[NoteTecnicheClienti] (
        [IdNota]        int           NOT NULL IDENTITY(1,1),
        [IdCliente]     int           NOT NULL,
        [Titolo]        nvarchar(max) NOT NULL,
        [Testo]         nvarchar(max) NULL,
        [DataCreazione] datetime2     NOT NULL DEFAULT SYSUTCDATETIME(),
        [DataModifica]  datetime2     NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT [PK_NoteTecnicheClienti] PRIMARY KEY ([IdNota]),
        CONSTRAINT [FK_NoteTecnicheClienti_Clienti_IdCliente]
            FOREIGN KEY ([IdCliente]) REFERENCES [dbo].[Clienti] ([IdCliente])
            ON DELETE CASCADE
    );
    CREATE INDEX [IX_NoteTecnicheClienti_IdCliente]
        ON [dbo].[NoteTecnicheClienti] ([IdCliente]);
    PRINT 'Creata NoteTecnicheClienti.';
END
ELSE
    PRINT 'NoteTecnicheClienti già esistente — saltato.';
GO

-- =============================================================================
-- 11. REGISTRAZIONE MIGRAZIONI EF CORE  (necessarie perché dotnet ef funzioni)
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260625140000_InitialCreate')
    INSERT INTO [dbo].[__EFMigrationsHistory] VALUES (N'20260625140000_InitialCreate', N'9.0.0');

IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260625160000_AddProduttoriFamiglieMacchine')
    INSERT INTO [dbo].[__EFMigrationsHistory] VALUES (N'20260625160000_AddProduttoriFamiglieMacchine', N'9.0.0');

IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260625180000_RinominaFamiglieInFormatiAddFormatoPiastra')
    INSERT INTO [dbo].[__EFMigrationsHistory] VALUES (N'20260625180000_RinominaFamiglieInFormatiAddFormatoPiastra', N'9.0.0');

IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra')
    INSERT INTO [dbo].[__EFMigrationsHistory] VALUES (N'20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra', N'9.0.0');

IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260702064753_AddClienteAttivoGestionale')
    INSERT INTO [dbo].[__EFMigrationsHistory] VALUES (N'20260702064753_AddClienteAttivoGestionale', N'9.0.0');

IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260703091911_MacchineDimensioniFoglio')
    INSERT INTO [dbo].[__EFMigrationsHistory] VALUES (N'20260703091911_MacchineDimensioniFoglio', N'9.0.0');

IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260709080730_AssociazioneMacchinePiastre')
    INSERT INTO [dbo].[__EFMigrationsHistory] VALUES (N'20260709080730_AssociazioneMacchinePiastre', N'9.0.0');

IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260716063245_AllegatiNoteCliente')
    INSERT INTO [dbo].[__EFMigrationsHistory] VALUES (N'20260716063245_AllegatiNoteCliente', N'9.0.0');

IF NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = N'20260716122907_DurezzaStandard')
    INSERT INTO [dbo].[__EFMigrationsHistory] VALUES (N'20260716122907_DurezzaStandard', N'9.0.0');

GO
PRINT 'Schema PlateArchiveDB aggiornato.';
GO
