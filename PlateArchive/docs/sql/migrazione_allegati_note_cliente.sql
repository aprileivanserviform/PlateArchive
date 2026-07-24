-- ============================================================
-- Migrazione: AllegatiNoteCliente
-- Data:       2026-07-16
-- Descrizione: Aggiunge le tabelle AllegatiClienti e NoteTecnicheClienti
--              per la gestione di allegati e note tecniche per cliente.
-- ============================================================

-- Tabella allegati cliente
CREATE TABLE [dbo].[AllegatiClienti] (
    [IdAllegato]      INT            NOT NULL IDENTITY(1,1),
    [IdCliente]       INT            NOT NULL,
    [NomeFile]        NVARCHAR(MAX)  NOT NULL,
    [PercorsoFile]    NVARCHAR(MAX)  NOT NULL,
    [Descrizione]     NVARCHAR(MAX)  NULL,
    [DimensioneBytes] BIGINT         NOT NULL,
    [DataCaricamento] DATETIME2      NOT NULL,

    CONSTRAINT [PK_AllegatiClienti] PRIMARY KEY ([IdAllegato]),
    CONSTRAINT [FK_AllegatiClienti_Clienti_IdCliente]
        FOREIGN KEY ([IdCliente]) REFERENCES [dbo].[Clienti] ([IdCliente])
        ON DELETE CASCADE
);

CREATE INDEX [IX_AllegatiClienti_IdCliente]
    ON [dbo].[AllegatiClienti] ([IdCliente]);

-- Tabella note tecniche cliente
CREATE TABLE [dbo].[NoteTecnicheClienti] (
    [IdNota]        INT            NOT NULL IDENTITY(1,1),
    [IdCliente]     INT            NOT NULL,
    [Titolo]        NVARCHAR(MAX)  NOT NULL,
    [Testo]         NVARCHAR(MAX)  NULL,
    [DataCreazione] DATETIME2      NOT NULL,
    [DataModifica]  DATETIME2      NOT NULL,

    CONSTRAINT [PK_NoteTecnicheClienti] PRIMARY KEY ([IdNota]),
    CONSTRAINT [FK_NoteTecnicheClienti_Clienti_IdCliente]
        FOREIGN KEY ([IdCliente]) REFERENCES [dbo].[Clienti] ([IdCliente])
        ON DELETE CASCADE
);

CREATE INDEX [IX_NoteTecnicheClienti_IdCliente]
    ON [dbo].[NoteTecnicheClienti] ([IdCliente]);

-- Aggiorna tabella __EFMigrationsHistory
INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260716063245_AllegatiNoteCliente', N'9.0.0');
