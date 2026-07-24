-- ============================================================================
-- Rimozione del peso dalla piastra (TASK-22)
-- ----------------------------------------------------------------------------
-- Come spessore e durezza (TASK-21), il peso è un dato di prodotto del gestionale:
-- PlateArchive gestisce solo i disegni delle piastre del cliente. La colonna viene
-- quindi eliminata.
--
-- Applicare a mano sul DB di produzione (PlateArchiveDB).
-- Idempotente: si può rieseguire senza errori.
-- Corrisponde alla migrazione EF 20260723061212_RimuoviPesoPiastra.
-- ============================================================================

USE PlateArchiveDB;
GO

-- 1) Colonna Peso
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE name = N'Peso' AND object_id = OBJECT_ID(N'[dbo].[Piastre]'))
BEGIN
    ALTER TABLE [dbo].[Piastre] DROP COLUMN [Peso];
    PRINT 'Colonna Piastre.Peso eliminata.';
END
GO

-- 2) Registra la migrazione nello storico EF
IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [dbo].[__EFMigrationsHistory]
                   WHERE [MigrationId] = N'20260723061212_RimuoviPesoPiastra')
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260723061212_RimuoviPesoPiastra', N'9.0.0');
    PRINT 'Migrazione registrata in __EFMigrationsHistory.';
END
GO

PRINT 'TASK-22: rimozione peso completata.';
GO
