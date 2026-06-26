-- =============================================================================
-- RESET COMPLETO DATABASE — solo per ambienti di sviluppo/test
--
-- !! ATTENZIONE: cancella TUTTI i dati esistenti. !!
-- Non eseguire su produzione.
--
-- Ricrea lo schema completo (tutte e tre le migrazioni) e inserisce
-- i dati di seed iniziali.
-- =============================================================================

USE PlateArchiveDB;
GO

BEGIN TRANSACTION;
BEGIN TRY

    PRINT '=== Reset completo PlateArchiveDB ===';
    PRINT '';

    -- =========================================================================
    -- FASE 1 — Elimina tutti gli oggetti (ordine rispetta le FK)
    -- =========================================================================
    PRINT '--- Fase 1: eliminazione oggetti esistenti ---';

    -- Tabelle figlie (con FK verso le tabelle padre)
    IF OBJECT_ID('ClientiPiastre',           'U') IS NOT NULL DROP TABLE ClientiPiastre;
    IF OBJECT_ID('ClientiMacchine',          'U') IS NOT NULL DROP TABLE ClientiMacchine;
    IF OBJECT_ID('PiastreMacchineCompatibili','U') IS NOT NULL DROP TABLE PiastreMacchineCompatibili;
    IF OBJECT_ID('Disegni',                  'U') IS NOT NULL DROP TABLE Disegni;
    -- Tabelle padre
    IF OBJECT_ID('Piastre',          'U') IS NOT NULL DROP TABLE Piastre;
    IF OBJECT_ID('MacchineStandard', 'U') IS NOT NULL DROP TABLE MacchineStandard;
    IF OBJECT_ID('Clienti',          'U') IS NOT NULL DROP TABLE Clienti;
    -- Lookup
    IF OBJECT_ID('FormatiMacchine',   'U') IS NOT NULL DROP TABLE FormatiMacchine;
    IF OBJECT_ID('FamiglieMacchine',  'U') IS NOT NULL DROP TABLE FamiglieMacchine;  -- nome vecchio
    IF OBJECT_ID('ProduttoriMacchine','U') IS NOT NULL DROP TABLE ProduttoriMacchine;
    IF OBJECT_ID('CategoriePiastre',  'U') IS NOT NULL DROP TABLE CategoriePiastre;
    -- Storico migrazioni
    IF OBJECT_ID('__EFMigrationsHistory', 'U') IS NOT NULL DROP TABLE __EFMigrationsHistory;

    PRINT '  [OK] Tabelle eliminate.';

    -- =========================================================================
    -- FASE 2 — Crea tabella migrazioni EF
    -- =========================================================================
    PRINT '';
    PRINT '--- Fase 2: struttura database ---';

    CREATE TABLE __EFMigrationsHistory (
        MigrationId    NVARCHAR(150) NOT NULL,
        ProductVersion NVARCHAR(32)  NOT NULL,
        CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY (MigrationId)
    );

    -- =========================================================================
    -- Lookup: CategoriePiastre
    -- =========================================================================
    CREATE TABLE CategoriePiastre (
        IdCategoriaPiastra INT           NOT NULL IDENTITY(1,1),
        Codice             NVARCHAR(450) NOT NULL,
        Descrizione        NVARCHAR(MAX) NOT NULL,
        Ordine             INT           NOT NULL,
        CONSTRAINT PK_CategoriePiastre PRIMARY KEY (IdCategoriaPiastra)
    );
    CREATE UNIQUE INDEX IX_CategoriePiastre_Codice ON CategoriePiastre (Codice);

    -- =========================================================================
    -- Lookup: FormatiMacchine  (dimensione piano fustellatura: 106, 145, 88, ...)
    -- =========================================================================
    CREATE TABLE FormatiMacchine (
        IdFormato   INT           NOT NULL IDENTITY(1,1),
        NomeFormato NVARCHAR(450) NOT NULL,
        IsEliminata BIT           NOT NULL CONSTRAINT DF_FormatiMacchine_IsEliminata DEFAULT 0,
        Note        NVARCHAR(MAX) NULL,
        CONSTRAINT PK_FormatiMacchine PRIMARY KEY (IdFormato)
    );
    CREATE UNIQUE INDEX IX_FormatiMacchine_NomeFormato ON FormatiMacchine (NomeFormato);

    -- =========================================================================
    -- Lookup: ProduttoriMacchine
    -- =========================================================================
    CREATE TABLE ProduttoriMacchine (
        IdProduttore   INT           NOT NULL IDENTITY(1,1),
        NomeProduttore NVARCHAR(450) NOT NULL,
        IsEliminata    BIT           NOT NULL CONSTRAINT DF_ProduttoriMacchine_IsEliminata DEFAULT 0,
        Note           NVARCHAR(MAX) NULL,
        CONSTRAINT PK_ProduttoriMacchine PRIMARY KEY (IdProduttore)
    );
    CREATE UNIQUE INDEX IX_ProduttoriMacchine_NomeProduttore ON ProduttoriMacchine (NomeProduttore);

    -- =========================================================================
    -- Clienti
    -- =========================================================================
    CREATE TABLE Clienti (
        IdCliente               INT           NOT NULL IDENTITY(1,1),
        CodiceClienteGestionale NVARCHAR(450) NOT NULL,
        RagioneSociale          NVARCHAR(MAX) NOT NULL,
        Note                    NVARCHAR(MAX) NULL,
        CONSTRAINT PK_Clienti PRIMARY KEY (IdCliente)
    );
    CREATE UNIQUE INDEX IX_Clienti_CodiceClienteGestionale ON Clienti (CodiceClienteGestionale);

    -- =========================================================================
    -- MacchineStandard
    -- =========================================================================
    CREATE TABLE MacchineStandard (
        IdMacchinaStandard INT              NOT NULL IDENTITY(1,1),
        CodiceMacchina     NVARCHAR(450)    NOT NULL,
        NomeMacchina       NVARCHAR(MAX)    NOT NULL,
        IdFormato          INT              NULL,
        IdProduttore       INT              NULL,
        LarghezzaMm        DECIMAL(18,2)    NULL,
        AltezzaMm          DECIMAL(18,2)    NULL,
        Versione           NVARCHAR(MAX)    NULL,
        Attiva             BIT              NOT NULL CONSTRAINT DF_MacchineStandard_Attiva DEFAULT 1,
        Note               NVARCHAR(MAX)    NULL,
        CONSTRAINT PK_MacchineStandard  PRIMARY KEY (IdMacchinaStandard),
        CONSTRAINT FK_MacchineStandard_FormatiMacchine_IdFormato
            FOREIGN KEY (IdFormato)    REFERENCES FormatiMacchine   (IdFormato)    ON DELETE NO ACTION,
        CONSTRAINT FK_MacchineStandard_ProduttoriMacchine_IdProduttore
            FOREIGN KEY (IdProduttore) REFERENCES ProduttoriMacchine (IdProduttore) ON DELETE SET NULL
    );
    CREATE UNIQUE INDEX IX_MacchineStandard_CodiceMacchina ON MacchineStandard (CodiceMacchina);
    CREATE        INDEX IX_MacchineStandard_IdFormato      ON MacchineStandard (IdFormato);
    CREATE        INDEX IX_MacchineStandard_IdProduttore   ON MacchineStandard (IdProduttore);

    -- =========================================================================
    -- Piastre
    -- =========================================================================
    CREATE TABLE Piastre (
        IdPiastra                INT           NOT NULL IDENTITY(1,1),
        CodicePiastra            NVARCHAR(450) NOT NULL,
        CodiceArticoloGestionale NVARCHAR(450) NULL,
        Descrizione              NVARCHAR(MAX) NULL,
        Stato                    INT           NOT NULL,
        IdCategoriaPiastra       INT           NULL,
        IdFormato                INT           NULL,
        IsEliminata              BIT           NOT NULL CONSTRAINT DF_Piastre_IsEliminata DEFAULT 0,
        LarghezzaMm              DECIMAL(18,2) NULL,
        AltezzaMm                DECIMAL(18,2) NULL,
        SpessoreMm               DECIMAL(18,2) NULL,
        Durezza                  DECIMAL(18,2) NULL,
        Peso                     DECIMAL(18,3) NULL,
        Note                     NVARCHAR(MAX) NULL,
        DataCreazione            DATETIME2     NOT NULL,
        DataUltimaModifica       DATETIME2     NOT NULL,
        CONSTRAINT PK_Piastre PRIMARY KEY (IdPiastra),
        CONSTRAINT FK_Piastre_CategoriePiastre_IdCategoriaPiastra
            FOREIGN KEY (IdCategoriaPiastra) REFERENCES CategoriePiastre (IdCategoriaPiastra) ON DELETE SET NULL,
        CONSTRAINT FK_Piastre_FormatiMacchine_IdFormato
            FOREIGN KEY (IdFormato) REFERENCES FormatiMacchine (IdFormato) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX IX_Piastre_CodicePiastra          ON Piastre (CodicePiastra);
    CREATE UNIQUE INDEX IX_Piastre_CodiceArticoloGestionale
        ON Piastre (CodiceArticoloGestionale) WHERE CodiceArticoloGestionale IS NOT NULL;
    CREATE INDEX IX_Piastre_IdCategoriaPiastra            ON Piastre (IdCategoriaPiastra);
    CREATE INDEX IX_Piastre_IdFormato                     ON Piastre (IdFormato);

    -- =========================================================================
    -- Disegni (1:1 con Piastra)
    -- =========================================================================
    CREATE TABLE Disegni (
        IdDisegno              INT           NOT NULL IDENTITY(1,1),
        IdPiastra              INT           NOT NULL,
        CodiceDisegno          NVARCHAR(MAX) NULL,
        NomeFile               NVARCHAR(MAX) NULL,
        PercorsoFile           NVARCHAR(MAX) NULL,
        VaultId                NVARCHAR(MAX) NULL,
        Revisione              NVARCHAR(MAX) NULL,
        Formato                NVARCHAR(MAX) NULL,
        Stato                  INT           NOT NULL,
        DataUltimaModificaFile DATETIME2     NULL,
        Note                   NVARCHAR(MAX) NULL,
        CONSTRAINT PK_Disegni             PRIMARY KEY (IdDisegno),
        CONSTRAINT FK_Disegni_Piastre_IdPiastra
            FOREIGN KEY (IdPiastra) REFERENCES Piastre (IdPiastra) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX IX_Disegni_IdPiastra ON Disegni (IdPiastra);

    -- =========================================================================
    -- PiastreMacchineCompatibili (M:N piastre ↔ macchine)
    -- =========================================================================
    CREATE TABLE PiastreMacchineCompatibili (
        IdCompatibilita    INT           NOT NULL IDENTITY(1,1),
        IdPiastra          INT           NOT NULL,
        IdMacchinaStandard INT           NOT NULL,
        FonteDato          INT           NULL,
        DataVerifica       DATETIME2     NULL,
        UtenteVerifica     NVARCHAR(MAX) NULL,
        Attiva             BIT           NOT NULL CONSTRAINT DF_PiastreMacchineCompatibili_Attiva DEFAULT 1,
        Note               NVARCHAR(MAX) NULL,
        CONSTRAINT PK_PiastreMacchineCompatibili PRIMARY KEY (IdCompatibilita),
        CONSTRAINT FK_PiastreMacchineCompatibili_Piastre_IdPiastra
            FOREIGN KEY (IdPiastra)          REFERENCES Piastre        (IdPiastra)          ON DELETE CASCADE,
        CONSTRAINT FK_PiastreMacchineCompatibili_MacchineStandard_IdMacchinaStandard
            FOREIGN KEY (IdMacchinaStandard) REFERENCES MacchineStandard (IdMacchinaStandard) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX IX_PiastreMacchineCompatibili_IdPiastra_IdMacchinaStandard
        ON PiastreMacchineCompatibili (IdPiastra, IdMacchinaStandard);
    CREATE INDEX IX_PiastreMacchineCompatibili_IdPiastra          ON PiastreMacchineCompatibili (IdPiastra);
    CREATE INDEX IX_PiastreMacchineCompatibili_IdMacchinaStandard ON PiastreMacchineCompatibili (IdMacchinaStandard);

    -- =========================================================================
    -- ClientiMacchine (M:N clienti ↔ macchine)
    -- =========================================================================
    CREATE TABLE ClientiMacchine (
        IdClienteMacchina    INT           NOT NULL IDENTITY(1,1),
        IdCliente            INT           NOT NULL,
        IdMacchinaStandard   INT           NOT NULL,
        Matricola            NVARCHAR(MAX) NULL,
        CodiceInternoCliente NVARCHAR(MAX) NULL,
        DataAssociazione     DATETIME2     NOT NULL,
        Attiva               BIT           NOT NULL CONSTRAINT DF_ClientiMacchine_Attiva DEFAULT 1,
        Note                 NVARCHAR(MAX) NULL,
        CONSTRAINT PK_ClientiMacchine PRIMARY KEY (IdClienteMacchina),
        CONSTRAINT FK_ClientiMacchine_Clienti_IdCliente
            FOREIGN KEY (IdCliente)          REFERENCES Clienti         (IdCliente)          ON DELETE CASCADE,
        CONSTRAINT FK_ClientiMacchine_MacchineStandard_IdMacchinaStandard
            FOREIGN KEY (IdMacchinaStandard) REFERENCES MacchineStandard (IdMacchinaStandard) ON DELETE CASCADE
    );
    CREATE INDEX IX_ClientiMacchine_IdCliente          ON ClientiMacchine (IdCliente);
    CREATE INDEX IX_ClientiMacchine_IdMacchinaStandard ON ClientiMacchine (IdMacchinaStandard);

    -- =========================================================================
    -- ClientiPiastre (M:N clienti ↔ piastre, con riferimento opzionale a macchina)
    -- =========================================================================
    CREATE TABLE ClientiPiastre (
        IdClientePiastra  INT           NOT NULL IDENTITY(1,1),
        IdCliente         INT           NOT NULL,
        IdPiastra         INT           NOT NULL,
        IdClienteMacchina INT           NULL,
        DataAssociazione  DATETIME2     NOT NULL,
        Stato             INT           NOT NULL,
        Note              NVARCHAR(MAX) NULL,
        CONSTRAINT PK_ClientiPiastre PRIMARY KEY (IdClientePiastra),
        CONSTRAINT FK_ClientiPiastre_Clienti_IdCliente
            FOREIGN KEY (IdCliente)         REFERENCES Clienti        (IdCliente)         ON DELETE CASCADE,
        CONSTRAINT FK_ClientiPiastre_Piastre_IdPiastra
            FOREIGN KEY (IdPiastra)         REFERENCES Piastre        (IdPiastra)         ON DELETE CASCADE,
        CONSTRAINT FK_ClientiPiastre_ClientiMacchine_IdClienteMacchina
            FOREIGN KEY (IdClienteMacchina) REFERENCES ClientiMacchine (IdClienteMacchina) ON DELETE NO ACTION
    );
    CREATE UNIQUE INDEX IX_ClientiPiastre_IdCliente_IdPiastra ON ClientiPiastre (IdCliente, IdPiastra);
    CREATE INDEX IX_ClientiPiastre_IdCliente         ON ClientiPiastre (IdCliente);
    CREATE INDEX IX_ClientiPiastre_IdPiastra         ON ClientiPiastre (IdPiastra);
    CREATE INDEX IX_ClientiPiastre_IdClienteMacchina ON ClientiPiastre (IdClienteMacchina);

    PRINT '  [OK] Schema creato.';

    -- =========================================================================
    -- FASE 3 — Dati di seed
    -- =========================================================================
    PRINT '';
    PRINT '--- Fase 3: dati seed ---';

    -- Categorie piastre
    INSERT INTO CategoriePiastre (Codice, Descrizione, Ordine) VALUES
        ('STD', 'Standard', 1),
        ('SPE', 'Speciale', 2);

    -- Formati macchina (dimensioni piano fustellatura più comuni)
    INSERT INTO FormatiMacchine (NomeFormato, IsEliminata) VALUES
        ('88',  0),
        ('102', 0),
        ('106', 0),
        ('120', 0),
        ('145', 0);

    -- Registra le tre migrazioni come gia applicate
    INSERT INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES
        ('20260625140000_InitialCreate',                                      '8.0.0'),
        ('20260625160000_AddProduttoriFamiglieMacchine',                      '8.0.0'),
        ('20260625180000_RinominaFamiglieInFormatiAddFormatoPiastra',          '8.0.0');

    PRINT '  [OK] Seed completato.';

    COMMIT TRANSACTION;
    PRINT '';
    PRINT '=== Reset completato con successo. ===';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '=== ERRORE — rollback eseguito. ===';
    PRINT ERROR_MESSAGE();
    THROW;
END CATCH;
GO
