using System.Collections.ObjectModel;
using System.Windows.Input;
using PlateArchive.Core.Enums;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Wpf.Commands;
using PlateArchive.Wpf.Services;

namespace PlateArchive.Wpf.ViewModels;

/// <summary>
/// ViewModel della schermata Dashboard (pagina iniziale).
/// Mostra:
/// - Ultime 10 piastre inserite (accesso rapido)
/// - Disegni in stato "Da verificare" (to-do tecnico)
/// - Caselle di ricerca rapida che navigano direttamente alla schermata corrispondente
///   con il filtro già impostato.
/// </summary>
public class DashboardViewModel : ViewModelBase
{
    private readonly IPiastraRepository _piastre;
    private readonly IDisegnoRepository _disegni;
    private readonly NavigationService  _navigation;

    private string _ricercaCliente  = string.Empty;
    private string _ricercaPiastra  = string.Empty;
    private string _ricercaMacchina = string.Empty;

    public DashboardViewModel(IPiastraRepository piastre, IDisegnoRepository disegni, NavigationService navigation)
    {
        _piastre    = piastre;
        _disegni    = disegni;
        _navigation = navigation;

        // I comandi di ricerca navigano alla schermata e pre-impostano il filtro testuale.
        // vm => vm.FiltroRicerca = ... è un'azione di inizializzazione eseguita da NavigationService
        // prima di esporre il ViewModel alla View.
        RicercaClienteCommand = new RelayCommand(_ =>
            _navigation.Navigate<ClientiViewModel>(vm => vm.FiltroRicerca = RicercaCliente));

        RicercaPiastraCommand = new RelayCommand(_ =>
            _navigation.Navigate<PiastreViewModel>(vm => vm.FiltroRicerca = RicercaPiastra));

        RicercaMacchinaCommand = new RelayCommand(_ =>
            _navigation.Navigate<MacchineViewModel>(vm => vm.FiltroRicerca = RicercaMacchina));

        NuovaPiastraCommand  = new RelayCommand(_ => _navigation.Navigate<PiastreViewModel>());
        NuovaMacchinaCommand = new RelayCommand(_ => _navigation.Navigate<MacchineViewModel>());

        // Pulsante Sincronizza disabilitato fino all'implementazione del TASK-13
        SincronizzaClientiCommand = new RelayCommand(_ => { }, _ => false);

        _ = LoadAsync();
    }

    // ─── Filtri di ricerca rapida ─────────────────────────────────────────────

    public string RicercaCliente
    {
        get => _ricercaCliente;
        set => SetField(ref _ricercaCliente, value);
    }

    public string RicercaPiastra
    {
        get => _ricercaPiastra;
        set => SetField(ref _ricercaPiastra, value);
    }

    public string RicercaMacchina
    {
        get => _ricercaMacchina;
        set => SetField(ref _ricercaMacchina, value);
    }

    // ─── Dati Dashboard ───────────────────────────────────────────────────────

    /// <summary>Le ultime 10 piastre inserite, mostrate in una griglia sulla dashboard.</summary>
    public ObservableCollection<Piastra> UltimePiastre       { get; } = [];

    /// <summary>Disegni tecnici con Stato = DaVerificare — richiedono attenzione dell'utente.</summary>
    public ObservableCollection<Disegno> DisegniDaVerificare { get; } = [];

    // ─── Comandi ─────────────────────────────────────────────────────────────

    public ICommand RicercaClienteCommand     { get; }
    public ICommand RicercaPiastraCommand     { get; }
    public ICommand RicercaMacchinaCommand    { get; }
    public ICommand NuovaPiastraCommand       { get; }
    public ICommand NuovaMacchinaCommand      { get; }
    public ICommand SincronizzaClientiCommand { get; }

    // ─── Caricamento ─────────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        var ultimePiastre = await _piastre.GetUltimeInseriteAsync(10);
        foreach (var p in ultimePiastre)
            UltimePiastre.Add(p);

        // Mostra solo i primi 10 disegni "Da verificare" per non sovraccaricare la dashboard.
        var daVerificare = await _disegni.GetByStatoAsync(StatoDisegno.DaVerificare);
        foreach (var d in daVerificare.Take(10))
            DisegniDaVerificare.Add(d);
    }
}
