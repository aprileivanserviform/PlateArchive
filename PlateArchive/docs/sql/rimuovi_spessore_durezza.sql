-- ============================================================================
-- Rimozione spessore e durezza dalla piastra (TASK-21)
-- ----------------------------------------------------------------------------
-- Spessore e durezza sono dati di prodotto del gestionale: lo stesso disegno serve
-- più spessori/durezze dello stesso formato. PlateArchive gestisce solo i disegni
-- delle piastre del cliente, quindi i due campi (e la tabella lookup DurezzePiastre)
-- vengono eliminati.
--
-- Applicare a mano sul DB di produzione (PlateArchiveDB), DOPO
-- elimina_piastre_senza_formato.sql.
--
-- Idempotente: si può rieseguire senza errori.
-- Corrisponde alla migrazione EF 20260722143719_RimuoviSpessoreDurezzaPiastra.
-- ============================================================================

USE PlateArchiveDB;
GO

-- 1) FK Piastre → DurezzePiastre
IF EXISTS (SELECT 1 FROM sys.foreign_keys
           WHERE name = N'FK_Piastre_DurezzePiastre_IdDurezza'
             AND parent_object_id = OBJECT_ID(N'[dbo].[Piastre]'))
BEGIN
    ALTER TABLE [dbo].[Piastre] DROP CONSTRAINT [FK_Piastre_DurezzePiastre_IdDurezza];
    PRINT 'FK FK_Piastre_DurezzePiastre_IdDurezza eliminata.';
END
GO

-- 2) Indice su IdDurezza
IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE name = N'IX_Piastre_IdDurezza'
             AND object_id = OBJECT_ID(N'[dbo].[Piastre]'))
BEGIN
    DROP INDEX [IX_Piastre_IdDurezza] ON [dbo].[Piastre];
    PRINT 'Indice IX_Piastre_IdDurezza eliminato.';
END
GO

-- 3) Colonne IdDurezza e SpessoreMm
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE name = N'IdDurezza' AND object_id = OBJECT_ID(N'[dbo].[Piastre]'))
BEGIN
    ALTER TABLE [dbo].[Piastre] DROP COLUMN [IdDurezza];
    PRINT 'Colonna Piastre.IdDurezza eliminata.';
END
GO

IF EXISTS (SELECT 1 FROM sys.columns
           WHERE name = N'SpessoreMm' AND object_id = OBJECT_ID(N'[dbo].[Piastre]'))
BEGIN
    ALTER TABLE [dbo].[Piastre] DROP COLUMN [SpessoreMm];
    PRINT 'Colonna Piastre.SpessoreMm eliminata.';
END
GO

-- 4) Tabella lookup DurezzePiastre
IF OBJECT_ID(N'[dbo].[DurezzePiastre]') IS NOT NULL
BEGIN
    DROP TABLE [dbo].[DurezzePiastre];
    PRINT 'Tabella DurezzePiastre eliminata.';
END
GO

-- 5) Registra la migrazione nello storico EF
IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory]
                   WHERE [MigrationId] = N'20260722143719_RimuoviSpessoreDurezzaPiastra')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260722143719_RimuoviSpessoreDurezzaPiastra', N'9.0.0');
    PRINT 'Migrazione registrata in __EFMigrationsHistory.';
END
GO

PRINT 'TASK-21: rimozione spessore/durezza completata.';
GO
