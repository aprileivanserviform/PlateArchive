-- =============================================================================
-- MIGRAZIONE INCREMENTALE
-- PlateArchive — migrazione 20260625180000_RinominaFamiglieInFormatiAddFormatoPiastra
--
-- Applicare su un database che ha già le prime due migrazioni applicate:
--   20260625140000_InitialCreate
--   20260625160000_AddProduttoriFamiglieMacchine
--
-- La migrazione è IDEMPOTENTE: controlla ogni step prima di eseguirlo.
-- Eseguire in SSMS con il database PlateArchiveDB selezionato.
-- =============================================================================

USE PlateArchiveDB;
GO

BEGIN TRANSACTION;
BEGIN TRY

    PRINT '=== Inizio migrazione 20260625180000 ===';

    -- =========================================================================
    -- STEP 1 — Rimuovi FK MacchineStandard -> FamiglieMacchine
    -- =========================================================================
    IF EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = 'FK_MacchineStandard_FamiglieMacchine_IdFamiglia'
          AND parent_object_id = OBJECT_ID('MacchineStandard')
    )
    BEGIN
        ALTER TABLE MacchineStandard
            DROP CONSTRAINT FK_MacchineStandard_FamiglieMacchine_IdFamiglia;
        PRINT '  [OK] FK_MacchineStandard_FamiglieMacchine_IdFamiglia rimosso.';
    END
    ELSE PRINT '  [SKIP] FK_MacchineStandard_FamiglieMacchine_IdFamiglia gia assente.';

    -- =========================================================================
    -- STEP 2 — Rimuovi indici che referenziano i vecchi nomi
    -- =========================================================================
    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_FamiglieMacchine_NomeFamiglia'
          AND object_id = OBJECT_ID('FamiglieMacchine')
    )
    BEGIN
        DROP INDEX IX_FamiglieMacchine_NomeFamiglia ON FamiglieMacchine;
        PRINT '  [OK] IX_FamiglieMacchine_NomeFamiglia rimosso.';
    END
    ELSE PRINT '  [SKIP] IX_FamiglieMacchine_NomeFamiglia gia assente.';

    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_MacchineStandard_IdFamiglia'
          AND object_id = OBJECT_ID('MacchineStandard')
    )
    BEGIN
        DROP INDEX IX_MacchineStandard_IdFamiglia ON MacchineStandard;
        PRINT '  [OK] IX_MacchineStandard_IdFamiglia rimosso.';
    END
    ELSE PRINT '  [SKIP] IX_MacchineStandard_IdFamiglia gia assente.';

    -- =========================================================================
    -- STEP 3 — Rinomina tabella FamiglieMacchine -> FormatiMacchine
    -- =========================================================================
    IF OBJECT_ID('FamiglieMacchine', 'U') IS NOT NULL
       AND OBJECT_ID('FormatiMacchine', 'U') IS NULL
    BEGIN
        EXEC sp_rename 'FamiglieMacchine', 'FormatiMacchine';
        PRINT '  [OK] Tabella rinominata: FamiglieMacchine -> FormatiMacchine.';
    END
    ELSE IF OBJECT_ID('FormatiMacchine', 'U') IS NOT NULL
        PRINT '  [SKIP] FormatiMacchine esiste gia.';
    ELSE
        PRINT '  [WARN] FamiglieMacchine non trovata (gia rinominata?).';

    -- =========================================================================
    -- STEP 4 — Rinomina colonne in FormatiMacchine
    -- =========================================================================
    IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('FormatiMacchine') AND name = 'IdFamiglia'
    )
    BEGIN
        EXEC sp_rename 'FormatiMacchine.IdFamiglia', 'IdFormato', 'COLUMN';
        PRINT '  [OK] FormatiMacchine.IdFamiglia -> IdFormato.';
    END
    ELSE PRINT '  [SKIP] FormatiMacchine.IdFormato esiste gia.';

    IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('FormatiMacchine') AND name = 'NomeFamiglia'
    )
    BEGIN
        EXEC sp_rename 'FormatiMacchine.NomeFamiglia', 'NomeFormato', 'COLUMN';
        PRINT '  [OK] FormatiMacchine.NomeFamiglia -> NomeFormato.';
    END
    ELSE PRINT '  [SKIP] FormatiMacchine.NomeFormato esiste gia.';

    -- =========================================================================
    -- STEP 5 — Rinomina colonna MacchineStandard.IdFamiglia -> IdFormato
    -- =========================================================================
    IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('MacchineStandard') AND name = 'IdFamiglia'
    )
    BEGIN
        EXEC sp_rename 'MacchineStandard.IdFamiglia', 'IdFormato', 'COLUMN';
        PRINT '  [OK] MacchineStandard.IdFamiglia -> IdFormato.';
    END
    ELSE PRINT '  [SKIP] MacchineStandard.IdFormato esiste gia.';

    -- =========================================================================
    -- STEP 6 — Rinomina vincolo PK (cosmetico, non bloccante)
    -- =========================================================================
    IF EXISTS (
        SELECT 1 FROM sys.key_constraints
        WHERE name = 'PK_FamiglieMacchine'
          AND parent_object_id = OBJECT_ID('FormatiMacchine')
    )
    BEGIN
        EXEC sp_rename 'PK_FamiglieMacchine', 'PK_FormatiMacchine';
        PRINT '  [OK] Vincolo PK rinominato: PK_FamiglieMacchine -> PK_FormatiMacchine.';
    END
    ELSE PRINT '  [SKIP] PK_FormatiMacchine gia aggiornato.';

    -- =========================================================================
    -- STEP 7 — Ricrea indici con i nuovi nomi
    -- =========================================================================
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_FormatiMacchine_NomeFormato'
          AND object_id = OBJECT_ID('FormatiMacchine')
    )
    BEGIN
        CREATE UNIQUE INDEX IX_FormatiMacchine_NomeFormato
            ON FormatiMacchine (NomeFormato);
        PRINT '  [OK] IX_FormatiMacchine_NomeFormato creato.';
    END
    ELSE PRINT '  [SKIP] IX_FormatiMacchine_NomeFormato esiste gia.';

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_MacchineStandard_IdFormato'
          AND object_id = OBJECT_ID('MacchineStandard')
    )
    BEGIN
        CREATE INDEX IX_MacchineStandard_IdFormato
            ON MacchineStandard (IdFormato);
        PRINT '  [OK] IX_MacchineStandard_IdFormato creato.';
    END
    ELSE PRINT '  [SKIP] IX_MacchineStandard_IdFormato esiste gia.';

    -- =========================================================================
    -- STEP 8 — Ricrea FK MacchineStandard -> FormatiMacchine
    -- =========================================================================
    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = 'FK_MacchineStandard_FormatiMacchine_IdFormato'
    )
    BEGIN
        ALTER TABLE MacchineStandard
            ADD CONSTRAINT FK_MacchineStandard_FormatiMacchine_IdFormato
            FOREIGN KEY (IdFormato)
            REFERENCES FormatiMacchine (IdFormato)
            ON DELETE SET NULL;
        PRINT '  [OK] FK_MacchineStandard_FormatiMacchine_IdFormato creata.';
    END
    ELSE PRINT '  [SKIP] FK_MacchineStandard_FormatiMacchine_IdFormato esiste gia.';

    -- =========================================================================
    -- STEP 9 — Aggiunge colonna Piastre.IdFormato
    -- =========================================================================
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('Piastre') AND name = 'IdFormato'
    )
    BEGIN
        ALTER TABLE Piastre ADD IdFormato INT NULL;
        PRINT '  [OK] Colonna Piastre.IdFormato aggiunta.';
    END
    ELSE PRINT '  [SKIP] Piastre.IdFormato esiste gia.';

    -- =========================================================================
    -- STEP 10 — Indice e FK Piastre -> FormatiMacchine
    -- =========================================================================
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_Piastre_IdFormato'
          AND object_id = OBJECT_ID('Piastre')
    )
    BEGIN
        CREATE INDEX IX_Piastre_IdFormato ON Piastre (IdFormato);
        PRINT '  [OK] IX_Piastre_IdFormato creato.';
    END
    ELSE PRINT '  [SKIP] IX_Piastre_IdFormato esiste gia.';

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = 'FK_Piastre_FormatiMacchine_IdFormato'
    )
    BEGIN
        ALTER TABLE Piastre
            ADD CONSTRAINT FK_Piastre_FormatiMacchine_IdFormato
            FOREIGN KEY (IdFormato)
            REFERENCES FormatiMacchine (IdFormato)
            ON DELETE SET NULL;
        PRINT '  [OK] FK_Piastre_FormatiMacchine_IdFormato creata.';
    END
    ELSE PRINT '  [SKIP] FK_Piastre_FormatiMacchine_IdFormato esiste gia.';

    -- =========================================================================
    -- STEP 11 — Seed formati standard (solo se la tabella è vuota)
    -- =========================================================================
    IF NOT EXISTS (SELECT 1 FROM FormatiMacchine)
    BEGIN
        INSERT INTO FormatiMacchine (NomeFormato, IsEliminata, Note)
        VALUES
            ('106',  0, NULL),
            ('145',  0, NULL),
            ('88',   0, NULL),
            ('102',  0, NULL),
            ('120',  0, NULL);
        PRINT '  [OK] Formati standard inseriti (106, 145, 88, 102, 120).';
    END
    ELSE PRINT '  [SKIP] FormatiMacchine non e vuota, seed saltato.';

    -- =========================================================================
    -- STEP 12 — Fix: IsEliminata NULL -> 0 per eventuali piastre inserite via SSMS
    -- =========================================================================
    UPDATE Piastre SET IsEliminata = 0 WHERE IsEliminata IS NULL;
    IF @@ROWCOUNT > 0
        PRINT '  [FIX] Corrette piastre con IsEliminata NULL.';

    -- =========================================================================
    -- STEP 13 — Registra la migrazione in __EFMigrationsHistory
    -- =========================================================================
    IF NOT EXISTS (
        SELECT 1 FROM __EFMigrationsHistory
        WHERE MigrationId = '20260625180000_RinominaFamiglieInFormatiAddFormatoPiastra'
    )
    BEGIN
        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES ('20260625180000_RinominaFamiglieInFormatiAddFormatoPiastra', '8.0.0');
        PRINT '  [OK] Migrazione registrata in __EFMigrationsHistory.';
    END
    ELSE PRINT '  [SKIP] Migrazione gia presente in __EFMigrationsHistory.';

    COMMIT TRANSACTION;
    PRINT '';
    PRINT '=== Migrazione completata con successo. ===';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '=== ERRORE — rollback eseguito. ===';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;
GO
