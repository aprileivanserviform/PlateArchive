-- ============================================================================
-- Rimozione colonna AllegatiClienti.Descrizione (TASK-23)
-- ----------------------------------------------------------------------------
-- Il campo non veniva mai valorizzato: il caricamento allegato non chiede una
-- descrizione, quindi la colonna in griglia risultava sempre vuota. Rimosso il
-- campo dal modello, dalla griglia e dal database.
--
-- Applicare a mano sul DB di produzione (PlateArchiveDB).
-- Idempotente: si può rieseguire senza errori.
-- Corrisponde alla migrazione EF 20260724081513_RimuoviDescrizioneAllegato.
-- ============================================================================

USE PlateArchiveDB;
GO

-- 1) Colonna Descrizione
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE name = N'Descrizione' AND object_id = OBJECT_ID(N'[dbo].[AllegatiClienti]'))
BEGIN
    ALTER TABLE [dbo].[AllegatiClienti] DROP COLUMN [Descrizione];
    PRINT 'Colonna AllegatiClienti.Descrizione eliminata.';
END
GO

-- 2) Registra la migrazione nello storico EF
IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory]
                   WHERE [MigrationId] = N'20260724081513_RimuoviDescrizioneAllegato')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260724081513_RimuoviDescrizioneAllegato', N'9.0.0');
    PRINT 'Migrazione registrata in __EFMigrationsHistory.';
END
GO

PRINT 'TASK-23: rimozione descrizione allegato completata.';
GO
