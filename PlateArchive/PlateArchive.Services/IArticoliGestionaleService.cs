namespace PlateArchive.Services;

/// <summary>
/// Lettura dell'anagrafica articoli piastre dal gestionale (DB2/Panthera).
/// A differenza delle righe ordine, l'anagrafica cambia raramente: la lista viene letta
/// una volta e tenuta in cache per la sessione dell'app (il servizio è un singleton).
/// </summary>
public interface IArticoliGestionaleService
{
    /// <summary>True se la stringa di connessione DB2 è configurata.</summary>
    bool IsDisponibile { get; }

    /// <summary>
    /// Restituisce gli articoli piastre dal gestionale (query configurata in
    /// appsettings.json, Db2:QueryArticoli — 1ª colonna codice, 2ª descrizione).
    /// La prima chiamata interroga DB2, le successive servono la cache;
    /// <paramref name="forzaRicarica"/> ripete l'interrogazione.
    /// </summary>
    Task<IReadOnlyList<ArticoloGestionale>> GetArticoliAsync(bool forzaRicarica = false, CancellationToken ct = default);
}
