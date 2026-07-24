-- ============================================================================
-- Cancellazione FISICA delle piastre senza formato macchina (TASK-21)
-- ----------------------------------------------------------------------------
-- Il formato macchina è ora obbligatorio: è il criterio con cui la piastra viene
-- abbinata alle righe ordine di vendita (cliente + formato). Le piastre prive di
-- formato non comparirebbero mai negli ordini.
--
-- ⚠️  ATTENZIONE — OPERAZIONE IRREVERSIBILE
--     Vengono eliminate anche le righe collegate:
--       - Disegni                      (SOLO i metadati: i FILE sul server
--                                       condiviso NON vengono toccati)
--       - PiastreMacchineCompatibili   (compatibilità tecniche)
--       - ClientiPiastre               (associazioni commerciali)
--     Fare un BACKUP del database prima di eseguire.
--
-- COME USARLO
--   1) Esegui solo la PARTE 1 e controlla l'elenco: sono le piastre che verranno
--      cancellate. Se qualcuna va salvata, assegnale un formato e rilancia.
--   2) Esegui la PARTE 2. Il COMMIT è volutamente COMMENTATO: dopo aver letto i
--      conteggi, decommenta COMMIT TRAN (o esegui ROLLBACK TRAN per annullare).
-- ============================================================================

USE PlateArchiveDB;
GO

-- Necessario: alcune tabelle (es. Disegni) hanno indici filtrati che richiedono
-- QUOTED_IDENTIFIER ON, altrimenti le DELETE falliscono con errore 1934.
-- In SSMS è già ON; in sqlcmd usare il flag -I oppure lasciare questi SET.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ─── PARTE 1 — Ricognizione (sola lettura) ──────────────────────────────────

SELECT COUNT(*) AS PiastreSenzaFormato
FROM dbo.Piastre
WHERE IdFormato IS NULL;

SELECT  p.IdPiastra,
        p.CodicePiastra,
        p.Descrizione,
        p.IsEliminata,
        (SELECT COUNT(*) FROM dbo.Disegni                    d  WHERE d.IdPiastra  = p.IdPiastra) AS NrDisegni,
        (SELECT COUNT(*) FROM dbo.ClientiPiastre             cp WHERE cp.IdPiastra = p.IdPiastra) AS NrClienti,
        (SELECT COUNT(*) FROM dbo.PiastreMacchineCompatibili mc WHERE mc.IdPiastra = p.IdPiastra) AS NrMacchine
FROM dbo.Piastre p
WHERE p.IdFormato IS NULL
ORDER BY p.CodicePiastra;

-- Percorsi dei file disegno che resteranno orfani sul server condiviso
-- (i file NON vengono cancellati: valutare a mano se archiviarli o rimuoverli).
SELECT d.IdPiastra, p.CodicePiastra, d.NomeFile, d.PercorsoFile
FROM dbo.Disegni d
INNER JOIN dbo.Piastre p ON p.IdPiastra = d.IdPiastra
WHERE p.IdFormato IS NULL
ORDER BY p.CodicePiastra;
GO

-- ─── PARTE 2 — Cancellazione (transazionale) ────────────────────────────────

SET XACT_ABORT ON;  -- in caso di errore annulla l'intera transazione
BEGIN TRAN;

    DECLARE @ids TABLE (IdPiastra int PRIMARY KEY);

    INSERT INTO @ids (IdPiastra)
    SELECT IdPiastra FROM dbo.Piastre WHERE IdFormato IS NULL;

    DELETE mc
    FROM dbo.PiastreMacchineCompatibili mc
    INNER JOIN @ids i ON i.IdPiastra = mc.IdPiastra;
    PRINT CONCAT('PiastreMacchineCompatibili eliminate: ', @@ROWCOUNT);

    DELETE cp
    FROM dbo.ClientiPiastre cp
    INNER JOIN @ids i ON i.IdPiastra = cp.IdPiastra;
    PRINT CONCAT('ClientiPiastre eliminate: ', @@ROWCOUNT);

    DELETE d
    FROM dbo.Disegni d
    INNER JOIN @ids i ON i.IdPiastra = d.IdPiastra;
    PRINT CONCAT('Disegni (metadati) eliminati: ', @@ROWCOUNT);

    DELETE p
    FROM dbo.Piastre p
    INNER JOIN @ids i ON i.IdPiastra = p.IdPiastra;
    PRINT CONCAT('Piastre eliminate: ', @@ROWCOUNT);

-- Controlla i conteggi stampati sopra, poi scegli:
-- COMMIT TRAN;
-- ROLLBACK TRAN;
GO
