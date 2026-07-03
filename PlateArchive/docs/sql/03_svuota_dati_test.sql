-- =============================================================================
-- SVUOTAMENTO DATI DI TEST — mantiene lo schema, cancella solo le righe
--
-- Scopo: ripartire da un database "pulito" per i test, SENZA toccare i dati
-- anagrafici dei Clienti (che provengono da DB2).
--
-- Svuota tutte le tabelle NON sincronizzate con DB2:
--   associazioni  -> ClientiPiastre, ClientiMacchine, PiastreMacchineCompatibili, Disegni
--   principali    -> Piastre, MacchineStandard
--   lookup/config -> CategoriePiastre, FormatiMacchine, ProduttoriMacchine
--
-- NON tocca: Clienti (fonte DB2) e __EFMigrationsHistory (schema/migrazioni).
-- I seed identità vengono azzerati: i nuovi record ripartono da Id = 1.
--
-- Solo per ambienti di sviluppo/test. NON eseguire in produzione.
-- =============================================================================

USE PlateArchiveDB;
GO

-- Necessario per le tabelle con indice filtrato (es. Piastre.CodiceArticoloGestionale).
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

BEGIN TRANSACTION;
BEGIN TRY

    PRINT '=== Svuotamento dati di test (Clienti preservati) ===';

    -- Ordine di cancellazione: prima le tabelle figlie, poi le padre, infine i lookup.
    DELETE FROM ClientiPiastre;
    DELETE FROM PiastreMacchineCompatibili;
    DELETE FROM Disegni;
    DELETE FROM ClientiMacchine;
    DELETE FROM Piastre;
    DELETE FROM MacchineStandard;
    DELETE FROM CategoriePiastre;
    DELETE FROM FormatiMacchine;
    DELETE FROM ProduttoriMacchine;

    -- Azzera i seed identità: il prossimo INSERT riparte da 1.
    DBCC CHECKIDENT ('ClientiPiastre',             RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('PiastreMacchineCompatibili', RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('Disegni',                    RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('ClientiMacchine',            RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('Piastre',                    RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('MacchineStandard',           RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('CategoriePiastre',           RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('FormatiMacchine',            RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT ('ProduttoriMacchine',         RESEED, 0) WITH NO_INFOMSGS;

    COMMIT TRANSACTION;
    PRINT '=== Svuotamento completato con successo. ===';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '=== ERRORE — rollback eseguito. ===';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;
GO


-- =============================================================================
-- OPZIONALE — svuota anche i CLIENTI (fonte DB2)
--
-- I Clienti si ricaricano DA SOLI al prossimo avvio dell'app: all'avvio viene
-- eseguita la sincronizzazione in background (App.OnStartup -> AvviaSyncInBackground)
-- che reinserisce da DB2 tutti i clienti mancanti. Non serve premere "Sincronizza".
--
-- PRE-REQUISITO: al prossimo avvio l'app deve poter raggiungere DB2 (VPN/ODBC
-- attivi), altrimenti la tabella resta vuota finché una sync non riesce.
--
-- Eseguire questo blocco SOLO se serve azzerare anche l'anagrafica clienti.
-- Va lanciato DOPO lo svuotamento sopra (le tabelle figlie devono essere vuote,
-- altrimenti le FK bloccano la DELETE).
--
-- Per usarlo: rimuovere i commenti (/* ... */).
-- =============================================================================
/*
USE PlateArchiveDB;
GO
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
GO

BEGIN TRANSACTION;
BEGIN TRY
    PRINT '=== Svuotamento Clienti (verranno ricaricati da DB2 al prossimo avvio) ===';

    DELETE FROM Clienti;
    DBCC CHECKIDENT ('Clienti', RESEED, 0) WITH NO_INFOMSGS;

    COMMIT TRANSACTION;
    PRINT '=== Clienti svuotati. Riavvia l''app (con VPN/DB2 raggiungibile) per ricaricarli. ===';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '=== ERRORE — rollback eseguito. ===';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;
GO
*/
