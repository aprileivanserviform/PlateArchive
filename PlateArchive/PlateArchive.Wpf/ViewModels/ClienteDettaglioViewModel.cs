using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using PlateArchive.Core.Enums;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Wpf.Commands;
using PlateArchive.Wpf.Services;

namespace PlateArchive.Wpf.ViewModels;

public record CompatibilitaRow(string NomeMacchina, string CodicePiastra, string? DescrizionePiastra, bool Attiva);

public record PiastraOpzione(Piastra Piastra, bool IsCompatibile);

public class ClienteDettaglioViewModel : ViewModelBase
{
    private readonly IClienteRepository          _clienteRepo;
    private readonly IClienteMacchinaRepository  _macchineRepo;
    private readonly IClientePiastraRepository   _piastreRepo;
    private readonly ICompatibilitaRepository    _compatRepo;
    private readonly IMacchinaStandardRepository _macchineStdRepo;
    private readonly IPiastraRepository          _piastraRepo;
    private readonly NavigationService           _navigation;

    private int _idCliente;
    private Cliente? _cliente;

    // Form macchina
    private bool _isAggiungiMacchinaVisible;
    private MacchinaStandard? _macchinaSelezionata;
    private string _matricolaNuova = string.Empty;
    private string _noteNuovaMacchina = string.Empty;

    // Form piastra
    private bool _isAggiungiPiastraVisible;
    private PiastraOpzione? _piastraSelezionata;
    private ClienteMacchina? _macchinaPerPiastra;
    private string? _errorePiastraEsistente;
    private string? _erroreDisegno;

    public ClienteDettaglioViewModel(
        IClienteRepository          clienteRepo,
        IClienteMacchinaRepository  macchineRepo,
        IClientePiastraRepository   piastreRepo,
        ICompatibilitaRepository    compatRepo,
        IMacchinaStandardRepository macchineStdRepo,
        IPiastraRepository          piastraRepo,
        NavigationService           navigation)
    {
        _clienteRepo     = clienteRepo;
        _macchineRepo    = macchineRepo;
        _piastreRepo     = piastreRepo;
        _compatRepo      = compatRepo;
        _macchineStdRepo = macchineStdRepo;
        _piastraRepo     = piastraRepo;
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

        AggiungiPiastraCommand = new RelayCommand(async _ => await AprirFormAggiungiPiastraAsync());

        ConfermaAggiungiPiastraCommand = new RelayCommand(
            async _ => await ConfermaAggiungiPiastraAsync(),
            _ => PiastraSelezionata is not null);

        AnnullaAggiungiPiastraCommand = new RelayCommand(_ => ChiudiFormPiastra());

        AprirDisegnoCommand = new RelayCommand(
            p => AprirDisegno((ClientePiastra)p!),
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

    public ObservableCollection<ClienteMacchina>   Macchine            { get; } = [];
    public ObservableCollection<ClientePiastra>    Piastre             { get; } = [];
    public ObservableCollection<CompatibilitaRow>  Compatibilita       { get; } = [];
    public ObservableCollection<MacchinaStandard>  MacchineDisponibili { get; } = [];
    public ObservableCollection<PiastraOpzione>    PiastreDisponibili  { get; } = [];

    // Form piastra
    public bool IsAggiungiPiastraVisible
    {
        get => _isAggiungiPiastraVisible;
        set => SetField(ref _isAggiungiPiastraVisible, value);
    }

    public PiastraOpzione? PiastraSelezionata
    {
        get => _piastraSelezionata;
        set => SetField(ref _piastraSelezionata, value);
    }

    public ClienteMacchina? MacchinaPerPiastra
    {
        get => _macchinaPerPiastra;
        set => SetField(ref _macchinaPerPiastra, value);
    }

    public string? ErrorePiastraEsistente
    {
        get => _errorePiastraEsistente;
        set { if (SetField(ref _errorePiastraEsistente, value)) OnPropertyChanged(nameof(IsErrorePiastraVisible)); }
    }

    public bool IsErrorePiastraVisible => !string.IsNullOrEmpty(_errorePiastraEsistente);

    public string? ErroreDisegno
    {
        get => _erroreDisegno;
        set { if (SetField(ref _erroreDisegno, value)) OnPropertyChanged(nameof(IsErroreDisegnoVisible)); }
    }

    public bool IsErroreDisegnoVisible => !string.IsNullOrEmpty(_erroreDisegno);

    // --- Comandi ---

    public ICommand TornaIndietroCommand            { get; }
    public ICommand AggiungiMacchinaCommand         { get; }
    public ICommand ConfermaAggiungiMacchinaCommand { get; }
    public ICommand AnnullaAggiungiMacchinaCommand  { get; }
    public ICommand RimuoviMacchinaCommand          { get; }
    public ICommand AggiungiPiastraCommand          { get; }
    public ICommand ConfermaAggiungiPiastraCommand  { get; }
    public ICommand AnnullaAggiungiPiastraCommand   { get; }
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

    private async Task AprirFormAggiungiPiastraAsync()
    {
        // Raccoglie gli IdPiastra compatibili con qualsiasi macchina del cliente
        var idCompatibili = new HashSet<int>();
        foreach (var cm in Macchine)
        {
            var comp = await _compatRepo.GetByMacchinaAsync(cm.IdMacchinaStandard);
            foreach (var c in comp)
                idCompatibili.Add(c.IdPiastra);
        }

        var tutte    = await _piastraRepo.GetAllAsync();
        var associate = Piastre.Select(cp => cp.IdPiastra).ToHashSet();

        PiastreDisponibili.Clear();

        // Compatibili prima (badge visivo), poi le altre — entrambi ordinati per codice
        var disponibili = tutte
            .Where(p => !associate.Contains(p.IdPiastra))
            .Select(p => new PiastraOpzione(p, idCompatibili.Contains(p.IdPiastra)))
            .OrderByDescending(o => o.IsCompatibile)
            .ThenBy(o => o.Piastra.CodicePiastra);

        foreach (var po in disponibili)
            PiastreDisponibili.Add(po);

        PiastraSelezionata       = null;
        MacchinaPerPiastra       = null;
        ErrorePiastraEsistente   = null;
        IsAggiungiPiastraVisible = true;
    }

    private async Task ConfermaAggiungiPiastraAsync()
    {
        if (PiastraSelezionata is null || Cliente is null) return;

        var idPiastra = PiastraSelezionata.Piastra.IdPiastra;

        if (await _piastreRepo.ExistsAsync(Cliente.IdCliente, idPiastra))
        {
            ErrorePiastraEsistente = "Questa piastra è già associata al cliente.";
            return;
        }

        await _piastreRepo.AddAsync(new ClientePiastra
        {
            IdCliente         = Cliente.IdCliente,
            IdPiastra         = idPiastra,
            IdClienteMacchina = MacchinaPerPiastra?.IdClienteMacchina,
            Stato             = StatoClientePiastra.Attiva
        });

        await CaricaPiastreAsync();
        ChiudiFormPiastra();
    }

    private void ChiudiFormPiastra()
    {
        IsAggiungiPiastraVisible = false;
        PiastraSelezionata       = null;
        MacchinaPerPiastra       = null;
        ErrorePiastraEsistente   = null;
        PiastreDisponibili.Clear();
    }

    private async Task RimuoviPiastraAsync(ClientePiastra piastra)
    {
        await _piastreRepo.DeleteAsync(piastra.IdClientePiastra);
        Piastre.Remove(piastra);
    }

    private void AprirDisegno(ClientePiastra cp)
    {
        var percorso = cp.Piastra?.Disegno?.PercorsoFile;
        if (string.IsNullOrEmpty(percorso)) return;

        ErroreDisegno = null;

        if (!File.Exists(percorso))
        {
            ErroreDisegno = $"File non trovato: {percorso}";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(percorso) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ErroreDisegno = $"Impossibile aprire il file: {ex.Message}";
        }
    }
}
