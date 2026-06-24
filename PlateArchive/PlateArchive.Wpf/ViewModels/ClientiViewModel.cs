using System.Collections.ObjectModel;
using System.Windows.Input;
using PlateArchive.Core.Enums;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Wpf.Commands;
using PlateArchive.Wpf.Services;

namespace PlateArchive.Wpf.ViewModels;

public class ClientiViewModel : ViewModelBase
{
    private readonly IClienteRepository _repo;
    private readonly NavigationService _navigation;
    private readonly ObservableCollection<Cliente> _tutti = [];
    private string _filtroRicerca        = string.Empty;
    private string _filtroStatoSelezionato = "Tutti";

    public ClientiViewModel(IClienteRepository repo, NavigationService navigation)
    {
        _repo       = repo;
        _navigation = navigation;

        AprirDettaglioCommand = new RelayCommand(
            p => _navigation.Navigate<ClienteDettaglioViewModel>(
                     vm => vm.IdCliente = ((Cliente)p!).IdCliente),
            p => p is Cliente);

        _ = LoadAsync();
    }

    public string FiltroRicerca
    {
        get => _filtroRicerca;
        set { if (SetField(ref _filtroRicerca, value)) AggiornaFiltro(); }
    }

    public string FiltroStatoSelezionato
    {
        get => _filtroStatoSelezionato;
        set { if (SetField(ref _filtroStatoSelezionato, value)) AggiornaFiltro(); }
    }

    public IEnumerable<string> StatiFiltro { get; } = ["Tutti", "Attivo", "Disattivato", "Storico"];

    public ObservableCollection<Cliente> ClientiFiltrati { get; } = [];

    public ICommand AprirDettaglioCommand { get; }

    private async Task LoadAsync()
    {
        var clienti = await _repo.GetAllAsync();
        foreach (var c in clienti) _tutti.Add(c);
        AggiornaFiltro();
    }

    private void AggiornaFiltro()
    {
        StatoCliente? statoFiltro = FiltroStatoSelezionato switch
        {
            "Attivo"      => StatoCliente.Attivo,
            "Disattivato" => StatoCliente.Disattivato,
            "Storico"     => StatoCliente.Storico,
            _             => null
        };

        ClientiFiltrati.Clear();
        var f = FiltroRicerca.Trim().ToLower();
        foreach (var c in _tutti.Where(c =>
            (statoFiltro is null || c.StatoCliente == statoFiltro)
            && (string.IsNullOrEmpty(f)
                || c.CodiceClienteGestionale.ToLower().Contains(f)
                || c.RagioneSociale.ToLower().Contains(f)
                || (c.PartitaIVA?.ToLower().Contains(f) ?? false))))
        {
            ClientiFiltrati.Add(c);
        }
    }
}
