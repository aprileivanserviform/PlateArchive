using System.Data.Common;
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

            var articolo = ToStringTrim(reader, 3);
            if (string.IsNullOrEmpty(articolo)) continue;

            righe.Add(new RigaOrdineVendita(
                AnnoOrdine:             ToStringTrim(reader, 0),
                NumeroOrdine:           ToStringTrim(reader, 1),
                RigaOrdine:             ToStringTrim(reader, 2),
                CodiceArticolo:         articolo,
                DescrizioneEstesa:      ToStringTrim(reader, 4),
                RagioneSocialeCliente:  ToStringTrim(reader, 5)));
        }

        return righe;
    }

    // Le colonne DB2/AS400 arrivano via ODBC con tipi eterogenei (decimal, char, ecc.) a seconda
    // della colonna: leggiamo tutto come stringa invece di assumere un tipo numerico, dato che
    // alcune (es. CHARACTER(10)) possono contenere causale e numero concatenati.
    private static string ToStringTrim(DbDataReader reader, int index) =>
        reader.IsDBNull(index) ? string.Empty : reader.GetValue(index).ToString()!.Trim();
}
