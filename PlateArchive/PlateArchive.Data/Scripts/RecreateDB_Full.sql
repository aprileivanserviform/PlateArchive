-- ===========================================================================
-- PlateArchive — Script di ricreazione completa del database
-- Schema finale: tutte e 4 le migrazioni EF Core 8.0 applicate
-- Ultima migrazione: 20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra
--
-- USO:
--   1. Connettiti a SQL Server con un login sysadmin
--   2. Esegui PRIMA il blocco "Crea database" (riga ~20) se il DB non esiste ancora
--   3. Poi esegui il resto (DROP → CREATE → seed)
--
-- ENUM C# → int (per riferimento rapido):
--   StatoPiastra:      Attiva=0  Obsoleta=1  DaVerificare=2
--   StatoDisegno:      Attivo=0  Obsoleto=1  DaVerificare=2
--   StatoClientePiastra: Attiva=0  Obsoleta=1  Proposta=2  DaVerificare=3
--   TipoPiastra:       Standard=0  SpecialeCliente=1
-- ===========================================================================


-- ---------------------------------------------------------------------------
-- BLOCCO OPZIONALE: crea il database se non esiste
-- Decommenta ed esegui separatamente prima del resto
-- ---------------------------------------------------------------------------
/*
USE [master];
GO
IF DB_ID('PlateArchiveDB') IS NULL
    CREATE DATABASE [PlateArchiveDB]
        COLLATE Latin1_General_CI_AS;
GO
*/

USE [PlateArchiveDB];
GO


-- ===========================================================================
-- SEZIONE 1 — DROP tabelle esistenti (ordine dipendenze inverse)
-- ===========================================================================

-- DisegniPiastre PRIMA: è una tabella orfana con FK verso Disegni e Piastre;
-- se non viene eliminata per prima blocca il DROP di tutte le tabelle referenziate.
IF OBJECT_ID('dbo.DisegniPiastre',             'U') IS NOT NULL DROP TABLE dbo.DisegniPiastre;
IF OBJECT_ID('dbo.ClientiPiastre',             'U') IS NOT NULL DROP TABLE dbo.ClientiPiastre;
IF OBJECT_ID('dbo.ClientiMacchine',            'U') IS NOT NULL DROP TABLE dbo.ClientiMacchine;
IF OBJECT_ID('dbo.PiastreMacchineCompatibili', 'U') IS NOT NULL DROP TABLE dbo.PiastreMacchineCompatibili;
IF OBJECT_ID('dbo.Disegni',                    'U') IS NOT NULL DROP TABLE dbo.Disegni;
IF OBJECT_ID('dbo.Piastre',                    'U') IS NOT NULL DROP TABLE dbo.Piastre;
IF OBJECT_ID('dbo.MacchineStandard',           'U') IS NOT NULL DROP TABLE dbo.MacchineStandard;
IF OBJECT_ID('dbo.FormatiMacchine',            'U') IS NOT NULL DROP TABLE dbo.FormatiMacchine;
IF OBJECT_ID('dbo.ProduttoriMacchine',         'U') IS NOT NULL DROP TABLE dbo.ProduttoriMacchine;
IF OBJECT_ID('dbo.Clienti',                    'U') IS NOT NULL DROP TABLE dbo.Clienti;
IF OBJECT_ID('dbo.CategoriePiastre',           'U') IS NOT NULL DROP TABLE dbo.CategoriePiastre;
IF OBJECT_ID('dbo.__EFMigrationsHistory',      'U') IS NOT NULL DROP TABLE dbo.__EFMigrationsHistory;
GO


-- ===========================================================================
-- SEZIONE 2 — Creazione tabelle
-- ===========================================================================

-- ---------------------------------------------------------------------------
-- CategoriePiastre
-- ---------------------------------------------------------------------------
CREATE TABLE dbo.CategoriePiastre (
    IdCategoriaPiastra  int           IDENTITY(1,1) NOT NULL,
    Codice              nvarchar(450) NOT NULL,
    Descrizione         nvarchar(max) NOT NULL,
    Ordine              int           NOT NULL,
    CONSTRAINT PK_CategoriePiastre PRIMARY KEY (IdCategoriaPiastra)
);
CREATE UNIQUE INDEX IX_CategoriePiastre_Codice
    ON dbo.CategoriePiastre (Codice);
GO

-- ---------------------------------------------------------------------------
-- Clienti
-- ---------------------------------------------------------------------------
CREATE TABLE dbo.Clienti (
    IdCliente               int           IDENTITY(1,1) NOT NULL,
    CodiceClienteGestionale nvarchar(450) NOT NULL,
    RagioneSociale          nvarchar(max) NOT NULL,
    Note                    nvarchar(max) NULL,
    CONSTRAINT PK_Clienti PRIMARY KEY (IdCliente)
);
CREATE UNIQUE INDEX IX_Clienti_CodiceClienteGestionale
    ON dbo.Clienti (CodiceClienteGestionale);
GO

-- ---------------------------------------------------------------------------
-- FormatiMacchine  (dimensioni del piano di fustellatura)
-- Nota: NomeFormato è nvarchar(max) — indice UNIQUE rimosso dalla migrazione
--       20260630063032. Unicità garantita a livello applicativo.
-- ---------------------------------------------------------------------------
CREATE TABLE dbo.FormatiMacchine (
    IdFormato   int           IDENTITY(1,1) NOT NULL,
    NomeFormato nvarchar(max) NOT NULL,
    IsEliminata bit           NOT NULL CONSTRAINT DF_FormatiMacchine_IsEliminata DEFAULT 0,
    Note        nvarchar(max) NULL,
    CONSTRAINT PK_FormatiMacchine PRIMARY KEY (IdFormato)
);
GO

-- ---------------------------------------------------------------------------
-- ProduttoriMacchine
-- Nota: NomeProduttore è nvarchar(max) — indice UNIQUE rimosso dalla migrazione
--       20260630063032. Unicità garantita a livello applicativo.
-- ---------------------------------------------------------------------------
CREATE TABLE dbo.ProduttoriMacchine (
    IdProduttore   int           IDENTITY(1,1) NOT NULL,
    NomeProduttore nvarchar(max) NOT NULL,
    IsEliminata    bit           NOT NULL CONSTRAINT DF_ProduttoriMacchine_IsEliminata DEFAULT 0,
    Note           nvarchar(max) NULL,
    CONSTRAINT PK_ProduttoriMacchine PRIMARY KEY (IdProduttore)
);
GO

-- ---------------------------------------------------------------------------
-- MacchineStandard
-- FK IdFormato    → FormatiMacchine    (NO ACTION — EF gestisce ClientSetNull)
-- FK IdProduttore → ProduttoriMacchine (SET NULL)
-- ---------------------------------------------------------------------------
CREATE TABLE dbo.MacchineStandard (
    IdMacchinaStandard int           IDENTITY(1,1) NOT NULL,
    CodiceMacchina     nvarchar(450) NOT NULL,
    NomeMacchina       nvarchar(max) NOT NULL,
    Versione           nvarchar(max) NULL,
    Attiva             bit           NOT NULL CONSTRAINT DF_MacchineStandard_Attiva DEFAULT 0,
    Note               nvarchar(max) NULL,
    IdFormato          int           NULL,
    IdProduttore       int           NULL,
    LarghezzaMm        decimal(18,2) NULL,
    AltezzaMm          decimal(18,2) NULL,
    CONSTRAINT PK_MacchineStandard PRIMARY KEY (IdMacchinaStandard),
    CONSTRAINT FK_MacchineStandard_FormatiMacchine_IdFormato
        FOREIGN KEY (IdFormato)
        REFERENCES dbo.FormatiMacchine (IdFormato)
        ON DELETE NO ACTION,
    CONSTRAINT FK_MacchineStandard_ProduttoriMacchine_IdProduttore
        FOREIGN KEY (IdProduttore)
        REFERENCES dbo.ProduttoriMacchine (IdProduttore)
        ON DELETE SET NULL
);
CREATE UNIQUE INDEX IX_MacchineStandard_CodiceMacchina
    ON dbo.MacchineStandard (CodiceMacchina);
CREATE INDEX IX_MacchineStandard_IdFormato
    ON dbo.MacchineStandard (IdFormato);
CREATE INDEX IX_MacchineStandard_IdProduttore
    ON dbo.MacchineStandard (IdProduttore);
GO

-- ---------------------------------------------------------------------------
-- Piastre
-- FK IdCategoriaPiastra  → CategoriePiastre  (SET NULL)
-- FK IdFormato           → FormatiMacchine   (NO ACTION — EF ClientSetNull)
-- FK IdClienteEsclusivo  → Clienti           (NO ACTION — EF ClientSetNull)
--    Solo le piastre TipoPiastra=1 (SpecialeCliente) valorizzano IdClienteEsclusivo
-- ---------------------------------------------------------------------------
CREATE TABLE dbo.Piastre (
    IdPiastra                int           IDENTITY(1,1) NOT NULL,
    CodicePiastra            nvarchar(450) NOT NULL,
    CodiceArticoloGestionale nvarchar(450) NULL,
    Descrizione              nvarchar(max) NULL,
    Stato                    int           NOT NULL,            -- StatoPiastra enum
    IdCategoriaPiastra       int           NULL,
    IdFormato                int           NULL,
    IsEliminata              bit           NOT NULL CONSTRAINT DF_Piastre_IsEliminata DEFAULT 0,
    LarghezzaMm              decimal(18,2) NULL,
    AltezzaMm                decimal(18,2) NULL,
    SpessoreMm               decimal(18,2) NULL,
    Durezza                  decimal(18,2) NULL,
    Peso                     decimal(18,2) NULL,
    Note                     nvarchar(max) NULL,
    DataCreazione            datetime2     NOT NULL CONSTRAINT DF_Piastre_DataCreazione DEFAULT GETUTCDATE(),
    DataUltimaModifica       datetime2     NOT NULL CONSTRAINT DF_Piastre_DataUltimaModifica DEFAULT GETUTCDATE(),
    TipoPiastra              int           NOT NULL CONSTRAINT DF_Piastre_TipoPiastra DEFAULT 0,  -- 0=Standard, 1=SpecialeCliente
    IdClienteEsclusivo       int           NULL,
    CONSTRAINT PK_Piastre PRIMARY KEY (IdPiastra),
    CONSTRAINT FK_Piastre_CategoriePiastre_IdCategoriaPiastra
        FOREIGN KEY (IdCategoriaPiastra)
        REFERENCES dbo.CategoriePiastre (IdCategoriaPiastra)
        ON DELETE SET NULL,
    CONSTRAINT FK_Piastre_FormatiMacchine_IdFormato
        FOREIGN KEY (IdFormato)
        REFERENCES dbo.FormatiMacchine (IdFormato)
        ON DELETE NO ACTION,
    CONSTRAINT FK_Piastre_Clienti_IdClienteEsclusivo
        FOREIGN KEY (IdClienteEsclusivo)
        REFERENCES dbo.Clienti (IdCliente)
        ON DELETE NO ACTION
);
CREATE UNIQUE INDEX IX_Piastre_CodicePiastra
    ON dbo.Piastre (CodicePiastra);
-- Indice filtrato: CodiceArticoloGestionale univoco solo quando valorizzato
CREATE UNIQUE INDEX IX_Piastre_CodiceArticoloGestionale
    ON dbo.Piastre (CodiceArticoloGestionale)
    WHERE [CodiceArticoloGestionale] IS NOT NULL;
CREATE INDEX IX_Piastre_IdCategoriaPiastra
    ON dbo.Piastre (IdCategoriaPiastra);
CREATE INDEX IX_Piastre_IdFormato
    ON dbo.Piastre (IdFormato);
CREATE INDEX IX_Piastre_IdClienteEsclusivo
    ON dbo.Piastre (IdClienteEsclusivo);
GO

-- ---------------------------------------------------------------------------
-- Disegni  (relazione 1:1 con Piastre — FK lato Disegni)
-- FK IdPiastra → Piastre (SET NULL: se la piastra viene eliminata il disegno
--                         resta ma perde il collegamento)
-- L'indice UNIQUE filtrato garantisce al massimo un disegno per piastra.
-- NULL multipli non violano il vincolo UNIQUE (piastre senza disegno).
-- ---------------------------------------------------------------------------
CREATE TABLE dbo.Disegni (
    IdDisegno              int           IDENTITY(1,1) NOT NULL,
    IdPiastra              int           NULL,
    CodiceDisegno          nvarchar(max) NULL,
    NomeFile               nvarchar(max) NULL,
    PercorsoFile           nvarchar(max) NULL,
    VaultId                nvarchar(max) NULL,
    Revisione              nvarchar(max) NULL,
    Formato                nvarchar(max) NULL,
    Stato                  int           NOT NULL,              -- StatoDisegno enum
    DataUltimaModificaFile datetime2     NULL,
    Note                   nvarchar(max) NULL,
    CONSTRAINT PK_Disegni PRIMARY KEY (IdDisegno),
    CONSTRAINT FK_Disegni_Piastre_IdPiastra
        FOREIGN KEY (IdPiastra)
        REFERENCES dbo.Piastre (IdPiastra)
        ON DELETE SET NULL
);
-- Una piastra ha al massimo un disegno; NULL non viola il vincolo
CREATE UNIQUE INDEX IX_Disegni_IdPiastra
    ON dbo.Disegni (IdPiastra)
    WHERE [IdPiastra] IS NOT NULL;
GO

-- ---------------------------------------------------------------------------
-- PiastreMacchineCompatibili  (N:M Piastre ↔ MacchineStandard)
-- ---------------------------------------------------------------------------
CREATE TABLE dbo.PiastreMacchineCompatibili (
    IdCompatibilita    int           IDENTITY(1,1) NOT NULL,
    IdPiastra          int           NOT NULL,
    IdMacchinaStandard int           NOT NULL,
    FonteDato          int           NULL,                      -- FonteDatoCompatibilita enum (nullable)
    DataVerifica       datetime2     NULL,
    UtenteVerifica     nvarchar(max) NULL,
    Attiva             bit           NOT NULL CONSTRAINT DF_PiastreMacchineCompatibili_Attiva DEFAULT 0,
    Note               nvarchar(max) NULL,
    CONSTRAINT PK_PiastreMacchineCompatibili PRIMARY KEY (IdCompatibilita),
    CONSTRAINT FK_PiastreMacchineCompatibili_Piastre_IdPiastra
        FOREIGN KEY (IdPiastra)
        REFERENCES dbo.Piastre (IdPiastra)
        ON DELETE CASCADE,
    CONSTRAINT FK_PiastreMacchineCompatibili_MacchineStandard_IdMacchinaStandard
        FOREIGN KEY (IdMacchinaStandard)
        REFERENCES dbo.MacchineStandard (IdMacchinaStandard)
        ON DELETE CASCADE
);
CREATE UNIQUE INDEX IX_PiastreMacchineCompatibili_IdPiastra_IdMacchinaStandard
    ON dbo.PiastreMacchineCompatibili (IdPiastra, IdMacchinaStandard);
CREATE INDEX IX_PiastreMacchineCompatibili_IdMacchinaStandard
    ON dbo.PiastreMacchineCompatibili (IdMacchinaStandard);
GO

-- ---------------------------------------------------------------------------
-- ClientiMacchine  (associazione Cliente ↔ MacchinaStandard — macchine possedute)
-- ---------------------------------------------------------------------------
CREATE TABLE dbo.ClientiMacchine (
    IdClienteMacchina    int           IDENTITY(1,1) NOT NULL,
    IdCliente            int           NOT NULL,
    IdMacchinaStandard   int           NOT NULL,
    Matricola            nvarchar(max) NULL,
    CodiceInternoCliente nvarchar(max) NULL,
    DataAssociazione     datetime2     NOT NULL,
    Attiva               bit           NOT NULL CONSTRAINT DF_ClientiMacchine_Attiva DEFAULT 0,
    Note                 nvarchar(max) NULL,
    CONSTRAINT PK_ClientiMacchine PRIMARY KEY (IdClienteMacchina),
    CONSTRAINT FK_ClientiMacchine_Clienti_IdCliente
        FOREIGN KEY (IdCliente)
        REFERENCES dbo.Clienti (IdCliente)
        ON DELETE CASCADE,
    CONSTRAINT FK_ClientiMacchine_MacchineStandard_IdMacchinaStandard
        FOREIGN KEY (IdMacchinaStandard)
        REFERENCES dbo.MacchineStandard (IdMacchinaStandard)
        ON DELETE CASCADE
);
CREATE INDEX IX_ClientiMacchine_IdCliente
    ON dbo.ClientiMacchine (IdCliente);
CREATE INDEX IX_ClientiMacchine_IdMacchinaStandard
    ON dbo.ClientiMacchine (IdMacchinaStandard);
GO

-- ---------------------------------------------------------------------------
-- ClientiPiastre  (associazione Cliente ↔ Piastra — piastre in uso)
-- FK IdClienteMacchina opzionale: se valorizzata indica su quale macchina
--   del cliente la piastra è montata. NO ACTION lato DB — EF gestisce ClientSetNull.
-- ---------------------------------------------------------------------------
CREATE TABLE dbo.ClientiPiastre (
    IdClientePiastra  int       IDENTITY(1,1) NOT NULL,
    IdCliente         int       NOT NULL,
    IdPiastra         int       NOT NULL,
    IdClienteMacchina int       NULL,
    DataAssociazione  datetime2 NOT NULL,
    Stato             int       NOT NULL,                       -- StatoClientePiastra enum
    Note              nvarchar(max) NULL,
    CONSTRAINT PK_ClientiPiastre PRIMARY KEY (IdClientePiastra),
    CONSTRAINT FK_ClientiPiastre_Clienti_IdCliente
        FOREIGN KEY (IdCliente)
        REFERENCES dbo.Clienti (IdCliente)
        ON DELETE CASCADE,
    CONSTRAINT FK_ClientiPiastre_Piastre_IdPiastra
        FOREIGN KEY (IdPiastra)
        REFERENCES dbo.Piastre (IdPiastra)
        ON DELETE CASCADE,
    CONSTRAINT FK_ClientiPiastre_ClientiMacchine_IdClienteMacchina
        FOREIGN KEY (IdClienteMacchina)
        REFERENCES dbo.ClientiMacchine (IdClienteMacchina)
        ON DELETE NO ACTION
);
-- Un cliente non può avere la stessa piastra due volte
CREATE UNIQUE INDEX IX_ClientiPiastre_IdCliente_IdPiastra
    ON dbo.ClientiPiastre (IdCliente, IdPiastra);
CREATE INDEX IX_ClientiPiastre_IdPiastra
    ON dbo.ClientiPiastre (IdPiastra);
CREATE INDEX IX_ClientiPiastre_IdClienteMacchina
    ON dbo.ClientiPiastre (IdClienteMacchina);
GO


-- ===========================================================================
-- SEZIONE 3 — Tracciamento migrazioni EF Core
-- Necessario perché dotnet ef database update non rilanci le migrazioni
-- ===========================================================================

CREATE TABLE dbo.__EFMigrationsHistory (
    MigrationId    nvarchar(150) NOT NULL,
    ProductVersion nvarchar(32)  NOT NULL,
    CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY (MigrationId)
);

INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion) VALUES
    ('20260625140000_InitialCreate',                              '8.0.0'),
    ('20260625160000_AddProduttoriFamiglieMacchine',              '8.0.0'),
    ('20260625180000_RinominaFamiglieInFormatiAddFormatoPiastra', '8.0.0'),
    ('20260630063032_RelazionePiastra_1a1Disegno_TipoPiastra',   '8.0.0');
GO


-- ===========================================================================
-- SEZIONE 4 — Seed obbligatori (inseriti dalla migrazione InitialCreate)
-- ===========================================================================

INSERT INTO dbo.CategoriePiastre (Codice, Descrizione, Ordine) VALUES
    ('STD', 'Standard', 1),
    ('SPE', 'Speciale', 2);
GO


-- ===========================================================================
-- SEZIONE 5 — Dati demo (DbSeeder — solo ambienti di sviluppo/test)
-- Elimina o commenta questa sezione su un DB di produzione.
-- ===========================================================================

-- Clienti demo
INSERT INTO dbo.Clienti (CodiceClienteGestionale, RagioneSociale) VALUES
    ('CLI-001', 'Rossi Imballaggi S.r.l.'),
    ('CLI-002', 'Verdi Macchine S.p.A.'),
    ('CLI-003', 'Bianchi Pack S.r.l.'),
    ('CLI-004', 'Neri Automazione S.p.A.');

-- Formati macchina
INSERT INTO dbo.FormatiMacchine (NomeFormato, IsEliminata) VALUES
    ('106', 0),
    ('145', 0),
    ('88',  0);
GO

-- Macchine standard (risolve gli IdFormato per nome)
DECLARE @fmt106 int = (SELECT IdFormato FROM dbo.FormatiMacchine WHERE NomeFormato = '106');
DECLARE @fmt145 int = (SELECT IdFormato FROM dbo.FormatiMacchine WHERE NomeFormato = '145');
DECLARE @fmt88  int = (SELECT IdFormato FROM dbo.FormatiMacchine WHERE NomeFormato = '88');

INSERT INTO dbo.MacchineStandard (CodiceMacchina, NomeMacchina, IdFormato, LarghezzaMm, AltezzaMm, Attiva) VALUES
    ('NOVACUT_106',       'NOVACUT 106',         @fmt106, 760, 1060, 1),
    ('NOVACUT_145',       'NOVACUT 145',         @fmt145, 760, 1450, 1),
    ('EXPERTCUT_106',     'EXPERTCUT 106',       @fmt106, 760, 1060, 1),
    ('EXPERTCUT_145',     'EXPERTCUT 145',       @fmt145, 760, 1450, 1),
    ('SPRINTERA_106PER',  'SPRINTERA 106 PER',   @fmt106, 760, 1060, 1),
    ('MASTERCUT_VECCHIO', 'MASTERCUT (vecchio)', @fmt88,  600,  880, 0);

-- Piastre (Stato: Attiva=0, Obsoleta=1, DaVerificare=2 | TipoPiastra: Standard=0)
DECLARE @now datetime2 = GETUTCDATE();

INSERT INTO dbo.Piastre (CodicePiastra, CodiceArticoloGestionale, Descrizione, IdFormato,
                          Stato, TipoPiastra, DataCreazione, DataUltimaModifica) VALUES
    ('PLT-000245', 'PLT-000245', 'Piastra frontale 106',          @fmt106, 0, 0, DATEADD(day,-120,@now), DATEADD(day,-30, @now)),
    ('PLT-000312', 'PLT-000312', 'Piastra laterale 106 destra',   @fmt106, 0, 0, DATEADD(day,-90, @now), DATEADD(day,-10, @now)),
    ('PLT-000418', 'PLT-000418', 'Piastra coperchio 145',         @fmt145, 0, 0, DATEADD(day,-60, @now), DATEADD(day,-5,  @now)),
    ('PLT-000501', 'PLT-000501', 'Piastra base EXPERTCUT 106',    @fmt106, 2, 0, DATEADD(day,-20, @now), DATEADD(day,-2,  @now)),
    ('PLT-000088', 'PLT-000088', 'Piastra obsoleta MASTERCUT 88', @fmt88,  1, 0, DATEADD(day,-500,@now), DATEADD(day,-200,@now));

-- Disegni 1:1 (Stato: Attivo=0, Obsoleto=1, DaVerificare=2)
-- PLT-000088 non ha disegno → icona warning nell'interfaccia
DECLARE @plt245 int = (SELECT IdPiastra FROM dbo.Piastre WHERE CodicePiastra = 'PLT-000245');
DECLARE @plt312 int = (SELECT IdPiastra FROM dbo.Piastre WHERE CodicePiastra = 'PLT-000312');
DECLARE @plt418 int = (SELECT IdPiastra FROM dbo.Piastre WHERE CodicePiastra = 'PLT-000418');
DECLARE @plt501 int = (SELECT IdPiastra FROM dbo.Piastre WHERE CodicePiastra = 'PLT-000501');

INSERT INTO dbo.Disegni (IdPiastra, CodiceDisegno, NomeFile, PercorsoFile,
                          Revisione, Formato, Stato, DataUltimaModificaFile) VALUES
    (@plt245, 'PLT-000245', 'PLT-000245.dwg', '\\192.168.0.57\SharedSpace\Piastre\Standard\PLT-000245.dwg', 'B', 'DWG', 0, DATEADD(day,-30,@now)),
    (@plt312, 'PLT-000312', 'PLT-000312.dwg', '\\192.168.0.57\SharedSpace\Piastre\Standard\PLT-000312.dwg', 'A', 'DWG', 2, DATEADD(day,-10,@now)),
    (@plt418, 'PLT-000418', 'PLT-000418.pdf', '\\192.168.0.57\SharedSpace\Piastre\Standard\PLT-000418.pdf', 'C', 'PDF', 0, DATEADD(day,-5, @now)),
    (@plt501, 'PLT-000501', 'PLT-000501.dwg', '\\192.168.0.57\SharedSpace\Piastre\Standard\PLT-000501.dwg', 'A', 'DWG', 2, DATEADD(day,-2, @now));

-- Compatibilità Piastra–Macchina
DECLARE @m1 int = (SELECT IdMacchinaStandard FROM dbo.MacchineStandard WHERE CodiceMacchina = 'NOVACUT_106');
DECLARE @m2 int = (SELECT IdMacchinaStandard FROM dbo.MacchineStandard WHERE CodiceMacchina = 'NOVACUT_145');
DECLARE @m3 int = (SELECT IdMacchinaStandard FROM dbo.MacchineStandard WHERE CodiceMacchina = 'EXPERTCUT_106');
DECLARE @m4 int = (SELECT IdMacchinaStandard FROM dbo.MacchineStandard WHERE CodiceMacchina = 'EXPERTCUT_145');
DECLARE @m5 int = (SELECT IdMacchinaStandard FROM dbo.MacchineStandard WHERE CodiceMacchina = 'SPRINTERA_106PER');
DECLARE @m6 int = (SELECT IdMacchinaStandard FROM dbo.MacchineStandard WHERE CodiceMacchina = 'MASTERCUT_VECCHIO');

INSERT INTO dbo.PiastreMacchineCompatibili (IdPiastra, IdMacchinaStandard, Attiva) VALUES
    (@plt245, @m1, 1),   -- PLT-000245 ↔ NOVACUT 106
    (@plt245, @m3, 1),   -- PLT-000245 ↔ EXPERTCUT 106
    (@plt312, @m1, 1),   -- PLT-000312 ↔ NOVACUT 106
    (@plt312, @m5, 1),   -- PLT-000312 ↔ SPRINTERA 106
    (@plt418, @m2, 1),   -- PLT-000418 ↔ NOVACUT 145
    (@plt418, @m4, 1),   -- PLT-000418 ↔ EXPERTCUT 145
    (@plt501, @m3, 1);   -- PLT-000501 ↔ EXPERTCUT 106
GO

-- ClientiMacchine (macchine possedute dai clienti demo)
DECLARE @cCli1 int = (SELECT IdCliente FROM dbo.Clienti WHERE CodiceClienteGestionale = 'CLI-001');
DECLARE @cCli2 int = (SELECT IdCliente FROM dbo.Clienti WHERE CodiceClienteGestionale = 'CLI-002');
DECLARE @cCli3 int = (SELECT IdCliente FROM dbo.Clienti WHERE CodiceClienteGestionale = 'CLI-003');
DECLARE @cCli4 int = (SELECT IdCliente FROM dbo.Clienti WHERE CodiceClienteGestionale = 'CLI-004');

DECLARE @mx1 int = (SELECT IdMacchinaStandard FROM dbo.MacchineStandard WHERE CodiceMacchina = 'NOVACUT_106');
DECLARE @mx2 int = (SELECT IdMacchinaStandard FROM dbo.MacchineStandard WHERE CodiceMacchina = 'NOVACUT_145');
DECLARE @mx3 int = (SELECT IdMacchinaStandard FROM dbo.MacchineStandard WHERE CodiceMacchina = 'EXPERTCUT_106');
DECLARE @mx5 int = (SELECT IdMacchinaStandard FROM dbo.MacchineStandard WHERE CodiceMacchina = 'SPRINTERA_106PER');
DECLARE @mx6 int = (SELECT IdMacchinaStandard FROM dbo.MacchineStandard WHERE CodiceMacchina = 'MASTERCUT_VECCHIO');

DECLARE @now2 datetime2 = GETUTCDATE();

INSERT INTO dbo.ClientiMacchine (IdCliente, IdMacchinaStandard, Matricola, DataAssociazione, Attiva) VALUES
    (@cCli1, @mx1, 'NC106-2019-001', DATEADD(year,-4, @now2), 1),
    (@cCli1, @mx3, 'EC106-2021-007', DATEADD(year,-2, @now2), 1),
    (@cCli2, @mx1, 'NC106-2020-003', DATEADD(year,-3, @now2), 1),
    (@cCli2, @mx2, 'NC145-2022-002', DATEADD(year,-1, @now2), 1),
    (@cCli3, @mx5, 'SP106-2023-001', DATEADD(month,-8,@now2), 1),
    (@cCli4, @mx6, 'MC88-2015-001',  DATEADD(year,-8, @now2), 1);

-- ClientiPiastre (Stato: Attiva=0)
DECLARE @cm1 int = (SELECT IdClienteMacchina FROM dbo.ClientiMacchine WHERE IdCliente = @cCli1 AND IdMacchinaStandard = @mx1);
DECLARE @cm3 int = (SELECT IdClienteMacchina FROM dbo.ClientiMacchine WHERE IdCliente = @cCli2 AND IdMacchinaStandard = @mx1);
DECLARE @cm4 int = (SELECT IdClienteMacchina FROM dbo.ClientiMacchine WHERE IdCliente = @cCli2 AND IdMacchinaStandard = @mx2);
DECLARE @cm5 int = (SELECT IdClienteMacchina FROM dbo.ClientiMacchine WHERE IdCliente = @cCli3 AND IdMacchinaStandard = @mx5);

DECLARE @px245 int = (SELECT IdPiastra FROM dbo.Piastre WHERE CodicePiastra = 'PLT-000245');
DECLARE @px312 int = (SELECT IdPiastra FROM dbo.Piastre WHERE CodicePiastra = 'PLT-000312');
DECLARE @px418 int = (SELECT IdPiastra FROM dbo.Piastre WHERE CodicePiastra = 'PLT-000418');

INSERT INTO dbo.ClientiPiastre (IdCliente, IdPiastra, IdClienteMacchina, DataAssociazione, Stato) VALUES
    (@cCli1, @px245, @cm1, DATEADD(day,-100,@now2), 0),  -- CLI-001 → PLT-000245 (su NOVACUT 106)
    (@cCli1, @px312, @cm1, DATEADD(day,-80, @now2), 0),  -- CLI-001 → PLT-000312 (su NOVACUT 106)
    (@cCli2, @px245, @cm3, DATEADD(day,-60, @now2), 0),  -- CLI-002 → PLT-000245 (su NOVACUT 106)
    (@cCli2, @px418, @cm4, DATEADD(day,-30, @now2), 0),  -- CLI-002 → PLT-000418 (su NOVACUT 145)
    (@cCli3, @px312, @cm5, DATEADD(day,-15, @now2), 0);  -- CLI-003 → PLT-000312 (su SPRINTERA 106)
GO

-- ===========================================================================
-- Fine script
-- ===========================================================================
