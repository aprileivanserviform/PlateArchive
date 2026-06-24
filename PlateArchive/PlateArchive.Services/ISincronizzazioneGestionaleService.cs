namespace PlateArchive.Services;

public interface ISincronizzazioneGestionaleService
{
    bool IsDisponibile { get; }
    Task<SincronizzazioneResult> SincronizzaClientiAsync(CancellationToken ct = default);
}

public record SincronizzazioneResult(int Inseriti, int Aggiornati, int Invariati, string? Errore)
{
    public bool HasErrore => Errore is not null;
    public string Riepilogo => HasErrore
        ? $"Errore: {Errore}"
        : $"Sincronizzazione completata — {Inseriti} nuovi, {Aggiornati} aggiornati, {Invariati} invariati";
}
