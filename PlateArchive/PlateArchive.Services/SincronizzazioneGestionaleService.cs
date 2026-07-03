using System.Data.Odbc;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;

namespace PlateArchive.Services;

/// <summary>
/// Implementazione della sincronizzazione clienti dal gestionale DB2.
/// Usa ODBC (non un driver .NET nativo) perché DB2 su AS/400 è accessibile
/// solo tramite il driver ODBC IBM installato sui PC aziendali via VPN.
/// La query è configurabile in appsettings.json (Db2:QueryClienti).
/// </summary>
public class SincronizzazioneGestionaleService(
    string connectionString,
    string queryClienti,
    IClienteRepository clienteRepo) : ISincronizzazioneGestionaleService
{
    public bool IsDisponibile => !string.IsNullOrWhiteSpace(connectionString);

    public async Task<SincronizzazioneResult> SincronizzaClientiAsync(CancellationToken ct = default)
    {
        // Se la stringa di connessione non è configurata (sviluppo locale senza VPN), esce subito.
        if (!IsDisponibile)
            return new SincronizzazioneResult(0, 0, 0, "Stringa di connessione DB2 non configurata.");

        int inseriti = 0, aggiornati = 0, invariati = 0, annullati = 0;

        try
        {
            using var conn   = new OdbcConnection(connectionString);
            await conn.OpenAsync(ct);

            using var cmd    = new OdbcCommand(queryClienti, conn);
            using var reader = await cmd.ExecuteReaderAsync(ct);

            // Codici restituiti dal gestionale (solo clienti validi: la query filtra STATO = 'V').
            // Serve a fine ciclo per marcare come annullati i clienti locali non più presenti.
            var codiciValidi = new HashSet<string>();

            while (await reader.ReadAsync(ct))
            {
                ct.ThrowIfCancellationRequested();

                var codice = reader.IsDBNull(0) ? null : reader.GetString(0).Trim();
                if (string.IsNullOrEmpty(codice)) { invariati++; continue; }

                codiciValidi.Add(codice);

                var ragSoc = reader.IsDBNull(1) ? codice : reader.GetString(1).Trim();

                // Strategia upsert: se esiste aggiorno la ragione sociale, altrimenti inserisco.
                // Un cliente marcato annullato che ricompare tra i validi viene riattivato.
                var esistente = await clienteRepo.GetByCodiceGestionaleAsync(codice);

                if (esistente is null)
                {
                    await clienteRepo.AddAsync(new Cliente
                    {
                        CodiceClienteGestionale = codice,
                        RagioneSociale          = ragSoc,
                    });
                    inseriti++;
                }
                else if (esistente.RagioneSociale != ragSoc || !esistente.AttivoGestionale)
                {
                    esistente.RagioneSociale   = ragSoc;
                    esistente.AttivoGestionale = true;
                    await clienteRepo.UpdateAsync(esistente);
                    aggiornati++;
                }
                else
                {
                    invariati++;
                }
            }

            // Clienti locali assenti dal gestionale → annullati (mai eliminati: lo storico
            // piastre/macchine resta intatto). Se la query non ha restituito righe si salta:
            // più probabile un problema di query/connessione che un annullamento di massa.
            if (codiciValidi.Count > 0)
            {
                foreach (var cliente in await clienteRepo.GetAllAsync())
                {
                    ct.ThrowIfCancellationRequested();

                    if (cliente.AttivoGestionale && !codiciValidi.Contains(cliente.CodiceClienteGestionale))
                    {
                        cliente.AttivoGestionale = false;
                        await clienteRepo.UpdateAsync(cliente);
                        annullati++;
                    }
                }
            }

            return new SincronizzazioneResult(inseriti, aggiornati, invariati, null, annullati);
        }
        catch (OperationCanceledException)
        {
            return new SincronizzazioneResult(inseriti, aggiornati, invariati, "Operazione annullata.", annullati);
        }
        catch (Exception ex)
        {
            return new SincronizzazioneResult(inseriti, aggiornati, invariati, ex.Message, annullati);
        }
    }
}
