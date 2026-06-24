using System.Collections.ObjectModel;
using System.Windows.Input;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Wpf.Commands;
using PlateArchive.Wpf.Services;

namespace PlateArchive.Wpf.ViewModels;

public record CompatibilitaRow(string NomeMacchina, string CodicePiastra, string? DescrizionePiastra, bool Attiva);

public class ClienteDettaglioViewModel : ViewModelBase
{
    private readonly IClienteRepository          _clienteRepo;
    private readonly IClienteMacchinaRepository  _macchineRepo;
    private readonly IClientePiastraRepository   _piastreRepo;
    private readonly ICompatibilitaRepository    _compatRepo;
    private readonly IMacchinaStandardRepository _macchineStdRepo;
    private readonly NavigationService           _navigation;

    private int _idCliente;
    private Cliente? _cliente;
    private bool _isAggiungiMacchinaVisible;
    private MacchinaStandard? _macchinaSelezionata;
    private string _matricolaNuova = string.Empty;
    private string _noteNuovaMacchina = string.Empty;

    public ClienteDettaglioViewModel(
        IClienteRepository          clienteRepo,
        IClienteMacchinaRepository  macchineRepo,
        IClientePiastraRepository   piastreRepo,
        ICompatibilitaRepository    compatRepo,
        IMacchinaStandardRepository macchineStdRepo,
        NavigationService           navigation)
    {
        _clienteRepo     = clienteRepo;
        _macchineRepo    = macchineRepo;
        _piastreRepo     = piastreRepo;
        _compatRepo      = compatRepo;
        _macchineStdRepo = macchineStdRepo;
        _navigation      = navigation;

        TornaIndietroCommand = new RelayCommand(_ => _navigation.Navigate<ClientiViewModel>());

        AggiungiMacchinaCommand = new RelayCommand(_ => IsAggiungiMacchinaVisible = true);

        ConfermaAggiungiMacchinaCommand = new RelayCommand(
            async _ => await ConfermaAggiungiMacchinaAsync(),
            _ => MacchinaSelezionata is not null);

        AnnullaAggiungiMacchinaCommand = new RelayCommand(_ =>
        {
            IsAggiungiMacchinaVisible = false;
            MacchinaSelezionata = null;
            MatricolaNuova = string.Empty;
            NoteNuovaMacchina = string.Empty;
        });

        RimuoviMacchinaCommand = new RelayCommand(
            async p => await RimuoviMacchinaAsync((ClienteMacchina)p!));

        RimuoviPiastraCommand = new RelayCommand(
            async p => await RimuoviPiastraAsync((ClientePiastra)p!));

        AprirDisegnoCommand = new RelayCommand(
            _ => { /* TASK-12: apertura file */ },
            p => p is ClientePiastra cp && !string.IsNullOrWhiteSpace(cp.Piastra?.Disegno?.PercorsoFile));
    }

    // --- Proprietà ---

    public int IdCliente
    {
        get => _idCliente;
        set { _idCliente = value; _ = LoadAsync(); }
    }

    public Cliente? Cliente
    {
        get => _cliente;
        private set => SetField(ref _cliente, value);
    }

    public bool IsAggiungiMacchinaVisible
    {
        get => _isAggiungiMacchinaVisible;
        set => SetField(ref _isAggiungiMacchinaVisible, value);
    }

    public MacchinaStandard? MacchinaSelezionata
    {
        get => _macchinaSelezionata;
        set => SetField(ref _macchinaSelezionata, value);
    }

    public string MatricolaNuova
    {
        get => _matricolaNuova;
        set => SetField(ref _matricolaNuova, value);
    }

    public string NoteNuovaMacchina
    {
        get => _noteNuovaMacchina;
        set => SetField(ref _noteNuovaMacchina, value);
    }

    public ObservableCollection<ClienteMacchina>   Macchine         { get; } = [];
    public ObservableCollection<ClientePiastra>    Piastre          { get; } = [];
    public ObservableCollection<CompatibilitaRow>  Compatibilita    { get; } = [];
    public ObservableCollection<MacchinaStandard>  MacchineDisponibili { get; } = [];

    // --- Comandi ---

    public ICommand TornaIndietroCommand            { get; }
    public ICommand AggiungiMacchinaCommand         { get; }
    public ICommand ConfermaAggiungiMacchinaCommand { get; }
    public ICommand AnnullaAggiungiMacchinaCommand  { get; }
    public ICommand RimuoviMacchinaCommand          { get; }
    public ICommand RimuoviPiastraCommand           { get; }
    public ICommand AprirDisegnoCommand             { get; }

    // --- Caricamento dati ---

    private async Task LoadAsync()
    {
        Cliente = await _clienteRepo.GetByIdAsync(_idCliente);
        if (Cliente is null) return;

        await Task.WhenAll(
            CaricaMacchineAsync(),
            CaricaPiastreAsync(),
            CaricaMacchineDisponibiliAsync());

        await CaricaCompatibilitaAsync();
    }

    private async Task CaricaMacchineAsync()
    {
        Macchine.Clear();
        foreach (var m in await _macchineRepo.GetByClienteAsync(_idCliente))
            Macchine.Add(m);
    }

    private async Task CaricaPiastreAsync()
    {
        Piastre.Clear();
        foreach (var p in await _piastreRepo.GetByClienteAsync(_idCliente))
            Piastre.Add(p);
    }

    private async Task CaricaMacchineDisponibiliAsync()
    {
        MacchineDisponibili.Clear();
        foreach (var m in await _macchineStdRepo.GetAttiveAsync())
            MacchineDisponibili.Add(m);
    }

    private async Task CaricaCompatibilitaAsync()
    {
        Compatibilita.Clear();
        foreach (var cm in Macchine)
        {
            var compatibili = await _compatRepo.GetByMacchinaAsync(cm.IdMacchinaStandard);
            foreach (var c in compatibili)
            {
                Compatibilita.Add(new CompatibilitaRow(
                    cm.MacchinaStandard.NomeMacchina,
                    c.Piastra.CodicePiastra,
                    c.Piastra.Descrizione,
                    c.Attiva));
            }
        }
    }

    // --- Operazioni ---

    private async Task ConfermaAggiungiMacchinaAsync()
    {
        if (MacchinaSelezionata is null || Cliente is null) return;

        await _macchineRepo.AddAsync(new ClienteMacchina
        {
            IdCliente          = Cliente.IdCliente,
            IdMacchinaStandard = MacchinaSelezionata.IdMacchinaStandard,
            Matricola          = string.IsNullOrWhiteSpace(MatricolaNuova) ? null : MatricolaNuova,
            Note               = string.IsNullOrWhiteSpace(NoteNuovaMacchina) ? null : NoteNuovaMacchina,
            Attiva             = true
        });

        await CaricaMacchineAsync();
        await CaricaCompatibilitaAsync();

        IsAggiungiMacchinaVisible = false;
        MacchinaSelezionata = null;
        MatricolaNuova = string.Empty;
        NoteNuovaMacchina = string.Empty;
    }

    private async Task RimuoviMacchinaAsync(ClienteMacchina macchina)
    {
        await _macchineRepo.DeleteAsync(macchina.IdClienteMacchina);
        Macchine.Remove(macchina);
        await CaricaCompatibilitaAsync();
    }

    private async Task RimuoviPiastraAsync(ClientePiastra piastra)
    {
        await _piastreRepo.DeleteAsync(piastra.IdClientePiastra);
        Piastre.Remove(piastra);
    }
}
