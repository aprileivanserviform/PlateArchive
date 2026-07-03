namespace PlateArchive.Services;

/// <summary>
/// Servizio di sincronizzazione dei clienti dal gestionale DB2 (via ODBC/VPN).
/// Viene eseguito in background all'avvio dell'applicazione (vedi App.xaml.cs).
/// Può anche essere eseguito manualmente dall'utente in ClientiViewModel.
/// </summary>
public interface ISincronizzazioneGestionaleService
{
    /// <summary>
    /// True se la stringa di connessione DB2 è configurata in appsettings.json.
    /// Se false, il pulsante "Sincronizza" rimane disabilitato.
    /// </summary>
    bool IsDisponibile { get; }

    Task<SincronizzazioneResult> SincronizzaClientiAsync(CancellationToken ct = default);
}

/// <summary>
/// Risultato della sincronizzazione: contatori e messaggio di errore se presente.
/// Record immutabile — viene creato una volta e passato a SyncStatusService.
/// <see cref="Annullati"/> = clienti locali non più presenti tra i validi del gestionale,
/// marcati AttivoGestionale = false (mai eliminati).
/// </summary>
public record SincronizzazioneResult(int Inseriti, int Aggiornati, int Invariati, string? Errore, int Annullati = 0)
{
    public bool   HasErrore  => Errore is not null;
    public string Riepilogo  => HasErrore
        ? $"Errore: {Errore}"
        : $"Sincronizzazione completata — {Inseriti} nuovi, {Aggiornati} aggiornati, {Invariati} invariati"
          + (Annullati > 0 ? $", {Annullati} annullati nel gestionale" : string.Empty);
}
