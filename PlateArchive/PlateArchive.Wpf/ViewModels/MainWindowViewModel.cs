using System.Windows.Input;
using PlateArchive.Services;
using PlateArchive.Wpf.Commands;
using PlateArchive.Wpf.Services;

namespace PlateArchive.Wpf.ViewModels;

/// <summary>
/// ViewModel della finestra principale (MainWindow).
/// Responsabilità:
/// 1. Gestisce la navigazione tra le schermate tramite <see cref="NavigationService"/>.
/// 2. Osserva <see cref="ISyncStatusService"/> e aggiorna la status bar in fondo alla finestra.
/// 3. Controlla l'espansione della sezione "Impostazioni" nella sidebar.
/// <para>
/// È registrato come Singleton perché MainWindow vive per tutta la sessione.
/// </para>
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private readonly NavigationService  _navigation;
    private readonly ISyncStatusService _syncStatus;

    private bool _isImpostazioniExpanded;

    public MainWindowViewModel(NavigationService navigation, ISyncStatusService syncStatus)
    {
        _navigation = navigation;
        _syncStatus = syncStatus;

        // Propaga i cambiamenti di CurrentViewModel alla View tramite PropertyChanged.
        navigation.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(NavigationService.CurrentViewModel))
                OnPropertyChanged(nameof(CurrentViewModel));
        };

        // Quando lo stato della sync cambia (da un thread background),
        // aggiorna le proprietà della status bar.
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
        NavigateToFormatiMacchinaCommand    = new RelayCommand(_ => _navigation.Navigate<FormatiMacchinaViewModel>());
        NavigateToCategoriePiastreCommand   = new RelayCommand(_ => _navigation.Navigate<CategoriePiastreViewModel>());
        NavigateToProduttoriMacchinaCommand = new RelayCommand(_ => _navigation.Navigate<ProduttoriMacchinaViewModel>());
        ToggleImpostazioniCommand           = new RelayCommand(_ => IsImpostazioniExpanded = !IsImpostazioniExpanded);
    }

    // ─── Navigazione ─────────────────────────────────────────────────────────

    /// <summary>
    /// ViewModel attualmente visualizzato nel ContentControl centrale.
    /// WPF lo abbina alla View corretta tramite i DataTemplate definiti in App.xaml.
    /// </summary>
    public ViewModelBase? CurrentViewModel => _navigation.CurrentViewModel;

    // ─── Status bar sincronizzazione ─────────────────────────────────────────

    /// <summary>Testo da mostrare nella status bar (null = status bar collassata).</summary>
    public string? SyncStatusText   => _syncStatus.StatusText;
    public bool    IsSyncing        => _syncStatus.IsRunning;
    public bool    SyncHasError     => _syncStatus.HasError;
    public bool    SyncStatusVisible => _syncStatus.StatusText is not null;

    // ─── Sezione Impostazioni (sidebar espandibile) ───────────────────────────

    /// <summary>Controlla la visibilità del sottomenu Impostazioni nella sidebar.</summary>
    public bool IsImpostazioniExpanded
    {
        get => _isImpostazioniExpanded;
        set
        {
            if (SetField(ref _isImpostazioniExpanded, value))
                OnPropertyChanged(nameof(ImpostazioniArrow));
        }
    }

    /// <summary>Icona freccia mostrata accanto a "Impostazioni": ▸ chiuso, ▾ aperto.</summary>
    public string ImpostazioniArrow => IsImpostazioniExpanded ? "▾" : "▸";

    // ─── Comandi navigazione ──────────────────────────────────────────────────

    public ICommand NavigateToDashboardCommand       { get; }
    public ICommand NavigateToClientiCommand         { get; }
    public ICommand NavigateToPiastreCommand         { get; }
    public ICommand NavigateToMacchineCommand        { get; }
    public ICommand NavigateToFormatiMacchinaCommand    { get; }
    public ICommand NavigateToCategoriePiastreCommand   { get; }
    public ICommand NavigateToProduttoriMacchinaCommand { get; }
    public ICommand ToggleImpostazioniCommand           { get; }
}
