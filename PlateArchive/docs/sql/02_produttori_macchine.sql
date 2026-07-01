-- =============================================================================
-- MIGRAZIONE INCREMENTALE
-- PlateArchive — 20260629000000_AddProduttoriMacchine
--
-- Crea la tabella lookup ProduttoriMacchine e la FK da MacchineStandard.
-- Applicare su un DB che ha già le prime tre migrazioni EF Core applicate:
--   20260625140000_InitialCreate
--   20260625160000_AddProduttoriFamiglieMacchine
--   20260625180000_RinominaFamiglieInFormatiAddFormatoPiastra
--
-- La migrazione è IDEMPOTENTE: controlla ogni step prima di eseguirlo.
-- Eseguire in SSMS con il database PlateArchiveDB selezionato.
-- =============================================================================

USE PlateArchiveDB;
GO

BEGIN TRANSACTION;
BEGIN TRY

    PRINT '=== Inizio migrazione 20260629000000_AddProduttoriMacchine ===';

    -- =========================================================================
    -- STEP 1 — Crea tabella ProduttoriMacchine
    -- =========================================================================
    IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ProduttoriMacchine')
    BEGIN
        CREATE TABLE ProduttoriMacchine (
            IdProduttore   INT           NOT NULL IDENTITY(1,1),
            NomeProduttore NVARCHAR(200) NOT NULL,
            IsEliminata    BIT           NOT NULL DEFAULT 0,
            Note           NVARCHAR(MAX) NULL,
            CONSTRAINT PK_ProduttoriMacchine PRIMARY KEY (IdProduttore)
        );
        PRINT '  [OK] Tabella ProduttoriMacchine creata.';
    END
    ELSE PRINT '  [SKIP] ProduttoriMacchine esiste gia.';

    -- =========================================================================
    -- STEP 2 — Indice su NomeProduttore
    -- =========================================================================
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_ProduttoriMacchine_NomeProduttore'
          AND object_id = OBJECT_ID('ProduttoriMacchine')
    )
    BEGIN
        CREATE INDEX IX_ProduttoriMacchine_NomeProduttore
            ON ProduttoriMacchine (NomeProduttore);
        PRINT '  [OK] IX_ProduttoriMacchine_NomeProduttore creato.';
    END
    ELSE PRINT '  [SKIP] IX_ProduttoriMacchine_NomeProduttore esiste gia.';

    -- =========================================================================
    -- STEP 3 — Aggiungi colonna IdProduttore a MacchineStandard
    -- =========================================================================
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('MacchineStandard') AND name = 'IdProduttore'
    )
    BEGIN
        ALTER TABLE MacchineStandard ADD IdProduttore INT NULL;
        PRINT '  [OK] Colonna MacchineStandard.IdProduttore aggiunta.';
    END
    ELSE PRINT '  [SKIP] MacchineStandard.IdProduttore esiste gia.';

    -- =========================================================================
    -- STEP 4 — Indice su MacchineStandard.IdProduttore
    -- =========================================================================
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_MacchineStandard_IdProduttore'
          AND object_id = OBJECT_ID('MacchineStandard')
    )
    BEGIN
        CREATE INDEX IX_MacchineStandard_IdProduttore
            ON MacchineStandard (IdProduttore);
        PRINT '  [OK] IX_MacchineStandard_IdProduttore creato.';
    END
    ELSE PRINT '  [SKIP] IX_MacchineStandard_IdProduttore esiste gia.';

    -- =========================================================================
    -- STEP 5 — FK MacchineStandard -> ProduttoriMacchine
    -- ON DELETE SET NULL: eliminare un produttore scollega le macchine (non le cancella).
    -- =========================================================================
    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE name = 'FK_MacchineStandard_ProduttoriMacchine_IdProduttore'
    )
    BEGIN
        ALTER TABLE MacchineStandard
            ADD CONSTRAINT FK_MacchineStandard_ProduttoriMacchine_IdProduttore
            FOREIGN KEY (IdProduttore)
            REFERENCES ProduttoriMacchine (IdProduttore)
            ON DELETE SET NULL;
        PRINT '  [OK] FK_MacchineStandard_ProduttoriMacchine_IdProduttore creata.';
    END
    ELSE PRINT '  [SKIP] FK_MacchineStandard_ProduttoriMacchine_IdProduttore esiste gia.';

    -- =========================================================================
    -- STEP 6 — Registra la migrazione in __EFMigrationsHistory
    -- =========================================================================
    IF NOT EXISTS (
        SELECT 1 FROM __EFMigrationsHistory
        WHERE MigrationId = '20260629000000_AddProduttoriMacchine'
    )
    BEGIN
        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES ('20260629000000_AddProduttoriMacchine', '8.0.0');
        PRINT '  [OK] Migrazione registrata in __EFMigrationsHistory.';
    END
    ELSE PRINT '  [SKIP] Migrazione gia presente in __EFMigrationsHistory.';

    COMMIT TRANSACTION;
    PRINT '';
    PRINT '=== Migrazione 20260629000000_AddProduttoriMacchine completata ===';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ERRORE: ' + ERROR_MESSAGE();
    THROW;
END CATCH
GO
