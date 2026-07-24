-- ============================================================================
-- Rimozione colonna Piastre.CodiceArticoloGestionale (TASK-20)
-- ----------------------------------------------------------------------------
-- Il match ordini vendita ↔ piastra avviene ora per cliente + formato (TASK-18):
-- il codice articolo gestionale sulla piastra non è più usato ed è stato rimosso
-- dalla gestione. Applicare a mano sul DB di produzione (PlateArchiveDB).
--
-- Idempotente: si può rieseguire senza errori.
-- Corrisponde alla migrazione EF 20260722094803_RimuoviCodiceArticoloGestionalePiastra.
-- ============================================================================

USE PlateArchiveDB;
GO

-- 1) Elimina l'indice univoco filtrato, se presente
IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE name = N'IX_Piastre_CodiceArticoloGestionale'
             AND object_id = OBJECT_ID(N'[dbo].[Piastre]'))
BEGIN
    DROP INDEX [IX_Piastre_CodiceArticoloGestionale] ON [dbo].[Piastre];
    PRINT 'Indice IX_Piastre_CodiceArticoloGestionale eliminato.';
END
GO

-- 2) Elimina la colonna, se presente
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE name = N'CodiceArticoloGestionale'
             AND object_id = OBJECT_ID(N'[dbo].[Piastre]'))
BEGIN
    ALTER TABLE [dbo].[Piastre] DROP COLUMN [CodiceArticoloGestionale];
    PRINT 'Colonna Piastre.CodiceArticoloGestionale eliminata.';
END
GO

-- 3) Registra la migrazione nello storico EF (se si usa __EFMigrationsHistory)
IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory]
                   WHERE [MigrationId] = N'20260722094803_RimuoviCodiceArticoloGestionalePiastra')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260722094803_RimuoviCodiceArticoloGestionalePiastra', N'9.0.0');
    PRINT 'Migrazione registrata in __EFMigrationsHistory.';
END
GO

PRINT 'TASK-20: rimozione CodiceArticoloGestionale completata.';
GO
