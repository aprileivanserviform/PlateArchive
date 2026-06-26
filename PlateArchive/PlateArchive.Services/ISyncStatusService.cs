using System.ComponentModel;

namespace PlateArchive.Services;

/// <summary>
/// Servizio singleton che espone lo stato della sincronizzazione clienti da DB2.
/// Implementa INotifyPropertyChanged così MainWindowViewModel può osservarne
/// i cambiamenti e aggiornarli nella status bar in fondo alla finestra.
/// Viene aggiornato da un Task in background (vedi App.xaml.cs → AvviaSyncInBackground).
/// </summary>
public interface ISyncStatusService : INotifyPropertyChanged
{
    /// <summary>True mentre la sincronizzazione è in corso (mostra l'icona ⟳).</summary>
    bool IsRunning { get; }

    /// <summary>Testo da mostrare nella status bar (null = status bar nascosta).</summary>
    string? StatusText { get; }

    /// <summary>True se l'ultima sincronizzazione è terminata con errore (testo rosso).</summary>
    bool HasError { get; }

    void SetRunning();
    void SetCompleted(SincronizzazioneResult result);
}
