-- =============================================================================
-- MIGRAZIONE INCREMENTALE
-- PlateArchive — migrazione 20260626000000_DisegnoNM
--
-- Cambia la relazione Disegno ↔ Piastra da 1:1 a N:M.
-- Prima di questa migrazione ogni Disegno aveva un IdPiastra (FK con UNIQUE).
-- Dopo: il collegamento passa per la tabella DisegniPiastre.
--
-- ORDINE CRITICO: la vecchia FK Disegni.IdPiastra → Piastre (CASCADE) deve essere
-- rimossa PRIMA di creare DisegniPiastre, altrimenti SQL Server segnala Msg 1785
-- (due percorsi di cascata da Piastre a DisegniPiastre: diretto e via Disegni).
--
-- La migrazione è IDEMPOTENTE: ogni step controlla prima di eseguire.
-- =============================================================================

USE PlateArchiveDB;
GO

BEGIN TRANSACTION;
BEGIN TRY

    PRINT '=== Inizio migrazione 20260626000000_DisegnoNM ===';

    -- =========================================================================
    -- STEP 1 — Rimuovi FK Disegni -> Piastre (percorso CASCADE vecchio)
    -- DEVE venire PRIMA della creazione di DisegniPiastre per evitare Msg 1785.
    -- =========================================================================
    DECLARE @fkDisegniPiastre NVARCHAR(256);
    SELECT @fkDisegniPiastre = name
    FROM   sys.foreign_keys
    WHERE  parent_object_id     = OBJECT_ID('Disegni')
      AND  referenced_object_id = OBJECT_ID('Piastre');

    IF @fkDisegniPiastre IS NOT NULL
    BEGIN
        EXEC('ALTER TABLE Disegni DROP CONSTRAINT ' + @fkDisegniPiastre);
        PRINT '  [OK] FK Disegni -> Piastre rimossa.';
    END
    ELSE PRINT '  [SKIP] FK Disegni -> Piastre già assente.';

    -- =========================================================================
    -- STEP 2 — Rimuovi indice univoco su Disegni.IdPiastra (dipende dalla colonna)
    -- =========================================================================
    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE object_id = OBJECT_ID('Disegni') AND name = 'IX_Disegni_IdPiastra'
    )
    BEGIN
        DROP INDEX IX_Disegni_IdPiastra ON Disegni;
        PRINT '  [OK] Indice IX_Disegni_IdPiastra rimosso.';
    END
    ELSE PRINT '  [SKIP] IX_Disegni_IdPiastra già assente.';

    -- =========================================================================
    -- STEP 3 — Crea tabella DisegniPiastre (junction N:M)
    -- Ora è sicuro usare CASCADE su entrambe le FK perché il percorso indiretto
    -- Piastre -> Disegni -> DisegniPiastre è stato spezzato nello step 1.
    -- =========================================================================
    IF OBJECT_ID('DisegniPiastre', 'U') IS NULL
    BEGIN
        CREATE TABLE DisegniPiastre (
            IdDisegnoPiastra INT           NOT NULL IDENTITY(1,1),
            IdDisegno        INT           NOT NULL,
            IdPiastra        INT           NOT NULL,
            DataAssociazione DATETIME2     NOT NULL DEFAULT GETUTCDATE(),
            Note             NVARCHAR(MAX) NULL,

            CONSTRAINT PK_DisegniPiastre PRIMARY KEY (IdDisegnoPiastra),

            CONSTRAINT FK_DisegniPiastre_Disegni
                FOREIGN KEY (IdDisegno) REFERENCES Disegni (IdDisegno) ON DELETE CASCADE,

            CONSTRAINT FK_DisegniPiastre_Piastre
                FOREIGN KEY (IdPiastra) REFERENCES Piastre (IdPiastra) ON DELETE CASCADE
        );
        PRINT '  [OK] Tabella DisegniPiastre creata.';
    END
    ELSE PRINT '  [SKIP] DisegniPiastre esiste già.';

    -- =========================================================================
    -- STEP 4 — Indice univoco (IdDisegno, IdPiastra)
    -- =========================================================================
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_DisegniPiastre_IdDisegno_IdPiastra'
          AND object_id = OBJECT_ID('DisegniPiastre')
    )
    BEGIN
        CREATE UNIQUE INDEX IX_DisegniPiastre_IdDisegno_IdPiastra
            ON DisegniPiastre (IdDisegno, IdPiastra);
        PRINT '  [OK] Indice univoco IX_DisegniPiastre_IdDisegno_IdPiastra creato.';
    END
    ELSE PRINT '  [SKIP] Indice IX_DisegniPiastre_IdDisegno_IdPiastra esiste già.';

    -- =========================================================================
    -- STEP 5 — Migra i dati esistenti: ogni Disegni.IdPiastra -> riga in DisegniPiastre
    -- =========================================================================
    IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('Disegni') AND name = 'IdPiastra'
    )
    BEGIN
        INSERT INTO DisegniPiastre (IdDisegno, IdPiastra, DataAssociazione)
        SELECT IdDisegno, IdPiastra, GETUTCDATE()
        FROM   Disegni
        WHERE  IdPiastra IS NOT NULL
          AND  NOT EXISTS (
              SELECT 1 FROM DisegniPiastre dp
              WHERE dp.IdDisegno = Disegni.IdDisegno
                AND dp.IdPiastra = Disegni.IdPiastra
          );
        PRINT '  [OK] Dati migrati da Disegni.IdPiastra -> DisegniPiastre.';
    END
    ELSE PRINT '  [SKIP] Colonna Disegni.IdPiastra assente (migrazione già completata o DB nuovo).';

    -- =========================================================================
    -- STEP 6 — Rimuovi colonna Disegni.IdPiastra
    -- =========================================================================
    IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID('Disegni') AND name = 'IdPiastra'
    )
    BEGIN
        ALTER TABLE Disegni DROP COLUMN IdPiastra;
        PRINT '  [OK] Colonna Disegni.IdPiastra rimossa.';
    END
    ELSE PRINT '  [SKIP] Colonna Disegni.IdPiastra già assente.';

    -- =========================================================================
    -- STEP 7 — Registra la migrazione
    -- =========================================================================
    IF NOT EXISTS (
        SELECT 1 FROM __EFMigrationsHistory
        WHERE MigrationId = '20260626000000_DisegnoNM'
    )
    BEGIN
        INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion)
        VALUES ('20260626000000_DisegnoNM', '8.0.0');
        PRINT '  [OK] Migrazione registrata.';
    END
    ELSE PRINT '  [SKIP] Migrazione già registrata.';

    COMMIT TRANSACTION;
    PRINT '';
    PRINT '=== Migrazione 20260626000000_DisegnoNM completata con successo. ===';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '=== ERRORE — rollback eseguito. ===';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;
GO
