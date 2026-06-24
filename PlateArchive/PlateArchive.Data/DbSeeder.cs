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

        var now = DateTime.UtcNow;

        // ── Macchine Standard (radici, senza FK) ─────────────────
        var nc106 = new MacchinaStandard { CodiceMacchina = "NOVACUT_106",       NomeMacchina = "NOVACUT 106",          Famiglia = "NOVACUT",    Formato = "106", Attiva = true  };
        var nc145 = new MacchinaStandard { CodiceMacchina = "NOVACUT_145",       NomeMacchina = "NOVACUT 145",          Famiglia = "NOVACUT",    Formato = "145", Attiva = true  };
        var ec106 = new MacchinaStandard { CodiceMacchina = "EXPERTCUT_106",     NomeMacchina = "EXPERTCUT 106",        Famiglia = "EXPERTCUT",  Formato = "106", Attiva = true  };
        var ec145 = new MacchinaStandard { CodiceMacchina = "EXPERTCUT_145",     NomeMacchina = "EXPERTCUT 145",        Famiglia = "EXPERTCUT",  Formato = "145", Attiva = true  };
        var sp106 = new MacchinaStandard { CodiceMacchina = "SPRINTERA_106PER",  NomeMacchina = "SPRINTERA 106 PER",    Famiglia = "SPRINTERA",  Formato = "106", Attiva = true  };
        var mc88  = new MacchinaStandard { CodiceMacchina = "MASTERCUT_VECCHIO", NomeMacchina = "MASTERCUT (vecchio)",  Famiglia = "MASTERCUT",  Formato = "88",  Attiva = false };

        // ── Piastre con Disegno e Compatibilità via navigation ───
        var plt245 = new Piastra
        {
            CodicePiastra = "PLT-000245", CodiceArticoloGestionale = "PLT-000245",
            Descrizione = "Piastra frontale 106", Stato = StatoPiastra.Attiva,
            DataCreazione = now.AddDays(-120), DataUltimaModifica = now.AddDays(-30),
            Disegno = new Disegno
            {
                CodiceDisegno = "PLT-000245", NomeFile = "PLT-000245.dwg",
                PercorsoFile = @"\\server\disegni\PLT-000245.dwg",
                Revisione = "B", Formato = "DWG", Stato = StatoDisegno.Attivo,
                DataUltimaModificaFile = now.AddDays(-30)
            },
            MacchineCompatibili =
            [
                new() { MacchinaStandard = nc106, Attiva = true },
                new() { MacchinaStandard = ec106, Attiva = true }
            ]
        };

        var plt312 = new Piastra
        {
            CodicePiastra = "PLT-000312", CodiceArticoloGestionale = "PLT-000312",
            Descrizione = "Piastra laterale 106 destra", Stato = StatoPiastra.Attiva,
            DataCreazione = now.AddDays(-90), DataUltimaModifica = now.AddDays(-10),
            Disegno = new Disegno
            {
                CodiceDisegno = "PLT-000312", NomeFile = "PLT-000312.dwg",
                PercorsoFile = @"\\server\disegni\PLT-000312.dwg",
                Revisione = "A", Formato = "DWG", Stato = StatoDisegno.DaVerificare,
                DataUltimaModificaFile = now.AddDays(-10)
            },
            MacchineCompatibili =
            [
                new() { MacchinaStandard = nc106, Attiva = true },
                new() { MacchinaStandard = sp106, Attiva = true }
            ]
        };

        var plt418 = new Piastra
        {
            CodicePiastra = "PLT-000418", CodiceArticoloGestionale = "PLT-000418",
            Descrizione = "Piastra coperchio 145", Stato = StatoPiastra.Attiva,
            DataCreazione = now.AddDays(-60), DataUltimaModifica = now.AddDays(-5),
            Disegno = new Disegno
            {
                CodiceDisegno = "PLT-000418", NomeFile = "PLT-000418.pdf",
                PercorsoFile = @"\\server\disegni\PLT-000418.pdf",
                Revisione = "C", Formato = "PDF", Stato = StatoDisegno.Attivo,
                DataUltimaModificaFile = now.AddDays(-5)
            },
            MacchineCompatibili =
            [
                new() { MacchinaStandard = nc145, Attiva = true },
                new() { MacchinaStandard = ec145, Attiva = true }
            ]
        };

        var plt501 = new Piastra
        {
            CodicePiastra = "PLT-000501", CodiceArticoloGestionale = "PLT-000501",
            Descrizione = "Piastra base EXPERTCUT 106", Stato = StatoPiastra.DaVerificare,
            DataCreazione = now.AddDays(-20), DataUltimaModifica = now.AddDays(-2),
            Disegno = new Disegno
            {
                CodiceDisegno = "PLT-000501", NomeFile = "PLT-000501.dwg",
                PercorsoFile = @"\\server\disegni\PLT-000501.dwg",
                Revisione = "A", Formato = "DWG", Stato = StatoDisegno.DaVerificare,
                DataUltimaModificaFile = now.AddDays(-2)
            },
            MacchineCompatibili = [new() { MacchinaStandard = ec106, Attiva = true }]
        };

        var plt088 = new Piastra
        {
            CodicePiastra = "PLT-000088", CodiceArticoloGestionale = "PLT-000088",
            Descrizione = "Piastra obsoleta MASTERCUT 88", Stato = StatoPiastra.Obsoleta,
            DataCreazione = now.AddDays(-500), DataUltimaModifica = now.AddDays(-200)
        };

        // ── Clienti con Macchine via navigation ───────────────────
        // Variabili nominali per i ClienteMacchina: servono gli ID per ClientePiastra
        var rossiNc106 = new ClienteMacchina { MacchinaStandard = nc106, Matricola = "NC106-2019-001", DataAssociazione = now.AddYears(-4) };
        var rossiEc106 = new ClienteMacchina { MacchinaStandard = ec106, Matricola = "EC106-2021-007", DataAssociazione = now.AddYears(-2) };
        var verdiNc106 = new ClienteMacchina { MacchinaStandard = nc106, Matricola = "NC106-2020-003", DataAssociazione = now.AddYears(-3) };
        var verdiNc145 = new ClienteMacchina { MacchinaStandard = nc145, Matricola = "NC145-2022-002", DataAssociazione = now.AddYears(-1) };
        var bianchiSp106 = new ClienteMacchina { MacchinaStandard = sp106, Matricola = "SP106-2023-001", DataAssociazione = now.AddMonths(-8) };

        var rossi  = new Cliente { CodiceClienteGestionale = "CLI-001", RagioneSociale = "Rossi Imballaggi S.r.l.", PartitaIVA = "01234567890", StatoCliente = StatoCliente.Attivo,  Macchine = [rossiNc106, rossiEc106] };
        var verdi  = new Cliente { CodiceClienteGestionale = "CLI-002", RagioneSociale = "Verdi Macchine S.p.A.",   PartitaIVA = "09876543210", StatoCliente = StatoCliente.Attivo,  Macchine = [verdiNc106, verdiNc145] };
        var bianchi = new Cliente { CodiceClienteGestionale = "CLI-003", RagioneSociale = "Bianchi Pack S.r.l.",     PartitaIVA = "05555555550", StatoCliente = StatoCliente.Attivo,  Macchine = [bianchiSp106] };
        var neri   = new Cliente { CodiceClienteGestionale = "CLI-004", RagioneSociale = "Neri Automazione S.p.A.", PartitaIVA = "07777777770", StatoCliente = StatoCliente.Storico, Macchine = [new() { MacchinaStandard = mc88, Matricola = "MC88-2015-001", DataAssociazione = now.AddYears(-8) }] };

        db.MacchineStandard.AddRange(nc106, nc145, ec106, ec145, sp106, mc88);
        db.Piastre.AddRange(plt245, plt312, plt418, plt501, plt088);
        db.Clienti.AddRange(rossi, verdi, bianchi, neri);

        // EF Core ordina gli INSERT seguendo il grafo: MacchineStandard → Piastre → Disegni
        // → PiastreMacchineCompatibili; e MacchineStandard → Clienti → ClientiMacchine
        await db.SaveChangesAsync();

        // ── ClientePiastre (tutti gli ID sono ora disponibili) ────
        db.ClientiPiastre.AddRange(
            new ClientePiastra { IdCliente = rossi.IdCliente,   IdPiastra = plt245.IdPiastra, IdClienteMacchina = rossiNc106.IdClienteMacchina,   DataAssociazione = now.AddDays(-100), Stato = StatoClientePiastra.Attiva },
            new ClientePiastra { IdCliente = rossi.IdCliente,   IdPiastra = plt312.IdPiastra, IdClienteMacchina = rossiNc106.IdClienteMacchina,   DataAssociazione = now.AddDays(-80),  Stato = StatoClientePiastra.Attiva },
            new ClientePiastra { IdCliente = verdi.IdCliente,   IdPiastra = plt245.IdPiastra, IdClienteMacchina = verdiNc106.IdClienteMacchina,   DataAssociazione = now.AddDays(-60),  Stato = StatoClientePiastra.Attiva },
            new ClientePiastra { IdCliente = verdi.IdCliente,   IdPiastra = plt418.IdPiastra, IdClienteMacchina = verdiNc145.IdClienteMacchina,   DataAssociazione = now.AddDays(-30),  Stato = StatoClientePiastra.Attiva },
            new ClientePiastra { IdCliente = bianchi.IdCliente, IdPiastra = plt312.IdPiastra, IdClienteMacchina = bianchiSp106.IdClienteMacchina, DataAssociazione = now.AddDays(-15),  Stato = StatoClientePiastra.Attiva }
        );
        await db.SaveChangesAsync();
    }
}
