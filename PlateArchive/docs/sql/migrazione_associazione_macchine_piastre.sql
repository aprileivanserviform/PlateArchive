-- =============================================================================
-- MIGRAZIONE: Associazione Macchine ↔ Piastre
--
-- 1. Rimuove le dimensioni foglio min/max da MacchineStandard: le misure ora
--    derivano (sola lettura) dalla piastra Standard associata alla macchina.
-- 2. Rimuove Matricola da ClientiMacchine (dato ininfluente).
--
-- Corrisponde alla migrazione EF Core 20260709080730_AssociazioneMacchinePiastre.
-- Idempotente: eseguibile una sola volta senza errori se già applicata.
-- Applicare a mano sul DB condiviso.
-- =============================================================================

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- 1. Rimuove le 4 colonne del formato foglio da MacchineStandard
IF COL_LENGTH('dbo.MacchineStandard', 'AltezzaMassimaFoglioMm') IS NOT NULL
    ALTER TABLE dbo.MacchineStandard DROP COLUMN AltezzaMassimaFoglioMm;

IF COL_LENGTH('dbo.MacchineStandard', 'AltezzaMinimaFoglioMm') IS NOT NULL
    ALTER TABLE dbo.MacchineStandard DROP COLUMN AltezzaMinimaFoglioMm;

IF COL_LENGTH('dbo.MacchineStandard', 'LarghezzaMassimaFoglioMm') IS NOT NULL
    ALTER TABLE dbo.MacchineStandard DROP COLUMN LarghezzaMassimaFoglioMm;

IF COL_LENGTH('dbo.MacchineStandard', 'LarghezzaMinimaFoglioMm') IS NOT NULL
    ALTER TABLE dbo.MacchineStandard DROP COLUMN LarghezzaMinimaFoglioMm;

-- 2. Rimuove Matricola da ClientiMacchine
IF COL_LENGTH('dbo.ClientiMacchine', 'Matricola') IS NOT NULL
    ALTER TABLE dbo.ClientiMacchine DROP COLUMN Matricola;

-- 3. Registra la migrazione in __EFMigrationsHistory (se la tabella esiste)
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory]
                   WHERE [MigrationId] = N'20260709080730_AssociazioneMacchinePiastre')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260709080730_AssociazioneMacchinePiastre', N'8.0.0');

COMMIT TRANSACTION;
GO
