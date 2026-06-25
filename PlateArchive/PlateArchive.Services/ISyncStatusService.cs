using System.ComponentModel;

namespace PlateArchive.Services;

public interface ISyncStatusService : INotifyPropertyChanged
{
    bool IsRunning { get; }
    string? StatusText { get; }
    bool HasError { get; }

    void SetRunning();
    void SetCompleted(SincronizzazioneResult result);
}
