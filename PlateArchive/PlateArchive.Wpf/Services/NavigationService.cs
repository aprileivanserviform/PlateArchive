using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using PlateArchive.Wpf.ViewModels;

namespace PlateArchive.Wpf.Services;

/// <summary>
/// Servizio di navigazione tra schermate — singleton registrato in App.xaml.cs.
/// Gestisce una sola schermata alla volta: quando si naviga, la precedente viene distrutta.
/// <para>
/// Funzionamento:
/// <list type="number">
///   <item>Crea un nuovo DI scope (ogni schermata ha i propri repository e DbContext).</item>
///   <item>Risolve il ViewModel richiesto <typeparamref name="TViewModel"/> dallo scope.</item>
///   <item>Esegue l'azione di configurazione opzionale (es. imposta filtro, IdCliente, ecc.).</item>
///   <item>Aggiorna <see cref="CurrentViewModel"/>, che notifica la View via INotifyPropertyChanged.</item>
/// </list>
/// In App.xaml i DataTemplate legano ogni ViewModel a una View specifica:
/// <c>ContentControl.Content = CurrentViewModel → DataTemplate → View</c>.
/// </para>
/// </summary>
public class NavigationService(IServiceScopeFactory scopeFactory) : INotifyPropertyChanged
{
    // Scope DI corrente: dispose esplicito quando si naviga altrove (rilascia repository, DbContext).
    private IServiceScope? _currentScope;
    private ViewModelBase? _currentViewModel;

    /// <summary>
    /// Il ViewModel della schermata attiva.
    /// MainWindow.xaml lega un ContentControl a questa proprietà — il DataTemplate seleziona
    /// automaticamente la View corrispondente al tipo di ViewModel.
    /// </summary>
    public ViewModelBase? CurrentViewModel
    {
        get => _currentViewModel;
        private set { _currentViewModel = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// Naviga alla schermata <typeparamref name="TViewModel"/>.
    /// </summary>
    /// <typeparam name="TViewModel">Tipo del ViewModel da creare e mostrare.</typeparam>
    /// <param name="configure">
    /// Azione di inizializzazione eseguita sul ViewModel appena creato, prima di mostrarla.
    /// Usata per passare parametri: es. <c>vm => vm.IdCliente = 42</c>.
    /// </param>
    public async void Navigate<TViewModel>(Action<TViewModel>? configure = null) where TViewModel : ViewModelBase
    {
        // Distrugge lo scope precedente — rilascia DbContext, repository e ViewModel vecchio.
        _currentScope?.Dispose();
        _currentScope = scopeFactory.CreateScope();
        var vm = _currentScope.ServiceProvider.GetRequiredService<TViewModel>();
        configure?.Invoke(vm);

        // Mostra la view subito (il ViewModel può impostare IsCaricamento = true nel costruttore
        // per mostrare uno skeleton/spinner mentre OnNavigatedAsync carica i dati).
        CurrentViewModel = vm;

        try
        {
            // Esegue il caricamento dati. Se l'utente naviga altrove prima che termini,
            // _currentScope viene eliminato e la query DB può fallire: l'eccezione viene
            // gestita qui senza propagarsi al thread UI.
            await vm.OnNavigatedAsync();
        }
        catch { /* scope disposto da una navigazione successiva — comportamento atteso */ }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
