-- ============================================================
-- Migrazione: DurezzaStandard  (idempotente — sicura da rieseguire)
-- Data:       2026-07-16
-- ============================================================

-- 1. Crea la tabella lookup (se non esiste già)
IF NOT EXISTS (
    SELECT 1 FROM sys.tables WHERE name = 'DurezzePiastre' AND schema_id = SCHEMA_ID('dbo')
)
BEGIN
    CREATE TABLE [dbo].[DurezzePiastre] (
        [IdDurezza]   INT            NOT NULL IDENTITY(1,1),
        [Valore]      NVARCHAR(MAX)  NOT NULL,
        [Note]        NVARCHAR(MAX)  NULL,
        [IsEliminata] BIT            NOT NULL DEFAULT 0,
        CONSTRAINT [PK_DurezzePiastre] PRIMARY KEY ([IdDurezza])
    );
    PRINT 'Tabella DurezzePiastre creata.';
END
ELSE
    PRINT 'Tabella DurezzePiastre già esistente — saltato.';

-- 2. Rimuove la vecchia colonna Durezza (se esiste ancora)
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[dbo].[Piastre]') AND name = 'Durezza'
)
BEGIN
    -- Prima rimuove eventuali vincoli DEFAULT legati alla colonna
    DECLARE @defName NVARCHAR(256);
    SELECT @defName = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON dc.parent_column_id = c.column_id
                      AND dc.parent_object_id = c.object_id
    WHERE c.object_id = OBJECT_ID('[dbo].[Piastre]') AND c.name = 'Durezza';
    IF @defName IS NOT NULL
        EXEC ('ALTER TABLE [dbo].[Piastre] DROP CONSTRAINT [' + @defName + ']');

    ALTER TABLE [dbo].[Piastre] DROP COLUMN [Durezza];
    PRINT 'Colonna Durezza rimossa da Piastre.';
END
ELSE
    PRINT 'Colonna Durezza non presente — saltato.';

-- 3. Aggiunge la FK nullable IdDurezza (se non esiste già)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[dbo].[Piastre]') AND name = 'IdDurezza'
)
BEGIN
    ALTER TABLE [dbo].[Piastre] ADD [IdDurezza] INT NULL;
    PRINT 'Colonna IdDurezza aggiunta a Piastre.';
END
ELSE
    PRINT 'Colonna IdDurezza già presente — saltato.';

-- 4. Crea l'indice (se non esiste già)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('[dbo].[Piastre]') AND name = 'IX_Piastre_IdDurezza'
)
BEGIN
    CREATE INDEX [IX_Piastre_IdDurezza] ON [dbo].[Piastre] ([IdDurezza]);
    PRINT 'Indice IX_Piastre_IdDurezza creato.';
END
ELSE
    PRINT 'Indice IX_Piastre_IdDurezza già esistente — saltato.';

-- 5. Aggiunge il vincolo FK (se non esiste già)
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_Piastre_DurezzePiastre_IdDurezza'
)
BEGIN
    ALTER TABLE [dbo].[Piastre]
        ADD CONSTRAINT [FK_Piastre_DurezzePiastre_IdDurezza]
            FOREIGN KEY ([IdDurezza])
            REFERENCES [dbo].[DurezzePiastre] ([IdDurezza])
            ON DELETE SET NULL;
    PRINT 'Vincolo FK_Piastre_DurezzePiastre_IdDurezza creato.';
END
ELSE
    PRINT 'Vincolo FK già esistente — saltato.';

-- 6. Aggiorna __EFMigrationsHistory (se non già registrata)
IF NOT EXISTS (
    SELECT 1 FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716122907_DurezzaStandard'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260716122907_DurezzaStandard', N'9.0.0');
    PRINT 'Record migrazione inserito in __EFMigrationsHistory.';
END
ELSE
    PRINT 'Migrazione già registrata in __EFMigrationsHistory — saltato.';
