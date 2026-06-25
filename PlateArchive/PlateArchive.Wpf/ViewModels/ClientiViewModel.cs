using System.Collections.ObjectModel;
using System.Windows.Input;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Services;
using PlateArchive.Wpf.Commands;
using PlateArchive.Wpf.Services;

namespace PlateArchive.Wpf.ViewModels;

public class ClientiViewModel : ViewModelBase
{
    private readonly IClienteRepository _repo;
    private readonly NavigationService  _navigation;
    private readonly ISincronizzazioneGestionaleService _syncService;
    private readonly ObservableCollection<Cliente> _tutti = [];
    private string _filtroRicerca  = string.Empty;
    private bool   _isSincronizzando;
    private string? _esitoSincronizzazione;
    private bool   _esitoIsErrore;

    public ClientiViewModel(
        IClienteRepository repo,
        NavigationService navigation,
        ISincronizzazioneGestionaleService syncService)
    {
        _repo        = repo;
        _navigation  = navigation;
        _syncService = syncService;

        AprirDettaglioCommand = new RelayCommand(
            p => _navigation.Navigate<ClienteDettaglioViewModel>(
                     vm => vm.IdCliente = ((Cliente)p!).IdCliente),
            p => p is Cliente);

        SincronizzaCommand = new RelayCommand(
            async _ => await SincronizzaAsync(),
            _        => !_isSincronizzando && _syncService.IsDisponibile);

        _ = LoadAsync();
    }

    public string FiltroRicerca
    {
        get => _filtroRicerca;
        set { if (SetField(ref _filtroRicerca, value)) AggiornaFiltro(); }
    }

    public ObservableCollection<Cliente> ClientiFiltrati { get; } = [];

    public bool IsSincronizzando
    {
        get => _isSincronizzando;
        private set { if (SetField(ref _isSincronizzando, value)) OnPropertyChanged(nameof(SincronizzaLabel)); }
    }

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

    public bool EsitoIsOk      => !_esitoIsErrore;
    public bool IsEsitoVisible => !string.IsNullOrEmpty(_esitoSincronizzazione);

    public ICommand AprirDettaglioCommand { get; }
    public ICommand SincronizzaCommand    { get; }

    internal async Task LoadAsync()
    {
        _tutti.Clear();
        var clienti = await _repo.GetAllAsync();
        foreach (var c in clienti) _tutti.Add(c);
        AggiornaFiltro();
    }

    private async Task SincronizzaAsync()
    {
        IsSincronizzando      = true;
        EsitoSincronizzazione = null;

        var result = await _syncService.SincronizzaClientiAsync();

        EsitoIsErrore         = result.HasErrore;
        EsitoSincronizzazione = result.Riepilogo;
        IsSincronizzando      = false;

        if (!result.HasErrore)
            await LoadAsync();
    }

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
