namespace PlateArchive.Services;

/// <summary>
/// Lettura live (nessuna cache locale) delle righe ordine di vendita non evase dal gestionale.
/// A differenza dei Clienti, lo stato "evasa/inevasa" cambia di continuo: ha senso solo
/// un'interrogazione diretta al momento dell'apertura della vista, non una sincronizzazione.
/// </summary>
public interface IRigheOrdineVenditaService
{
    /// <summary>True se la stringa di connessione DB2 è configurata.</summary>
    bool IsDisponibile { get; }

    /// <summary>Legge tutte le righe ordine non evase dal gestionale.</summary>
    Task<IReadOnlyList<RigaOrdineVendita>> LeggiRigheInevaseAsync(CancellationToken ct = default);
}
