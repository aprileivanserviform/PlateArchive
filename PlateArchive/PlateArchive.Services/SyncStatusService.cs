using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PlateArchive.Services;

/// <summary>
/// Implementazione di <see cref="ISyncStatusService"/>.
/// Gestisce il marshalling sul thread UI: le notifiche PropertyChanged
/// devono arrivare sul thread UI anche se la sync gira su un thread di background.
/// </summary>
public class SyncStatusService : ISyncStatusService
{
    // SynchronizationContext catturato al momento della costruzione (durante OnStartup = thread UI).
    // Usato per rimandare le notifiche PropertyChanged sul thread corretto,
    // altrimenti WPF lancerebbe InvalidOperationException dal thread di background.
    private readonly SynchronizationContext? _uiContext = SynchronizationContext.Current;

    private bool    _isRunning;
    private string? _statusText;
    private bool    _hasError;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsRunning
    {
        get => _isRunning;
        private set => Set(ref _isRunning, value);
    }

    public string? StatusText
    {
        get => _statusText;
        private set => Set(ref _statusText, value);
    }

    public bool HasError
    {
        get => _hasError;
        private set => Set(ref _hasError, value);
    }

    public void SetRunning()
    {
        IsRunning  = true;
        HasError   = false;
        StatusText = "Sincronizzazione clienti in corso...";
    }

    public void SetCompleted(SincronizzazioneResult result)
    {
        IsRunning  = false;
        HasError   = result.HasErrore;
        StatusText = result.Riepilogo;
    }

    // Notifica PropertyChanged sul thread UI via SynchronizationContext.Post (fire-and-forget).
    // Se non c'è contesto UI (es. unit test), notifica direttamente.
    private void Set<T>(ref T field, T value, [CallerMemberName] string? prop = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        var args = new PropertyChangedEventArgs(prop);
        if (_uiContext != null)
            _uiContext.Post(_ => PropertyChanged?.Invoke(this, args), null);
        else
            PropertyChanged?.Invoke(this, args);
    }
}
