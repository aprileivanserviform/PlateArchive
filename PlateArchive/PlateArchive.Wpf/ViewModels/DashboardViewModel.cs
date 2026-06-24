using System.Collections.ObjectModel;
using System.Windows.Input;
using PlateArchive.Core.Enums;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Wpf.Commands;
using PlateArchive.Wpf.Services;

namespace PlateArchive.Wpf.ViewModels;

public class DashboardViewModel : ViewModelBase
{
    private readonly IPiastraRepository _piastre;
    private readonly IDisegnoRepository _disegni;
    private readonly NavigationService _navigation;

    private string _ricercaCliente = string.Empty;
    private string _ricercaPiastra = string.Empty;
    private string _ricercaMacchina = string.Empty;

    public DashboardViewModel(IPiastraRepository piastre, IDisegnoRepository disegni, NavigationService navigation)
    {
        _piastre    = piastre;
        _disegni    = disegni;
        _navigation = navigation;

        RicercaClienteCommand = new RelayCommand(_ =>
            _navigation.Navigate<ClientiViewModel>(vm => vm.FiltroRicerca = RicercaCliente));

        RicercaPiastraCommand = new RelayCommand(_ =>
            _navigation.Navigate<PiastreViewModel>(vm => vm.FiltroRicerca = RicercaPiastra));

        RicercaMacchinaCommand = new RelayCommand(_ =>
            _navigation.Navigate<MacchineViewModel>(vm => vm.FiltroRicerca = RicercaMacchina));

        NuovaPiastraCommand  = new RelayCommand(_ => _navigation.Navigate<PiastreViewModel>());
        NuovaMacchinaCommand = new RelayCommand(_ => _navigation.Navigate<MacchineViewModel>());

        // Sincronizzazione DB2 disabilitata fino a TASK-13
        SincronizzaClientiCommand = new RelayCommand(_ => { }, _ => false);

        _ = LoadAsync();
    }

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

    public ObservableCollection<Piastra> UltimePiastre       { get; } = [];
    public ObservableCollection<Disegno> DisegniDaVerificare { get; } = [];

    public ICommand RicercaClienteCommand     { get; }
    public ICommand RicercaPiastraCommand     { get; }
    public ICommand RicercaMacchinaCommand    { get; }
    public ICommand NuovaPiastraCommand       { get; }
    public ICommand NuovaMacchinaCommand      { get; }
    public ICommand SincronizzaClientiCommand { get; }

    private async Task LoadAsync()
    {
        var ultimePiastre = await _piastre.GetUltimeInseriteAsync(10);
        foreach (var p in ultimePiastre)
            UltimePiastre.Add(p);

        var daVerificare = await _disegni.GetByStatoAsync(StatoDisegno.DaVerificare);
        foreach (var d in daVerificare.Take(10))
            DisegniDaVerificare.Add(d);
    }
}
