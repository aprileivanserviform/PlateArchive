using System.Data.Common;
using System.Data.Odbc;
using PlateArchive.Core.Servizi;

namespace PlateArchive.Services;

/// <summary>
/// Implementazione della lettura anagrafica articoli dal gestionale DB2 (Panthera), stessa
/// connessione ODBC/VPN di clienti e righe ordine. La query (appsettings.json,
/// Db2:QueryArticoli) legge THIP.ARTICOLI: 1ª colonna = codice, 2ª = descrizione (posizionali,
/// così la SELECT può usare alias liberamente). Cache in memoria per la sessione: l'anagrafica
/// cambia raramente, il servizio è registrato singleton.
/// <para>
/// Sono tenuti solo gli articoli della NUOVA codifica piastre (primi 16 caratteri numerici, vedi
/// <see cref="CodiceArticoloPanthera.IsNuovaCodifica"/>): la query da sola non basta a distinguere
/// le piastre da software/moduli/licenze che pure iniziano per "30", quindi si filtra qui.
/// </para>
/// </summary>
public class ArticoliGestionaleService(string connectionString, string queryArticoli)
    : IArticoliGestionaleService
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IReadOnlyList<ArticoloGestionale>? _cache;

    public bool IsDisponibile => !string.IsNullOrWhiteSpace(connectionString);

    public async Task<IReadOnlyList<ArticoloGestionale>> GetArticoliAsync(
        bool forzaRicarica = false, CancellationToken ct = default)
    {
        if (!IsDisponibile)
            throw new InvalidOperationException("Stringa di connessione DB2 non configurata.");

        if (!forzaRicarica && _cache is not null) return _cache;

        await _lock.WaitAsync(ct);
        try
        {
            if (forzaRicarica || _cache is null)
                _cache = await LeggiDaDb2Async(ct);
            return _cache;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IReadOnlyList<ArticoloGestionale>> LeggiDaDb2Async(CancellationToken ct)
    {
        var articoli = new List<ArticoloGestionale>();

        using var conn = new OdbcConnection(connectionString);
        await conn.OpenAsync(ct);

        using var cmd    = new OdbcCommand(queryArticoli, conn);
        using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            ct.ThrowIfCancellationRequested();

            var codice = ToStringTrim(reader, 0);
            if (string.IsNullOrEmpty(codice)) continue;

            // Solo la nuova codifica piastre (16 cifre + suffisso): esclude software, moduli e
            // licenze che iniziano anch'essi per "30" ma non hanno i primi 16 caratteri numerici.
            if (!CodiceArticoloPanthera.IsNuovaCodifica(codice)) continue;

            var descrizione = reader.FieldCount > 1 ? ToStringTrim(reader, 1) : string.Empty;
            articoli.Add(new ArticoloGestionale(codice, descrizione));
        }

        return articoli;
    }

    // Stessa normalizzazione di RigheOrdineVenditaService: tutto come stringa (tipi DB2
    // eterogenei) e a-capo compattati in uno spazio (DESCR_ESTESA può essere multiriga).
    private static string ToStringTrim(DbDataReader reader, int index) =>
        reader.IsDBNull(index)
            ? string.Empty
            : System.Text.RegularExpressions.Regex.Replace(
                  reader.GetValue(index).ToString()!.Trim(), @"\s*[\r\n]+\s*", " ");
}
