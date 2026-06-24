using Microsoft.EntityFrameworkCore;
using PlateArchive.Core.Enums;
using PlateArchive.Core.Models;

namespace PlateArchive.Data;

/// <summary>
/// Popola il database con dati di esempio solo se è vuoto (utilizzo in sviluppo).
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(PlateArchiveDbContext db)
    {
        if (await db.Clienti.AnyAsync()) return;

        // ── Clienti ──────────────────────────────────────────────
        var clienti = new[]
        {
            new Cliente { CodiceClienteGestionale = "CLI-001", RagioneSociale = "Rossi Imballaggi S.r.l.", PartitaIVA = "01234567890", StatoCliente = StatoCliente.Attivo },
            new Cliente { CodiceClienteGestionale = "CLI-002", RagioneSociale = "Verdi Macchine S.p.A.",   PartitaIVA = "09876543210", StatoCliente = StatoCliente.Attivo },
            new Cliente { CodiceClienteGestionale = "CLI-003", RagioneSociale = "Bianchi Pack S.r.l.",     PartitaIVA = "05555555550", StatoCliente = StatoCliente.Attivo },
            new Cliente { CodiceClienteGestionale = "CLI-004", RagioneSociale = "Neri Automazione S.p.A.", PartitaIVA = "07777777770", StatoCliente = StatoCliente.Storico },
        };
        db.Clienti.AddRange(clienti);

        // ── Macchine Standard ────────────────────────────────────
        var macchine = new[]
        {
            new MacchinaStandard { CodiceMacchina = "NOVACUT_106",      NomeMacchina = "NOVACUT 106",         Famiglia = "NOVACUT",    Formato = "106", Attiva = true },
            new MacchinaStandard { CodiceMacchina = "NOVACUT_145",      NomeMacchina = "NOVACUT 145",         Famiglia = "NOVACUT",    Formato = "145", Attiva = true },
            new MacchinaStandard { CodiceMacchina = "EXPERTCUT_106",    NomeMacchina = "EXPERTCUT 106",       Famiglia = "EXPERTCUT",  Formato = "106", Attiva = true },
            new MacchinaStandard { CodiceMacchina = "EXPERTCUT_145",    NomeMacchina = "EXPERTCUT 145",       Famiglia = "EXPERTCUT",  Formato = "145", Attiva = true },
            new MacchinaStandard { CodiceMacchina = "SPRINTERA_106PER", NomeMacchina = "SPRINTERA 106 PER",   Famiglia = "SPRINTERA",  Formato = "106", Attiva = true },
            new MacchinaStandard { CodiceMacchina = "MASTERCUT_VECCHIO",NomeMacchina = "MASTERCUT (vecchio)", Famiglia = "MASTERCUT",  Formato = "88",  Attiva = false },
        };
        db.MacchineStandard.AddRange(macchine);

        // ── Piastre ──────────────────────────────────────────────
        var now = DateTime.UtcNow;
        var piastre = new[]
        {
            new Piastra { CodicePiastra = "PLT-000245", CodiceArticoloGestionale = "PLT-000245", Descrizione = "Piastra frontale 106",         Stato = StatoPiastra.Attiva,      DataCreazione = now.AddDays(-120), DataUltimaModifica = now.AddDays(-30) },
            new Piastra { CodicePiastra = "PLT-000312", CodiceArticoloGestionale = "PLT-000312", Descrizione = "Piastra laterale 106 destra",   Stato = StatoPiastra.Attiva,      DataCreazione = now.AddDays(-90),  DataUltimaModifica = now.AddDays(-10) },
            new Piastra { CodicePiastra = "PLT-000418", CodiceArticoloGestionale = "PLT-000418", Descrizione = "Piastra coperchio 145",         Stato = StatoPiastra.Attiva,      DataCreazione = now.AddDays(-60),  DataUltimaModifica = now.AddDays(-5)  },
            new Piastra { CodicePiastra = "PLT-000501", CodiceArticoloGestionale = "PLT-000501", Descrizione = "Piastra base EXPERTCUT 106",    Stato = StatoPiastra.DaVerificare, DataCreazione = now.AddDays(-20),  DataUltimaModifica = now.AddDays(-2)  },
            new Piastra { CodicePiastra = "PLT-000088", CodiceArticoloGestionale = "PLT-000088", Descrizione = "Piastra obsoleta MASTERCUT 88", Stato = StatoPiastra.Obsoleta,    DataCreazione = now.AddDays(-500), DataUltimaModifica = now.AddDays(-200)},
        };
        db.Piastre.AddRange(piastre);

        await db.SaveChangesAsync();

        // ── Disegni (1:1 con Piastra) ────────────────────────────
        var disegni = new[]
        {
            new Disegno { IdPiastra = piastre[0].IdPiastra, CodiceDisegno = "PLT-000245", NomeFile = "PLT-000245.dwg", PercorsoFile = @"\\server\disegni\PLT-000245.dwg", Revisione = "B", Formato = "DWG", Stato = StatoDisegno.Attivo,        DataUltimaModificaFile = now.AddDays(-30) },
            new Disegno { IdPiastra = piastre[1].IdPiastra, CodiceDisegno = "PLT-000312", NomeFile = "PLT-000312.dwg", PercorsoFile = @"\\server\disegni\PLT-000312.dwg", Revisione = "A", Formato = "DWG", Stato = StatoDisegno.DaVerificare,  DataUltimaModificaFile = now.AddDays(-10) },
            new Disegno { IdPiastra = piastre[2].IdPiastra, CodiceDisegno = "PLT-000418", NomeFile = "PLT-000418.pdf", PercorsoFile = @"\\server\disegni\PLT-000418.pdf", Revisione = "C", Formato = "PDF", Stato = StatoDisegno.Attivo,        DataUltimaModificaFile = now.AddDays(-5)  },
            new Disegno { IdPiastra = piastre[3].IdPiastra, CodiceDisegno = "PLT-000501", NomeFile = "PLT-000501.dwg", PercorsoFile = @"\\server\disegni\PLT-000501.dwg", Revisione = "A", Formato = "DWG", Stato = StatoDisegno.DaVerificare,  DataUltimaModificaFile = now.AddDays(-2)  },
        };
        db.Disegni.AddRange(disegni);

        // ── Compatibilità Piastra–Macchina ───────────────────────
        var compat = new[]
        {
            // PLT-000245: NOVACUT 106 + EXPERTCUT 106
            new PiastraMacchinaCompatibile { IdPiastra = piastre[0].IdPiastra, IdMacchinaStandard = macchine[0].IdMacchinaStandard, Attiva = true },
            new PiastraMacchinaCompatibile { IdPiastra = piastre[0].IdPiastra, IdMacchinaStandard = macchine[2].IdMacchinaStandard, Attiva = true },
            // PLT-000312: NOVACUT 106 + SPRINTERA 106
            new PiastraMacchinaCompatibile { IdPiastra = piastre[1].IdPiastra, IdMacchinaStandard = macchine[0].IdMacchinaStandard, Attiva = true },
            new PiastraMacchinaCompatibile { IdPiastra = piastre[1].IdPiastra, IdMacchinaStandard = macchine[4].IdMacchinaStandard, Attiva = true },
            // PLT-000418: NOVACUT 145 + EXPERTCUT 145
            new PiastraMacchinaCompatibile { IdPiastra = piastre[2].IdPiastra, IdMacchinaStandard = macchine[1].IdMacchinaStandard, Attiva = true },
            new PiastraMacchinaCompatibile { IdPiastra = piastre[2].IdPiastra, IdMacchinaStandard = macchine[3].IdMacchinaStandard, Attiva = true },
            // PLT-000501: EXPERTCUT 106
            new PiastraMacchinaCompatibile { IdPiastra = piastre[3].IdPiastra, IdMacchinaStandard = macchine[2].IdMacchinaStandard, Attiva = true },
        };
        db.PiastreMacchineCompatibili.AddRange(compat);

        // ── ClienteMacchina (macchine possedute dai clienti) ─────
        var clientiMacchine = new[]
        {
            new ClienteMacchina { IdCliente = clienti[0].IdCliente, IdMacchinaStandard = macchine[0].IdMacchinaStandard, Matricola = "NC106-2019-001", DataAssociazione = now.AddYears(-4) },
            new ClienteMacchina { IdCliente = clienti[0].IdCliente, IdMacchinaStandard = macchine[2].IdMacchinaStandard, Matricola = "EC106-2021-007", DataAssociazione = now.AddYears(-2) },
            new ClienteMacchina { IdCliente = clienti[1].IdCliente, IdMacchinaStandard = macchine[0].IdMacchinaStandard, Matricola = "NC106-2020-003", DataAssociazione = now.AddYears(-3) },
            new ClienteMacchina { IdCliente = clienti[1].IdCliente, IdMacchinaStandard = macchine[1].IdMacchinaStandard, Matricola = "NC145-2022-002", DataAssociazione = now.AddYears(-1) },
            new ClienteMacchina { IdCliente = clienti[2].IdCliente, IdMacchinaStandard = macchine[4].IdMacchinaStandard, Matricola = "SP106-2023-001", DataAssociazione = now.AddMonths(-8) },
            new ClienteMacchina { IdCliente = clienti[3].IdCliente, IdMacchinaStandard = macchine[5].IdMacchinaStandard, Matricola = "MC88-2015-001",  DataAssociazione = now.AddYears(-8) },
        };
        db.ClientiMacchine.AddRange(clientiMacchine);

        await db.SaveChangesAsync();

        // ── ClientePiastra (piastre associate ai clienti) ────────
        var clientiPiastre = new[]
        {
            // Rossi: PLT-000245 su NOVACUT 106, PLT-000312 su NOVACUT 106
            new ClientePiastra { IdCliente = clienti[0].IdCliente, IdPiastra = piastre[0].IdPiastra, IdClienteMacchina = clientiMacchine[0].IdClienteMacchina, DataAssociazione = now.AddDays(-100), Stato = StatoClientePiastra.Attiva },
            new ClientePiastra { IdCliente = clienti[0].IdCliente, IdPiastra = piastre[1].IdPiastra, IdClienteMacchina = clientiMacchine[0].IdClienteMacchina, DataAssociazione = now.AddDays(-80),  Stato = StatoClientePiastra.Attiva },
            // Verdi: PLT-000245 su NOVACUT 106, PLT-000418 su NOVACUT 145
            new ClientePiastra { IdCliente = clienti[1].IdCliente, IdPiastra = piastre[0].IdPiastra, IdClienteMacchina = clientiMacchine[2].IdClienteMacchina, DataAssociazione = now.AddDays(-60),  Stato = StatoClientePiastra.Attiva },
            new ClientePiastra { IdCliente = clienti[1].IdCliente, IdPiastra = piastre[2].IdPiastra, IdClienteMacchina = clientiMacchine[3].IdClienteMacchina, DataAssociazione = now.AddDays(-30),  Stato = StatoClientePiastra.Attiva },
            // Bianchi: PLT-000312 su SPRINTERA 106
            new ClientePiastra { IdCliente = clienti[2].IdCliente, IdPiastra = piastre[1].IdPiastra, IdClienteMacchina = clientiMacchine[4].IdClienteMacchina, DataAssociazione = now.AddDays(-15),  Stato = StatoClientePiastra.Attiva },
        };
        db.ClientiPiastre.AddRange(clientiPiastre);

        await db.SaveChangesAsync();
    }
}
