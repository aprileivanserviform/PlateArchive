using System.Data.Odbc;
using PlateArchive.Core.Enums;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;

namespace PlateArchive.Services;

/// <summary>
/// Sincronizza i clienti da DB2 (PANTH01) verso il database locale via ODBC.
/// La query deve restituire le colonne in ordine: codice, ragione_sociale, p_iva.
/// </summary>
public class SincronizzazioneGestionaleService(
    string connectionString,
    string queryClienti,
    IClienteRepository clienteRepo) : ISincronizzazioneGestionaleService
{
    public bool IsDisponibile => !string.IsNullOrWhiteSpace(connectionString);

    public async Task<SincronizzazioneResult> SincronizzaClientiAsync(CancellationToken ct = default)
    {
        if (!IsDisponibile)
            return new SincronizzazioneResult(0, 0, 0, "Stringa di connessione DB2 non configurata.");

        int inseriti = 0, aggiornati = 0, invariati = 0;

        try
        {
            using var conn = new OdbcConnection(connectionString);
            await conn.OpenAsync(ct);

            using var cmd = new OdbcCommand(queryClienti, conn);
            using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                ct.ThrowIfCancellationRequested();

                var codice = reader.IsDBNull(0) ? null : reader.GetString(0).Trim();
                if (string.IsNullOrEmpty(codice)) { invariati++; continue; }

                var ragSoc = reader.IsDBNull(1) ? codice : reader.GetString(1).Trim();
                var pIva   = reader.IsDBNull(2) ? null   : reader.GetString(2).Trim();
                if (string.IsNullOrEmpty(pIva)) pIva = null;

                var esistente = await clienteRepo.GetByCodiceGestionaleAsync(codice);

                if (esistente is null)
                {
                    await clienteRepo.AddAsync(new Cliente
                    {
                        CodiceClienteGestionale  = codice,
                        RagioneSociale           = ragSoc,
                        PartitaIVA               = pIva,
                        StatoCliente             = StatoCliente.Attivo,
                        DataUltimaSincronizzazione = DateTime.UtcNow
                    });
                    inseriti++;
                }
                else
                {
                    var cambiato = esistente.RagioneSociale != ragSoc || esistente.PartitaIVA != pIva;
                    if (cambiato)
                    {
                        esistente.RagioneSociale            = ragSoc;
                        esistente.PartitaIVA                = pIva;
                        esistente.DataUltimaSincronizzazione = DateTime.UtcNow;
                        await clienteRepo.UpdateAsync(esistente);
                        aggiornati++;
                    }
                    else
                    {
                        invariati++;
                    }
                }
            }

            return new SincronizzazioneResult(inseriti, aggiornati, invariati, null);
        }
        catch (OperationCanceledException)
        {
            return new SincronizzazioneResult(inseriti, aggiornati, invariati, "Operazione annullata.");
        }
        catch (Exception ex)
        {
            return new SincronizzazioneResult(inseriti, aggiornati, invariati, ex.Message);
        }
    }
}
