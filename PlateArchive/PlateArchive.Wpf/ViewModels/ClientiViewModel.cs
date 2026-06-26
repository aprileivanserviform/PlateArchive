using System.Collections.ObjectModel;
using System.Windows.Input;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Services;
using PlateArchive.Wpf.Commands;
using PlateArchive.Wpf.Services;

namespace PlateArchive.Wpf.ViewModels;

/// <summary>
/// ViewModel della schermata Clienti (lista anagrafica).
/// Funzioni principali:
/// - Mostra tutti i clienti con filtro di ricerca real-time
/// - Apre il dettaglio cliente tramite ClienteDettaglioViewModel
/// - Permette la sincronizzazione manuale con il gestionale DB2
/// <para>
/// La lista completa viene tenuta in <c>_tutti</c> (non esposta alla View)
/// e filtrata in <see cref="ClientiFiltrati"/> — evita query ripetute al database.
/// </para>
/// </summary>
public class ClientiViewModel : ViewModelBase
{
    private readonly IClienteRepository                  _repo;
    private readonly NavigationService                   _navigation;
    private readonly ISincronizzazioneGestionaleService  _syncService;

    // Lista completa in memoria — il filtro viene applicato su questa, non sul DB.
    private readonly ObservableCollection<Cliente> _tutti = [];

    private string  _filtroRicerca      = string.Empty;
    private bool    _isSincronizzando;
    private string? _esitoSincronizzazione;
    private bool    _esitoIsErrore;

    public ClientiViewModel(
        IClienteRepository                 repo,
        NavigationService                  navigation,
        ISincronizzazioneGestionaleService syncService)
    {
        _repo        = repo;
        _navigation  = navigation;
        _syncService = syncService;

        // Il parametro del comando è il Cliente cliccato nella lista.
        // NavigationService crea un nuovo ClienteDettaglioViewModel e imposta l'IdCliente.
        AprirDettaglioCommand = new RelayCommand(
            p => _navigation.Navigate<ClienteDettaglioViewModel>(
                     vm => vm.IdCliente = ((Cliente)p!).IdCliente),
            p => p is Cliente);

        SincronizzaCommand = new RelayCommand(
            async _ => await SincronizzaAsync(),
            _        => !_isSincronizzando && _syncService.IsDisponibile);

        _ = LoadAsync();
    }

    // ─── Filtro ricerca ───────────────────────────────────────────────────────

    public string FiltroRicerca
    {
        get => _filtroRicerca;
        set { if (SetField(ref _filtroRicerca, value)) AggiornaFiltro(); }
    }

    /// <summary>Subset di _tutti filtrato per la ricerca — bound alla DataGrid nella View.</summary>
    public ObservableCollection<Cliente> ClientiFiltrati { get; } = [];

    // ─── Stato sincronizzazione ───────────────────────────────────────────────

    public bool IsSincronizzando
    {
        get => _isSincronizzando;
        private set { if (SetField(ref _isSincronizzando, value)) OnPropertyChanged(nameof(SincronizzaLabel)); }
    }

    /// <summary>Etichetta del pulsante: cambia mentre la sync è in corso.</summary>
    public string SincronizzaLabel => _isSincronizzando ? "Sincronizzazione..." : "Sincronizza";

    public string? EsitoSincronizzazione
    {
        get => _esitoSincronizzazione;
        private set
        {
            if (SetField(ref _esitoSincronizzazione, value))
                OnPropertyChanged(nameof(IsEsitoVisible));
        }
    }

    public bool EsitoIsErrore
    {
        get => _esitoIsErrore;
        private set { if (SetField(ref _esitoIsErrore, value)) OnPropertyChanged(nameof(EsitoIsOk)); }
    }

    public bool EsitoIsOk       => !_esitoIsErrore;
    public bool IsEsitoVisible  => !string.IsNullOrEmpty(_esitoSincronizzazione);

    // ─── Comandi ─────────────────────────────────────────────────────────────

    public ICommand AprirDettaglioCommand { get; }
    public ICommand SincronizzaCommand    { get; }

    // ─── Caricamento ─────────────────────────────────────────────────────────

    // internal (non private) perché DashboardViewModel può richiamare il reload dopo la sync.
    internal async Task LoadAsync()
    {
        _tutti.Clear();
        var clienti = await _repo.GetAllAsync();
        foreach (var c in clienti) _tutti.Add(c);
        AggiornaFiltro();
    }

    // ─── Sincronizzazione DB2 ─────────────────────────────────────────────────

    private async Task SincronizzaAsync()
    {
        IsSincronizzando      = true;
        EsitoSincronizzazione = null;

        var result = await _syncService.SincronizzaClientiAsync();

        EsitoIsErrore         = result.HasErrore;
        EsitoSincronizzazione = result.Riepilogo;
        IsSincronizzando      = false;

        // Ricarica la lista solo se la sync è andata a buon fine.
        if (!result.HasErrore)
            await LoadAsync();
    }

    // ─── Filtro in memoria ────────────────────────────────────────────────────

    private void AggiornaFiltro()
    {
        ClientiFiltrati.Clear();
        var f = _filtroRicerca.Trim().ToLower();
        foreach (var c in _tutti.Where(c =>
            string.IsNullOrEmpty(f)
            || c.CodiceClienteGestionale.ToLower().Contains(f)
            || c.RagioneSociale.ToLower().Contains(f)))
        {
            ClientiFiltrati.Add(c);
        }
    }
}
