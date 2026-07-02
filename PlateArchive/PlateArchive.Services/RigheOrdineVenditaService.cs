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

    // Colonne che alimentano la logica applicativa (ricerca piastra, auto-associazione):
    // vengono individuate per NOME nel risultato, quindi la SELECT può cambiare liberamente
    // ordine e colonne purché queste due restino presenti senza alias.
    private const string ColonnaArticolo = "R_ARTICOLO";
    private const string ColonnaCliente  = "R_CLIENTE";

    public async Task<RigheOrdineVenditaResult> LeggiRigheInevaseAsync(CancellationToken ct = default)
    {
        if (!IsDisponibile)
            throw new InvalidOperationException("Stringa di connessione DB2 non configurata.");

        var righe = new List<RigaOrdineVendita>();

        using var conn = new OdbcConnection(connectionString);
        await conn.OpenAsync(ct);

        using var cmd    = new OdbcCommand(queryRigheOrdine, conn);
        using var reader = await cmd.ExecuteReaderAsync(ct);

        // Le colonne (nome + posizione) sono definite dalla SELECT: la griglia le mostra così come sono.
        var colonne = new string[reader.FieldCount];
        for (int i = 0; i < reader.FieldCount; i++)
            colonne[i] = reader.GetName(i).Trim();

        var idxArticolo = Array.FindIndex(colonne, c => c.Equals(ColonnaArticolo, StringComparison.OrdinalIgnoreCase));
        var idxCliente  = Array.FindIndex(colonne, c => c.Equals(ColonnaCliente,  StringComparison.OrdinalIgnoreCase));

        while (await reader.ReadAsync(ct))
        {
            ct.ThrowIfCancellationRequested();

            var valori = new string[reader.FieldCount];
            for (int i = 0; i < reader.FieldCount; i++)
                valori[i] = ToStringTrim(reader, i);

            var articolo = idxArticolo >= 0 ? valori[idxArticolo] : string.Empty;
            if (idxArticolo >= 0 && string.IsNullOrEmpty(articolo)) continue;

            righe.Add(new RigaOrdineVendita(
                CodiceArticolo:          articolo,
                CodiceClienteGestionale: idxCliente >= 0 ? valori[idxCliente] : string.Empty,
                Valori:                  valori));
        }

        return new RigheOrdineVenditaResult(colonne, righe);
    }

    // Le colonne DB2/AS400 arrivano via ODBC con tipi eterogenei (decimal, char, ecc.) a seconda
    // della colonna: leggiamo tutto come stringa invece di assumere un tipo numerico, dato che
    // alcune (es. CHARACTER(10)) possono contenere causale e numero concatenati.
    private static string ToStringTrim(DbDataReader reader, int index) =>
        reader.IsDBNull(index) ? string.Empty : reader.GetValue(index).ToString()!.Trim();
}
