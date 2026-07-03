-- =============================================================================
-- MIGRAZIONE: MacchineStandard — formato foglio lavorabile (min / max)
--
-- Sostituisce le vecchie dimensioni fisiche della macchina (LarghezzaMm / AltezzaMm)
-- con le dimensioni del foglio fustellabile: larghezza/altezza MINIMA e MASSIMA.
--
-- Le vecchie colonne vengono rinominate come dimensioni MASSIME (i valori esistenti
-- rappresentano il formato più grande gestito); le dimensioni minime sono nuove
-- colonne nullable.
--
-- Corrisponde alla migrazione EF Core 20260703091911_MacchineDimensioniFoglio.
-- Idempotente: eseguibile una sola volta senza errori se già applicata.
-- Applicare a mano sul DB condiviso.
-- =============================================================================

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- 1. Rinomina LarghezzaMm → LarghezzaMassimaFoglioMm
IF  COL_LENGTH('dbo.MacchineStandard', 'LarghezzaMm') IS NOT NULL
AND COL_LENGTH('dbo.MacchineStandard', 'LarghezzaMassimaFoglioMm') IS NULL
    EXEC sp_rename N'dbo.MacchineStandard.LarghezzaMm', N'LarghezzaMassimaFoglioMm', N'COLUMN';

-- 2. Rinomina AltezzaMm → AltezzaMassimaFoglioMm
IF  COL_LENGTH('dbo.MacchineStandard', 'AltezzaMm') IS NOT NULL
AND COL_LENGTH('dbo.MacchineStandard', 'AltezzaMassimaFoglioMm') IS NULL
    EXEC sp_rename N'dbo.MacchineStandard.AltezzaMm', N'AltezzaMassimaFoglioMm', N'COLUMN';

-- 3. Aggiunge LarghezzaMinimaFoglioMm
IF COL_LENGTH('dbo.MacchineStandard', 'LarghezzaMinimaFoglioMm') IS NULL
    ALTER TABLE dbo.MacchineStandard ADD LarghezzaMinimaFoglioMm decimal(18,2) NULL;

-- 4. Aggiunge AltezzaMinimaFoglioMm
IF COL_LENGTH('dbo.MacchineStandard', 'AltezzaMinimaFoglioMm') IS NULL
    ALTER TABLE dbo.MacchineStandard ADD AltezzaMinimaFoglioMm decimal(18,2) NULL;

-- 5. Registra la migrazione in __EFMigrationsHistory (se la tabella esiste)
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory]
                   WHERE [MigrationId] = N'20260703091911_MacchineDimensioniFoglio')
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260703091911_MacchineDimensioniFoglio', N'8.0.0');

COMMIT TRANSACTION;
GO
