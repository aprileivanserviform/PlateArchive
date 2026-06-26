using System.Windows.Input;
using PlateArchive.Services;
using PlateArchive.Wpf.Commands;
using PlateArchive.Wpf.Services;

namespace PlateArchive.Wpf.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly NavigationService  _navigation;
    private readonly ISyncStatusService _syncStatus;

    private bool _isImpostazioniExpanded;

    public MainWindowViewModel(NavigationService navigation, ISyncStatusService syncStatus)
    {
        _navigation = navigation;
        _syncStatus = syncStatus;

        navigation.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(NavigationService.CurrentViewModel))
                OnPropertyChanged(nameof(CurrentViewModel));
        };

        syncStatus.PropertyChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(SyncStatusText));
            OnPropertyChanged(nameof(IsSyncing));
            OnPropertyChanged(nameof(SyncHasError));
            OnPropertyChanged(nameof(SyncStatusVisible));
        };

        NavigateToDashboardCommand        = new RelayCommand(_ => _navigation.Navigate<DashboardViewModel>());
        NavigateToClientiCommand          = new RelayCommand(_ => _navigation.Navigate<ClientiViewModel>());
        NavigateToPiastreCommand          = new RelayCommand(_ => _navigation.Navigate<PiastreViewModel>());
        NavigateToMacchineCommand         = new RelayCommand(_ => _navigation.Navigate<MacchineViewModel>());
        NavigateToDisegniCommand          = new RelayCommand(_ => _navigation.Navigate<DisegniViewModel>());
        NavigateToFormatiMacchinaCommand  = new RelayCommand(_ => _navigation.Navigate<FormatiMacchinaViewModel>());
        ToggleImpostazioniCommand         = new RelayCommand(_ => IsImpostazioniExpanded = !IsImpostazioniExpanded);
    }

    public ViewModelBase? CurrentViewModel => _navigation.CurrentViewModel;

    // ── Stato sincronizzazione (visibile nella status bar) ──────────────
    public string? SyncStatusText  => _syncStatus.StatusText;
    public bool    IsSyncing       => _syncStatus.IsRunning;
    public bool    SyncHasError    => _syncStatus.HasError;
    public bool    SyncStatusVisible => _syncStatus.StatusText is not null;

    // ── Sezione Impostazioni (espandibile) ───────────────────────────────
    public bool IsImpostazioniExpanded
    {
        get => _isImpostazioniExpanded;
        set
        {
            if (SetField(ref _isImpostazioniExpanded, value))
                OnPropertyChanged(nameof(ImpostazioniArrow));
        }
    }

    public string ImpostazioniArrow => IsImpostazioniExpanded ? "▾" : "▸";

    // ── Navigazione ─────────────────────────────────────────────────────
    public ICommand NavigateToDashboardCommand       { get; }
    public ICommand NavigateToClientiCommand         { get; }
    public ICommand NavigateToPiastreCommand         { get; }
    public ICommand NavigateToMacchineCommand        { get; }
    public ICommand NavigateToDisegniCommand         { get; }
    public ICommand NavigateToFormatiMacchinaCommand { get; }
    public ICommand ToggleImpostazioniCommand        { get; }
}
