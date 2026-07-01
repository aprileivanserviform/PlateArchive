using System.Data.Odbc;

namespace PlateArchive.Services;

/// <summary>
/// Implementazione della lettura live delle righe ordine di vendita dal gestionale DB2 (Panthera).
/// Stessa connessione ODBC/VPN già usata da <see cref="SincronizzazioneGestionaleService"/> per i
/// Clienti. La query (configurabile in appsettings.json, Db2:QueryRigheOrdineVendita) unisce
/// THIP.ORD_VEN_RIG (righe), THIP.ORD_VEN_TES (testata, per risalire al cliente) e
/// FINANCE.BBCLIPT (ragione sociale cliente) — stesso schema/join già usato per i Clienti.
/// </summary>
public class RigheOrdineVenditaService(string connectionString, string queryRigheOrdine)
    : IRigheOrdineVenditaService
{
    public bool IsDisponibile => !string.IsNullOrWhiteSpace(connectionString);

    public async Task<IReadOnlyList<RigaOrdineVendita>> LeggiRigheInevaseAsync(CancellationToken ct = default)
    {
        if (!IsDisponibile)
            throw new InvalidOperationException("Stringa di connessione DB2 non configurata.");

        var righe = new List<RigaOrdineVendita>();

        using var conn = new OdbcConnection(connectionString);
        await conn.OpenAsync(ct);

        using var cmd    = new OdbcCommand(queryRigheOrdine, conn);
        using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            ct.ThrowIfCancellationRequested();

            var articolo = reader.IsDBNull(3) ? null : reader.GetValue(3).ToString()?.Trim();
            if (string.IsNullOrEmpty(articolo)) continue;

            righe.Add(new RigaOrdineVendita(
                AnnoOrdine:             ToInt(reader.GetValue(0)),
                NumeroOrdine:           ToInt(reader.GetValue(1)),
                RigaOrdine:             ToInt(reader.GetValue(2)),
                CodiceArticolo:         articolo,
                RagioneSocialeCliente:  reader.IsDBNull(4) ? string.Empty : reader.GetValue(4).ToString()!.Trim()));
        }

        return righe;
    }

    // I campi numerici DB2/AS400 arrivano via ODBC come decimal o come stringa a seconda del
    // driver: normalizziamo qui invece di assumere un tipo .NET specifico.
    private static int ToInt(object value) => Convert.ToInt32(value);
}
