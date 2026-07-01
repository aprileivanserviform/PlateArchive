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

/// <summary>
/// Riga di compatibilità usata nella tabella "Compatibilità macchine/piastre" del dettaglio cliente.
/// Record immutabile: viene ricreato ad ogni reload.
/// </summary>
public record CompatibilitaRow(string NomeMacchina, string CodicePiastra, string? DescrizionePiastra, bool Attiva);

/// <summary>
/// Piastra con indicazione di compatibilità con le macchine del cliente corrente.
/// Usata nel ComboBox "Aggiungi piastra": mostra prima le piastre compatibili (badge visivo).
/// </summary>
public record PiastraOpzione(Piastra Piastra, bool IsCompatibile);

/// <summary>
/// ViewModel del dettaglio cliente (schermata aperta da ClientiViewModel → AprirDettaglioCommand).
/// Gestisce tre sezioni collegate:
/// <list type="bullet">
///   <item><b>Macchine possedute</b>: ClienteMacchina — modello fisico + matricola unità</item>
///   <item><b>Piastre associate</b>: ClientePiastra — piastre usate dal cliente</item>
///   <item><b>Compatibilità</b>: riepilogo incrociato macchine ↔ piastre (sola lettura)</item>
/// </list>
/// Il caricamento avviene quando NavigationService imposta <see cref="IdCliente"/>.
/// </summary>
public class ClienteDettaglioViewModel : ViewModelBase
{
    private readonly IClienteRepository          _clienteRepo;
    private readonly IClienteMacchinaRepository  _macchineRepo;
    private readonly IClientePiastraRepository   _piastreRepo;
    private readonly ICompatibilitaRepository    _compatRepo;
    private readonly IMacchinaStandardRepository _macchineStdRepo;
    private readonly IPiastraRepository          _piastraRepo;
    private readonly NavigationService           _navigation;

    private int     _idCliente;
    private Cliente? _cliente;

    // Form aggiungi macchina
    private bool              _isAggiungiMacchinaVisible;
    private MacchinaStandard? _macchinaSelezionata;
    private string            _matricolaNuova     = string.Empty;
    private string            _noteNuovaMacchina  = string.Empty;

    // Form aggiungi piastra
    private bool           _isAggiungiPiastraVisible;
    private PiastraOpzione? _piastraSelezionata;
    private ClienteMacchina? _macchinaPerPiastra;
    private string?         _errorePiastraEsistente;
    private string?         _erroreDisegno;

    // Lista completa piastre del cliente — filtrata in Piastre (storiche on/off).
    private readonly ObservableCollection<ClientePiastra> _tuttePiastre = [];
    private bool _mostraStoriche;

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

        // Torna alla lista clienti
        TornaIndietroCommand = new RelayCommand(_ => _navigation.Navigate<ClientiViewModel>());

        // Apre il pannello inline per aggiungere una macchina (ricarica la lista al click)
        AggiungiMacchinaCommand = new RelayCommand(async _ => await ApriAggiungiMacchinaAsync());

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

        ToggleStatoPiastraCommand = new RelayCommand(
            async p => await ToggleStatoPiastraAsync((ClientePiastra)p!));

        ToggleAttivaCommand = new RelayCommand(
            async p => await ToggleAttivaAsync((ClienteMacchina)p!));
    }

    // ─── Dati cliente ─────────────────────────────────────────────────────────

    /// <summary>
    /// Settato da NavigationService subito dopo la costruzione del ViewModel.
    /// Il setter scatena il caricamento asincrono di tutti i dati del cliente.
    /// </summary>
    public int IdCliente
    {
        get => _idCliente;
        set => _idCliente = value;
    }

    // ─── Inizializzazione navigazione ─────────────────────────────────────────

    public override Task OnNavigatedAsync() => LoadAsync();

    public Cliente? Cliente
    {
        get => _cliente;
        private set => SetField(ref _cliente, value);
    }

    // ─── Form aggiungi macchina ───────────────────────────────────────────────

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

    /// <summary>Matricola fisica dell'unità macchina (es. numero di serie dell'esemplare).</summary>
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

    // ─── Collezioni dati ─────────────────────────────────────────────────────

    /// <summary>Macchine possedute dal cliente (modello fisico con matricola).</summary>
    public ObservableCollection<ClienteMacchina>   Macchine            { get; } = [];

    /// <summary>Piastre associate al cliente (filtrate da _tuttePiastre per MostraStoriche).</summary>
    public ObservableCollection<ClientePiastra>    Piastre             { get; } = [];

    /// <summary>Incrociato macchine × piastre compatibili (riepilogo sola lettura).</summary>
    public ObservableCollection<CompatibilitaRow>  Compatibilita       { get; } = [];

    /// <summary>Tutti i modelli macchina attivi — ComboBox "Seleziona modello" nel form.</summary>
    public ObservableCollection<MacchinaStandard>  MacchineDisponibili { get; } = [];

    /// <summary>
    /// Piastre non ancora associate al cliente, con indicazione di compatibilità.
    /// Compatibili prima, poi le altre — ordinate per CodicePiastra.
    /// </summary>
    public ObservableCollection<PiastraOpzione>    PiastreDisponibili  { get; } = [];

    // ─── Form aggiungi piastra ────────────────────────────────────────────────

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

    /// <summary>
    /// Opzionale: macchina specifica a cui collegare la piastra (IdClienteMacchina).
    /// Nullable by design — il cliente può avere una piastra senza specificare la macchina.
    /// </summary>
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

    // ─── Comandi ─────────────────────────────────────────────────────────────

    public ICommand TornaIndietroCommand            { get; }
    public ICommand AggiungiMacchinaCommand         { get; }
    public ICommand ConfermaAggiungiMacchinaCommand { get; }
    public ICommand AnnullaAggiungiMacchinaCommand  { get; }
    public ICommand RimuoviMacchinaCommand          { get; }
    public ICommand ToggleAttivaCommand             { get; }
    public ICommand AggiungiPiastraCommand          { get; }
    public ICommand ConfermaAggiungiPiastraCommand  { get; }
    public ICommand AnnullaAggiungiPiastraCommand   { get; }
    public ICommand RimuoviPiastraCommand           { get; }
    public ICommand AprirDisegnoCommand             { get; }
    public ICommand ToggleStatoPiastraCommand       { get; }

    /// <summary>Se true, mostra anche le piastre con stato Obsoleta nella lista.</summary>
    public bool MostraStoriche
    {
        get => _mostraStoriche;
        set { if (SetField(ref _mostraStoriche, value)) AggiornaPiastre(); }
    }

    // ─── Caricamento dati ─────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        Cliente = await _clienteRepo.GetByIdAsync(_idCliente);
        if (Cliente is null) return;

        // Sequenziale: lo stesso DbContext non supporta query concorrenti.
        await CaricaMacchineAsync();
        await CaricaPiastreAsync();
        await CaricaMacchineDisponibiliAsync();
        // Compatibilità dipende da Macchine → eseguita dopo.
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
        _tuttePiastre.Clear();
        foreach (var p in await _piastreRepo.GetByClienteAsync(_idCliente))
            _tuttePiastre.Add(p);
        AggiornaPiastre();
    }

    /// <summary>Filtra _tuttePiastre escludendo le obsolete (a meno che MostraStoriche = true).</summary>
    private void AggiornaPiastre()
    {
        Piastre.Clear();
        foreach (var p in _tuttePiastre.Where(p => _mostraStoriche || p.Stato != StatoClientePiastra.Obsoleta))
            Piastre.Add(p);
    }

    private async Task CaricaMacchineDisponibiliAsync()
    {
        MacchineDisponibili.Clear();
        foreach (var m in await _macchineStdRepo.GetAttiveAsync())
            MacchineDisponibili.Add(m);
    }

    /// <summary>
    /// Per ogni macchina del cliente, carica le piastre tecnicamente compatibili
    /// e le aggiunge alla tabella riepilogativa Compatibilita.
    /// </summary>
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

    // ─── Aggiungi macchina ────────────────────────────────────────────────────

    private async Task ApriAggiungiMacchinaAsync()
    {
        // Ricarica sempre al click: garantisce che le macchine create dopo il caricamento iniziale
        // della view siano visibili, e risolve la race condition con LoadAsync() fire-and-forget.
        await CaricaMacchineDisponibiliAsync();
        MacchinaSelezionata = null;
        MatricolaNuova = string.Empty;
        NoteNuovaMacchina = string.Empty;
        IsAggiungiMacchinaVisible = true;
    }

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
        // Ricarica compatibilità perché la nuova macchina potrebbe avere piastre associate.
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

    // ─── Aggiungi piastra ─────────────────────────────────────────────────────

    private async Task AprirFormAggiungiPiastraAsync()
    {
        // Raccoglie gli IdPiastra compatibili con qualsiasi macchina del cliente.
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

        // Compatibili prima (badge visivo), poi le altre — entrambi ordinati per codice.
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

        // Doppia verifica sul DB (la lista locale potrebbe essere parziale).
        if (await _piastreRepo.ExistsAsync(Cliente.IdCliente, idPiastra))
        {
            ErrorePiastraEsistente = "Questa piastra è già associata al cliente.";
            return;
        }

        await _piastreRepo.AddAsync(new ClientePiastra
        {
            IdCliente         = Cliente.IdCliente,
            IdPiastra         = idPiastra,
            // IdClienteMacchina nullable: il cliente può avere la piastra senza una macchina specifica.
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
        _tuttePiastre.Remove(piastra);
        AggiornaPiastre();
    }

    // ─── Toggle attiva macchina (Attiva ↔ Inattiva) ──────────────────────────

    private async Task ToggleAttivaAsync(ClienteMacchina cm)
    {
        cm.Attiva = !cm.Attiva;
        await _macchineRepo.UpdateAsync(cm);
        // Notifica la DataGrid che la riga è cambiata (non è un ObservableObject, usiamo Replace).
        var idx = Macchine.IndexOf(cm);
        if (idx >= 0) { Macchine.RemoveAt(idx); Macchine.Insert(idx, cm); }
    }

    // ─── Toggle stato piastra (Attiva ↔ Obsoleta) ────────────────────────────

    private async Task ToggleStatoPiastraAsync(ClientePiastra cp)
    {
        var nuovoStato = cp.Stato == StatoClientePiastra.Attiva
            ? StatoClientePiastra.Obsoleta
            : StatoClientePiastra.Attiva;
        cp.Stato = nuovoStato;
        await _piastreRepo.SetStatoAsync(cp.IdClientePiastra, nuovoStato);
        // Riapplica il filtro: se MostraStoriche = false, la piastra obsoleta sparisce.
        AggiornaPiastre();
    }

    // ─── Apertura file disegno ────────────────────────────────────────────────

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
