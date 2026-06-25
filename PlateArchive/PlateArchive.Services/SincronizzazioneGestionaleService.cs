using System.Data.Odbc;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;

namespace PlateArchive.Services;

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
                else if (esistente.RagioneSociale != ragSoc)
                {
                    esistente.RagioneSociale = ragSoc;
                    await clienteRepo.UpdateAsync(esistente);
                    aggiornati++;
                }
                else
                {
                    invariati++;
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
