using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using PlateArchive.Core.Enums;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Services;
using PlateArchive.Wpf.Commands;

namespace PlateArchive.Wpf.ViewModels;

/// <summary>
/// ViewModel della schermata Piastre — entità centrale del sistema.
/// Layout: lista con 4 filtri (ricerca, stato, categoria, formato) | pannello destra.
/// <para>
/// Il pannello destra mostra alternativamente:
/// - <b>Form creazione/modifica</b> quando <see cref="IsFormVisible"/> = true
/// - <b>Dettaglio piastra</b> quando <see cref="IsDetailVisible"/> = true
/// </para>
/// Il dettaglio include macchine compatibili, clienti associati e il disegno tecnico (1:1).
/// Il disegno è associabile via drag &amp; drop. Le piastre SpecialeCliente hanno un
/// cliente esclusivo e il file viene archiviato in Clienti\{CodiceCliente}\.
/// </summary>
public class PiastreViewModel : ViewModelBase
{
    private readonly IPiastraRepository          _piastreRepo;
    private readonly ICompatibilitaRepository    _compatRepo;
    private readonly IClientePiastraRepository   _clientiPiastreRepo;
    private readonly IMacchinaStandardRepository _macchineRepo;
    private readonly IDisegnoRepository          _disegniRepo;
    private readonly IFileArchivioService        _fileArchivio;
    private readonly ICategoriaPiastraRepository _categorieRepo;
    private readonly IFormatoMacchinaRepository  _formatiRepo;
    private readonly IClienteRepository          _clientiRepo;

    private readonly ObservableCollection<Piastra> _tutti = [];
    private List<Cliente> _tuttiClienti = [];

    private string    _filtroRicerca = string.Empty;
    private Piastra?  _piastraSelezionata;
    private bool      _isFormVisible;
    private bool      _isModifica;
    private int       _idPiastraInModifica;

    // ── Form campi ────────────────────────────────────────────────────────────
    private string             _formCodicePiastra        = string.Empty;
    private string             _formCodiceArticolo       = string.Empty;
    private string             _formDescrizione          = string.Empty;
    private StatoPiastra       _formStato                = StatoPiastra.Attiva;
    private TipoPiastra        _formTipo                 = TipoPiastra.Standard;
    private CategoriaPiastra?  _formCategoriaSelezionata;
    private FormatoMacchina?   _formFormatoSelezionato;
    private string             _formLarghezza            = string.Empty;
    private string             _formAltezza              = string.Empty;
    private string             _formSpessore             = string.Empty;
    private string             _formDurezza              = string.Empty;
    private string             _formPeso                 = string.Empty;
    private string             _formNote                 = string.Empty;
    private string?            _erroreCodiceDuplicato;
    private string?            _erroreDisegno;
    private string?            _percorsoDisegnoPendente;

    // ── Form: cliente esclusivo (solo per SpecialeCliente) ────────────────────
    private Cliente? _formClienteEsclusivo;
    private string   _filtroClienteEsclusivo     = string.Empty;
    private string   _formFiltroClienteAssociato = string.Empty;

    // ── Pannello aggiungi macchina ─────────────────────────────────────────────
    private bool              _isAggiungiMacchinaVisible;
    private MacchinaStandard? _macchinaCompatibileDaAggiungere;

    // ── Pannello aggiungi cliente associato ───────────────────────────────────
    private bool          _isAggiungiClienteVisible;
    private Cliente?      _clienteSelezionato;
    private List<Cliente> _tuttiClientiDisponibili = [];
    private string        _filtroCliente           = string.Empty;

    private Task _loadDettaglioTask = Task.CompletedTask;

    public PiastreViewModel(
        IPiastraRepository          piastreRepo,
        ICompatibilitaRepository    compatRepo,
        IClientePiastraRepository   clientiPiastreRepo,
        IMacchinaStandardRepository macchineRepo,
        IDisegnoRepository          disegniRepo,
        IFileArchivioService        fileArchivio,
        ICategoriaPiastraRepository categorieRepo,
        IFormatoMacchinaRepository  formatiRepo,
        IClienteRepository          clientiRepo)
    {
        _piastreRepo        = piastreRepo;
        _compatRepo         = compatRepo;
        _clientiPiastreRepo = clientiPiastreRepo;
        _macchineRepo       = macchineRepo;
        _disegniRepo        = disegniRepo;
        _fileArchivio       = fileArchivio;
        _categorieRepo      = categorieRepo;
        _formatiRepo        = formatiRepo;
        _clientiRepo        = clientiRepo;

        // Registra tutti i filtri colonna → riesegui AggiornaFiltro al cambio
        foreach (var f in new[] {
            FiltroCodice, FiltroDescrizione, FiltroArtGestionale,
            FiltroCategoria, FiltroFormato, FiltroTipo, FiltroStato,
            FiltroLarghezza, FiltroAltezza, FiltroSpessore, FiltroDurezza, FiltroPeso,
            FiltroDataCreazione, FiltroDataModifica })
        {
            f.Cambiato += AggiornaFiltro;
        }

        NuovaCommand                    = new RelayCommand(_ => ApriFormNuova());
        ModificaCommand                 = new RelayCommand(_ => ApriFormModifica(),                          _ => PiastraSelezionata is not null);
        SalvaCommand                    = new RelayCommand(async _ => await SalvaAsync());
        AnnullaFormCommand              = new RelayCommand(_ => ChiudiForm());
        EliminaCommand                  = new RelayCommand(async _ => await EliminaAsync(),                  _ => PiastraSelezionata is not null);
        SfogliaFileFormCommand          = new RelayCommand(_ => SfogliaFileForm());
        AggiungiMacchinaCommand         = new RelayCommand(async _ => await ApriAggiungiMacchinaAsync(),     _ => PiastraSelezionata is not null);
        ConfermaAggiungiMacchinaCommand = new RelayCommand(async _ => await ConfermaAggiungiMacchinaAsync(), _ => MacchinaCompatibileDaAggiungere is not null);
        AnnullaAggiungiMacchinaCommand  = new RelayCommand(_ => ChiudiAggiungiMacchina());
        RimuoviCompatibilitaCommand     = new RelayCommand(async p => await RimuoviCompatibilitaAsync(p));
        AprirDisegnoCommand             = new RelayCommand(_ => AprirDisegno(),  _ => DisegnoCorrente is not null);
        RimuoviDisegnoCommand           = new RelayCommand(async _ => await RimuoviDisegnoAsync(), _ => DisegnoCorrente is not null);
        AggiungiClienteCommand          = new RelayCommand(async _ => await ApriAggiungiClienteAsync(),      _ => PiastraSelezionata is not null);
        ConfermaAggiungiClienteCommand  = new RelayCommand(async _ => await ConfermaAggiungiClienteAsync(),  _ => ClienteSelezionato is not null);
        AnnullaAggiungiClienteCommand   = new RelayCommand(_ => ChiudiAggiungiCliente());
        RimuoviClienteCommand               = new RelayCommand(async p => await RimuoviClienteAsync(p));
        SelezionaClienteCommand             = new RelayCommand(p => ClienteSelezionato = (Cliente)p!);
        RimuoviClienteSelezionatoCommand    = new RelayCommand(_ => ClienteSelezionato = null);
        SelezionaClienteEsclusivoCommand        = new RelayCommand(p => FormClienteEsclusivo = (Cliente)p!);
        RimuoviClienteEsclusivoCommand          = new RelayCommand(_ => FormClienteEsclusivo = null);
        SelezionaClienteAssociatoFormCommand    = new RelayCommand(p => AggiungiClienteAssociatoForm((Cliente)p!));
        RimuoviClienteAssociatoFormCommand      = new RelayCommand(p => RimuoviClienteAssociatoForm((Cliente)p!));
        SfogliaFileDettaglioCommand             = new RelayCommand(async _ => await SfogliaFileDettaglioAsync(), _ => PiastraSelezionata is not null && DisegnoCorrente is null);
    }

    // ─── Lookup per i ComboBox del form ──────────────────────────────────────

    public ObservableCollection<CategoriaPiastra> CategoriePiastre { get; } = [];
    public ObservableCollection<FormatoMacchina>  FormatiMacchine  { get; } = [];

    // ─── Filtri lista ─────────────────────────────────────────────────────────

    public string FiltroRicerca
    {
        get => _filtroRicerca;
        set { if (SetField(ref _filtroRicerca, value)) AggiornaFiltro(); }
    }

    // Filtri per colonna (assegnati come Header delle DataGridColumn in code-behind)
    public FiltroColonna FiltroCodice        { get; } = new("Codice",          FiltroColonnaTipo.Testo);
    public FiltroColonna FiltroDescrizione   { get; } = new("Descrizione",     FiltroColonnaTipo.Testo);
    public FiltroColonna FiltroArtGestionale { get; } = new("Art. gestionale", FiltroColonnaTipo.Testo);
    public FiltroColonna FiltroCategoria     { get; } = new("Categoria",       FiltroColonnaTipo.Enum);
    public FiltroColonna FiltroFormato       { get; } = new("Formato",         FiltroColonnaTipo.Enum);
    public FiltroColonna FiltroTipo          { get; } = new("Tipo",            FiltroColonnaTipo.Enum);
    public FiltroColonna FiltroStato         { get; } = new("Stato",           FiltroColonnaTipo.Enum);
    public FiltroColonna FiltroLarghezza     { get; } = new("Larghezza",       FiltroColonnaTipo.Numerico);
    public FiltroColonna FiltroAltezza       { get; } = new("Altezza",         FiltroColonnaTipo.Numerico);
    public FiltroColonna FiltroSpessore      { get; } = new("Spessore",        FiltroColonnaTipo.Numerico);
    public FiltroColonna FiltroDurezza       { get; } = new("Durezza",         FiltroColonnaTipo.Numerico);
    public FiltroColonna FiltroPeso          { get; } = new("Peso",            FiltroColonnaTipo.Numerico);
    public FiltroColonna FiltroDataCreazione { get; } = new("Data creazione",  FiltroColonnaTipo.Data);
    public FiltroColonna FiltroDataModifica  { get; } = new("Ultima modifica", FiltroColonnaTipo.Data);

    public IEnumerable<StatoPiastra> StatiPiastra { get; } = Enum.GetValues<StatoPiastra>();
    public IEnumerable<TipoPiastra>  TipiPiastra  { get; } = Enum.GetValues<TipoPiastra>();

    public ObservableCollection<Piastra> PiastreFiltrate { get; } = [];

    // ─── Selezione ───────────────────────────────────────────────────────────

    public Piastra? PiastraSelezionata
    {
        get => _piastraSelezionata;
        set
        {
            if (SetField(ref _piastraSelezionata, value))
            {
                ErroreDisegno = null;
                OnPropertyChanged(nameof(IsDetailVisible));
                OnPropertyChanged(nameof(IsPannelloDxVisible));
                if (IsAggiungiMacchinaVisible) ChiudiAggiungiMacchina();
                if (IsAggiungiClienteVisible)  ChiudiAggiungiCliente();
                if (IsFormVisible && IsModifica && value is not null)
                    ApriFormModifica();
                _loadDettaglioTask = LoadDettaglioAsync();
            }
        }
    }

    public string? ErroreDisegno
    {
        get => _erroreDisegno;
        set
        {
            if (SetField(ref _erroreDisegno, value))
                OnPropertyChanged(nameof(IsErroreDisegnoVisible));
        }
    }

    public bool IsErroreDisegnoVisible => !string.IsNullOrEmpty(_erroreDisegno);

    // ─── Pannello dettaglio ───────────────────────────────────────────────────

    public ObservableCollection<PiastraMacchinaCompatibile> MacchineCompatibili { get; } = [];
    public ObservableCollection<ClientePiastra>             ClientiAssociati    { get; } = [];
    public ObservableCollection<MacchinaStandard>           MacchineDisponibili { get; } = [];
    public ObservableCollection<Cliente>                    ClientiSuggeriti    { get; } = [];

    private Disegno? _disegnoCorrente;
    public Disegno? DisegnoCorrente
    {
        get => _disegnoCorrente;
        set
        {
            if (SetField(ref _disegnoCorrente, value))
            {
                OnPropertyChanged(nameof(IsDisegnoPresente));
                OnPropertyChanged(nameof(IsDisegnoAssente));
                OnPropertyChanged(nameof(IsFormDisegnoPresente));
                OnPropertyChanged(nameof(IsFormDisegnoAssente));
            }
        }
    }

    public bool IsDisegnoPresente     => DisegnoCorrente is not null;
    public bool IsDisegnoAssente      => DisegnoCorrente is null;
    // Nel form: in modifica mostra il disegno esistente; in creazione mostra sempre la drop zone
    public bool IsFormDisegnoPresente => IsModifica && DisegnoCorrente is not null;
    public bool IsFormDisegnoAssente  => !IsModifica || DisegnoCorrente is null;

    // ─── Pannello aggiungi macchina compatibile ───────────────────────────────

    public bool IsAggiungiMacchinaVisible
    {
        get => _isAggiungiMacchinaVisible;
        set => SetField(ref _isAggiungiMacchinaVisible, value);
    }

    public MacchinaStandard? MacchinaCompatibileDaAggiungere
    {
        get => _macchinaCompatibileDaAggiungere;
        set => SetField(ref _macchinaCompatibileDaAggiungere, value);
    }

    // ─── Pannello aggiungi cliente associato ──────────────────────────────────

    public bool IsAggiungiClienteVisible
    {
        get => _isAggiungiClienteVisible;
        set => SetField(ref _isAggiungiClienteVisible, value);
    }

    public Cliente? ClienteSelezionato
    {
        get => _clienteSelezionato;
        set
        {
            if (SetField(ref _clienteSelezionato, value))
            {
                if (value is not null)
                {
                    _filtroCliente = string.Empty;
                    OnPropertyChanged(nameof(FiltroCliente));
                    ClientiSuggeriti.Clear();
                    OnPropertyChanged(nameof(IsClienteSuggerimentiVisible));
                }
                OnPropertyChanged(nameof(IsClienteSelezionatoVisible));
                OnPropertyChanged(nameof(IsClienteSearchVisible));
            }
        }
    }

    public string FiltroCliente
    {
        get => _filtroCliente;
        set { if (SetField(ref _filtroCliente, value)) AggiornaClientiSuggeriti(); }
    }

    public bool IsClienteSelezionatoVisible  => ClienteSelezionato is not null;
    public bool IsClienteSearchVisible       => ClienteSelezionato is null;
    public bool IsClienteSuggerimentiVisible => ClientiSuggeriti.Count > 0;

    // ─── Stato pannello destra ────────────────────────────────────────────────

    public bool IsFormVisible
    {
        get => _isFormVisible;
        set
        {
            if (SetField(ref _isFormVisible, value))
            {
                OnPropertyChanged(nameof(IsDetailVisible));
                OnPropertyChanged(nameof(IsPannelloDxVisible));
            }
        }
    }

    public bool IsModifica
    {
        get => _isModifica;
        set
        {
            if (SetField(ref _isModifica, value))
            {
                OnPropertyChanged(nameof(FormTitolo));
                OnPropertyChanged(nameof(IsFormDisegnoPresente));
                OnPropertyChanged(nameof(IsFormDisegnoAssente));
            }
        }
    }

    public bool   IsDetailVisible     => PiastraSelezionata is not null && !IsFormVisible;
    public bool   IsPannelloDxVisible => IsDetailVisible || IsFormVisible;
    public string FormTitolo          => IsModifica ? "Modifica piastra" : "Nuova piastra";

    // ─── Campi form creazione/modifica ────────────────────────────────────────

    public string FormCodicePiastra
    {
        get => _formCodicePiastra;
        set { if (SetField(ref _formCodicePiastra, value)) ControllaDuplicato(value); }
    }

    public string FormCodiceArticolo
    {
        get => _formCodiceArticolo;
        set => SetField(ref _formCodiceArticolo, value);
    }

    public string FormDescrizione
    {
        get => _formDescrizione;
        set => SetField(ref _formDescrizione, value);
    }

    public StatoPiastra FormStato
    {
        get => _formStato;
        set => SetField(ref _formStato, value);
    }

    public TipoPiastra FormTipo
    {
        get => _formTipo;
        set
        {
            if (SetField(ref _formTipo, value))
            {
                if (value == TipoPiastra.Standard) FormClienteEsclusivo = null;
                OnPropertyChanged(nameof(IsClienteEsclusivoVisible));
                OnPropertyChanged(nameof(IsAssociazioneClientiVisible));
            }
        }
    }

    /// <summary>True se la sezione "cliente esclusivo" deve essere visibile nel form.</summary>
    public bool IsClienteEsclusivoVisible    => FormTipo == TipoPiastra.SpecialeCliente;
    /// <summary>True se la sezione "associa a clienti" (Standard) deve essere visibile nel form.</summary>
    public bool IsAssociazioneClientiVisible => FormTipo == TipoPiastra.Standard;

    // ── Typeahead cliente esclusivo nel form ──────────────────────────────────

    public ObservableCollection<Cliente> ClientiEsclusiviSuggeriti      { get; } = [];
    public ObservableCollection<Cliente> FormClientiDaAssociare          { get; } = [];
    public ObservableCollection<Cliente> FormClientiAssociatiSuggeriti   { get; } = [];

    public Cliente? FormClienteEsclusivo
    {
        get => _formClienteEsclusivo;
        set
        {
            if (SetField(ref _formClienteEsclusivo, value))
            {
                if (value is not null)
                {
                    _filtroClienteEsclusivo = string.Empty;
                    OnPropertyChanged(nameof(FiltroClienteEsclusivo));
                    ClientiEsclusiviSuggeriti.Clear();
                    OnPropertyChanged(nameof(IsClientiEsclusiviSuggerimentiVisible));
                }
                OnPropertyChanged(nameof(IsClienteEsclusivoSelezionatoVisible));
                OnPropertyChanged(nameof(IsClienteEsclusivoSearchVisible));
            }
        }
    }

    public string FiltroClienteEsclusivo
    {
        get => _filtroClienteEsclusivo;
        set { if (SetField(ref _filtroClienteEsclusivo, value)) AggiornaClientiEsclusiviSuggeriti(); }
    }

    public bool IsClienteEsclusivoSelezionatoVisible   => FormClienteEsclusivo is not null;
    public bool IsClienteEsclusivoSearchVisible         => FormClienteEsclusivo is null;
    public bool IsClientiEsclusiviSuggerimentiVisible   => ClientiEsclusiviSuggeriti.Count > 0;

    // ── Typeahead clienti da associare (Standard) ─────────────────────────────

    public string FormFiltroClienteAssociato
    {
        get => _formFiltroClienteAssociato;
        set { if (SetField(ref _formFiltroClienteAssociato, value)) AggiornaFormClientiAssociatiSuggeriti(); }
    }

    public bool IsFormClientiAssociatiSuggerimentiVisible => FormClientiAssociatiSuggeriti.Count > 0;

    public CategoriaPiastra? FormCategoriaSelezionata
    {
        get => _formCategoriaSelezionata;
        set => SetField(ref _formCategoriaSelezionata, value);
    }

    public FormatoMacchina? FormFormatoSelezionato
    {
        get => _formFormatoSelezionato;
        set => SetField(ref _formFormatoSelezionato, value);
    }

    public string FormLarghezza
    {
        get => _formLarghezza;
        set => SetField(ref _formLarghezza, value);
    }

    public string FormAltezza
    {
        get => _formAltezza;
        set => SetField(ref _formAltezza, value);
    }

    public string FormSpessore
    {
        get => _formSpessore;
        set => SetField(ref _formSpessore, value);
    }

    public string FormDurezza
    {
        get => _formDurezza;
        set => SetField(ref _formDurezza, value);
    }

    public string FormPeso
    {
        get => _formPeso;
        set => SetField(ref _formPeso, value);
    }

    public string FormNote
    {
        get => _formNote;
        set => SetField(ref _formNote, value);
    }

    public string? ErroreCodiceDuplicato
    {
        get => _erroreCodiceDuplicato;
        set
        {
            if (SetField(ref _erroreCodiceDuplicato, value))
                OnPropertyChanged(nameof(IsErroreVisible));
        }
    }

    public bool IsErroreVisible => !string.IsNullOrEmpty(_erroreCodiceDuplicato);

    public string? PercorsoDisegnoPendente
    {
        get => _percorsoDisegnoPendente;
        set
        {
            if (SetField(ref _percorsoDisegnoPendente, value))
            {
                OnPropertyChanged(nameof(IsDisegnoPendenteVisible));
                OnPropertyChanged(nameof(IsDisegnoPendenteAssente));
                OnPropertyChanged(nameof(NomeFilePendente));
            }
        }
    }

    public bool    IsDisegnoPendenteVisible => !string.IsNullOrEmpty(_percorsoDisegnoPendente);
    public bool    IsDisegnoPendenteAssente => string.IsNullOrEmpty(_percorsoDisegnoPendente);
    public string? NomeFilePendente         => Path.GetFileName(_percorsoDisegnoPendente);

    // ─── Comandi ─────────────────────────────────────────────────────────────

    public ICommand NuovaCommand                    { get; }
    public ICommand ModificaCommand                 { get; }
    public ICommand SalvaCommand                    { get; }
    public ICommand AnnullaFormCommand              { get; }
    public ICommand EliminaCommand                  { get; }
    public ICommand SfogliaFileFormCommand          { get; }
    public ICommand AggiungiMacchinaCommand         { get; }
    public ICommand ConfermaAggiungiMacchinaCommand { get; }
    public ICommand AnnullaAggiungiMacchinaCommand  { get; }
    public ICommand RimuoviCompatibilitaCommand     { get; }
    public ICommand AprirDisegnoCommand             { get; }
    public ICommand RimuoviDisegnoCommand           { get; }
    public ICommand AggiungiClienteCommand              { get; }
    public ICommand ConfermaAggiungiClienteCommand      { get; }
    public ICommand AnnullaAggiungiClienteCommand       { get; }
    public ICommand RimuoviClienteCommand               { get; }
    public ICommand SelezionaClienteCommand             { get; }
    public ICommand RimuoviClienteSelezionatoCommand    { get; }
    public ICommand SelezionaClienteEsclusivoCommand        { get; }
    public ICommand RimuoviClienteEsclusivoCommand          { get; }
    public ICommand SelezionaClienteAssociatoFormCommand    { get; }
    public ICommand RimuoviClienteAssociatoFormCommand      { get; }
    public ICommand SfogliaFileDettaglioCommand             { get; }

    // ─── Inizializzazione navigazione ─────────────────────────────────────────

    public override Task OnNavigatedAsync() => LoadAsync();

    // ─── Caricamento ─────────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        var categorie = await _categorieRepo.GetAllAsync();
        foreach (var c in categorie) CategoriePiastre.Add(c);

        var formati = await _formatiRepo.GetAllAsync();
        foreach (var f in formati) FormatiMacchine.Add(f);

        // Popola i valori disponibili nei filtri enum
        FiltroCategoria.ValoriEnum.AddRange(CategoriePiastre.Select(c => c.Descrizione));
        FiltroFormato.ValoriEnum.AddRange(FormatiMacchine.Select(f => f.NomeFormato));
        FiltroTipo.ValoriEnum.AddRange(Enum.GetNames<TipoPiastra>());
        FiltroStato.ValoriEnum.AddRange(Enum.GetNames<StatoPiastra>());

        _tuttiClienti = (await _clientiRepo.GetAllAsync()).ToList();

        var piastre = await _piastreRepo.GetAllAsync();
        foreach (var p in piastre) _tutti.Add(p);
        AggiornaFiltro();
    }

    private async Task LoadDettaglioAsync()
    {
        MacchineCompatibili.Clear();
        ClientiAssociati.Clear();
        DisegnoCorrente = null;

        if (PiastraSelezionata is null) return;

        var id = PiastraSelezionata.IdPiastra;
        var macchine = await _compatRepo.GetByPiastraAsync(id);
        var clienti  = await _clientiPiastreRepo.GetByPiastraAsync(id);
        var disegno  = await _disegniRepo.GetByPiastraAsync(id);

        foreach (var m in macchine) MacchineCompatibili.Add(m);
        foreach (var c in clienti)  ClientiAssociati.Add(c);
        DisegnoCorrente = disegno;
    }

    // ─── Filtro lista ─────────────────────────────────────────────────────────

    private void AggiornaFiltro()
    {
        var f = FiltroRicerca.Trim().ToLower();

        PiastreFiltrate.Clear();
        foreach (var p in _tutti.Where(p =>
            // Ricerca testo globale (barra di ricerca in cima)
            (string.IsNullOrEmpty(f)
                || p.CodicePiastra.ToLower().Contains(f)
                || (p.CodiceArticoloGestionale?.ToLower().Contains(f) ?? false)
                || (p.Descrizione?.ToLower().Contains(f) ?? false))
            // Filtri per colonna
            && FiltroCodice.ApplicaA(p.CodicePiastra)
            && FiltroDescrizione.ApplicaA(p.Descrizione)
            && FiltroArtGestionale.ApplicaA(p.CodiceArticoloGestionale)
            && FiltroCategoria.ApplicaA(p.Categoria?.Descrizione)
            && FiltroFormato.ApplicaA(p.Formato?.NomeFormato)
            && FiltroTipo.ApplicaA(p.TipoPiastra.ToString())
            && FiltroStato.ApplicaA(p.Stato.ToString())
            && FiltroLarghezza.ApplicaA(p.LarghezzaMm.HasValue
                ? p.LarghezzaMm.Value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) : null)
            && FiltroAltezza.ApplicaA(p.AltezzaMm.HasValue
                ? p.AltezzaMm.Value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) : null)
            && FiltroSpessore.ApplicaA(p.SpessoreMm.HasValue
                ? p.SpessoreMm.Value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) : null)
            && FiltroDurezza.ApplicaA(p.Durezza.HasValue
                ? p.Durezza.Value.ToString("F1", System.Globalization.CultureInfo.InvariantCulture) : null)
            && FiltroPeso.ApplicaA(p.Peso.HasValue
                ? p.Peso.Value.ToString("F3", System.Globalization.CultureInfo.InvariantCulture) : null)
            && FiltroDataCreazione.ApplicaA(p.DataCreazione.ToString("yyyy-MM-dd"))
            && FiltroDataModifica.ApplicaA(p.DataUltimaModifica.ToString("yyyy-MM-dd"))))
        {
            PiastreFiltrate.Add(p);
        }
    }

    // ─── Validazione duplicato codice ─────────────────────────────────────────

    private void ControllaDuplicato(string codice)
    {
        if (string.IsNullOrWhiteSpace(codice)) { ErroreCodiceDuplicato = null; return; }
        var simile = _tutti.FirstOrDefault(p =>
            p.CodicePiastra.Equals(codice.Trim(), StringComparison.OrdinalIgnoreCase)
            && p.IdPiastra != _idPiastraInModifica);
        ErroreCodiceDuplicato = simile is not null
            ? $"Codice '{simile.CodicePiastra}' già presente."
            : null;
    }

    // ─── Gestione form creazione/modifica ─────────────────────────────────────

    private void ApriFormNuova()
    {
        _idPiastraInModifica = 0;
        IsModifica    = false;
        DisegnoCorrente = null;
        ResetForm();
        IsFormVisible = true;
    }

    private void ApriFormModifica()
    {
        if (PiastraSelezionata is null) return;
        _idPiastraInModifica     = PiastraSelezionata.IdPiastra;
        FormCodicePiastra        = PiastraSelezionata.CodicePiastra;
        FormCodiceArticolo       = PiastraSelezionata.CodiceArticoloGestionale  ?? string.Empty;
        FormDescrizione          = PiastraSelezionata.Descrizione                ?? string.Empty;
        FormStato                = PiastraSelezionata.Stato;
        FormTipo                 = PiastraSelezionata.TipoPiastra;
        FormCategoriaSelezionata = CategoriePiastre.FirstOrDefault(c => c.IdCategoriaPiastra == PiastraSelezionata.IdCategoriaPiastra);
        FormFormatoSelezionato   = FormatiMacchine.FirstOrDefault(f => f.IdFormato == PiastraSelezionata.IdFormato);
        FormLarghezza            = PiastraSelezionata.LarghezzaMm?.ToString("F1")  ?? string.Empty;
        FormAltezza              = PiastraSelezionata.AltezzaMm?.ToString("F1")    ?? string.Empty;
        FormSpessore             = PiastraSelezionata.SpessoreMm?.ToString("F2")   ?? string.Empty;
        FormDurezza              = PiastraSelezionata.Durezza?.ToString("F1")      ?? string.Empty;
        FormPeso                 = PiastraSelezionata.Peso?.ToString("F3")         ?? string.Empty;
        FormNote                 = PiastraSelezionata.Note                          ?? string.Empty;
        FormClienteEsclusivo     = PiastraSelezionata.IdClienteEsclusivo.HasValue
            ? _tuttiClienti.FirstOrDefault(c => c.IdCliente == PiastraSelezionata.IdClienteEsclusivo)
            : null;
        ErroreCodiceDuplicato    = null;
        IsModifica    = true;
        IsFormVisible = true;
    }

    private void ChiudiForm()
    {
        IsFormVisible = false;
        ResetForm();
        if (PiastraSelezionata is not null)
            _loadDettaglioTask = LoadDettaglioAsync();
    }

    private void ResetForm()
    {
        FormCodicePiastra = FormCodiceArticolo = FormDescrizione = FormNote = string.Empty;
        FormLarghezza = FormAltezza = FormSpessore = FormDurezza = FormPeso = string.Empty;
        FormStato                = StatoPiastra.Attiva;
        FormTipo                 = TipoPiastra.Standard;
        FormCategoriaSelezionata = CategoriePiastre.FirstOrDefault(c => c.Codice == "STD");
        FormFormatoSelezionato   = null;
        FormClienteEsclusivo     = null;
        ErroreCodiceDuplicato    = null;
        PercorsoDisegnoPendente  = null;
        _filtroClienteEsclusivo         = string.Empty;
        _formFiltroClienteAssociato     = string.Empty;
        OnPropertyChanged(nameof(FiltroClienteEsclusivo));
        OnPropertyChanged(nameof(FormFiltroClienteAssociato));
        ClientiEsclusiviSuggeriti.Clear();
        FormClientiDaAssociare.Clear();
        FormClientiAssociatiSuggeriti.Clear();
        OnPropertyChanged(nameof(IsClientiEsclusiviSuggerimentiVisible));
        OnPropertyChanged(nameof(IsFormClientiAssociatiSuggerimentiVisible));
    }

    private async Task SalvaAsync()
    {
        if (string.IsNullOrWhiteSpace(FormCodicePiastra)) return;
        if (IsErroreVisible) return;
        if (FormTipo == TipoPiastra.SpecialeCliente && FormClienteEsclusivo is null)
        {
            MessageBox.Show(
                "Per una piastra speciale cliente è necessario selezionare il cliente.",
                "Cliente mancante",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        Piastra piastraSalvata;
        if (IsModifica)
        {
            var p = _tutti.FirstOrDefault(x => x.IdPiastra == _idPiastraInModifica);
            if (p is null) return;
            p.CodicePiastra            = FormCodicePiastra.Trim();
            p.CodiceArticoloGestionale = N(FormCodiceArticolo);
            p.Descrizione              = N(FormDescrizione);
            p.Stato                    = FormStato;
            p.TipoPiastra              = FormTipo;
            p.IdClienteEsclusivo       = FormClienteEsclusivo?.IdCliente;
            p.ClienteEsclusivo         = FormClienteEsclusivo;
            p.IdCategoriaPiastra       = FormCategoriaSelezionata?.IdCategoriaPiastra;
            p.Categoria                = FormCategoriaSelezionata;
            p.IdFormato                = FormFormatoSelezionato?.IdFormato;
            p.Formato                  = FormFormatoSelezionato;
            p.LarghezzaMm              = ParseDecimal(FormLarghezza);
            p.AltezzaMm                = ParseDecimal(FormAltezza);
            p.SpessoreMm               = ParseDecimal(FormSpessore);
            p.Durezza                  = ParseDecimal(FormDurezza);
            p.Peso                     = ParseDecimal(FormPeso);
            p.Note                     = N(FormNote);
            await _piastreRepo.UpdateAsync(p);
            piastraSalvata = p;
        }
        else
        {
            var nuova = new Piastra
            {
                CodicePiastra            = FormCodicePiastra.Trim(),
                CodiceArticoloGestionale = N(FormCodiceArticolo),
                Descrizione              = N(FormDescrizione),
                Stato                    = FormStato,
                TipoPiastra              = FormTipo,
                IdClienteEsclusivo       = FormClienteEsclusivo?.IdCliente,
                ClienteEsclusivo         = FormClienteEsclusivo,
                IdCategoriaPiastra       = FormCategoriaSelezionata?.IdCategoriaPiastra,
                Categoria                = FormCategoriaSelezionata,
                IdFormato                = FormFormatoSelezionato?.IdFormato,
                Formato                  = FormFormatoSelezionato,
                LarghezzaMm              = ParseDecimal(FormLarghezza),
                AltezzaMm                = ParseDecimal(FormAltezza),
                SpessoreMm               = ParseDecimal(FormSpessore),
                Durezza                  = ParseDecimal(FormDurezza),
                Peso                     = ParseDecimal(FormPeso),
                Note                     = N(FormNote)
            };
            await _piastreRepo.AddAsync(nuova);
            _tutti.Add(nuova);
            piastraSalvata = nuova;
        }

        var filePendente = _percorsoDisegnoPendente;
        if (!string.IsNullOrEmpty(filePendente))
            await AssociaDisegnoAsync(piastraSalvata, filePendente);

        // Associa i clienti selezionati nel form (solo in creazione; in modifica si usa il pannello dettaglio)
        if (!IsModifica)
        {
            var clientiDaAssociare = FormClientiDaAssociare.ToList();
            // Per SpecialeCliente, il cliente esclusivo viene incluso anche come associazione
            if (FormTipo == TipoPiastra.SpecialeCliente && FormClienteEsclusivo is not null
                && !clientiDaAssociare.Contains(FormClienteEsclusivo))
                clientiDaAssociare.Add(FormClienteEsclusivo);

            foreach (var cliente in clientiDaAssociare)
            {
                var cp = new ClientePiastra
                {
                    IdCliente        = cliente.IdCliente,
                    IdPiastra        = piastraSalvata.IdPiastra,
                    DataAssociazione = DateTime.UtcNow,
                    Stato            = Core.Enums.StatoClientePiastra.Attiva,
                    Cliente          = cliente,
                    Piastra          = piastraSalvata
                };
                await _clientiPiastreRepo.AddAsync(cp);
            }
        }

        AggiornaFiltro();
        ChiudiForm();
        PiastraSelezionata = piastraSalvata;
    }

    // ─── Eliminazione logica ──────────────────────────────────────────────────

    private async Task EliminaAsync()
    {
        if (PiastraSelezionata is null) return;

        var hasClienti = await _piastreRepo.HasClientiAssociatiAsync(PiastraSelezionata.IdPiastra);
        if (hasClienti)
        {
            MessageBox.Show(
                $"Impossibile eliminare '{PiastraSelezionata.CodicePiastra}':\nè associata ad almeno un cliente.",
                "Eliminazione non consentita",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var hasMacchine = MacchineCompatibili.Count > 0;
        var testo = hasMacchine
            ? $"La piastra '{PiastraSelezionata.CodicePiastra}' è associata a {MacchineCompatibili.Count} macchina/e compatibile/i.\n\nEliminarla comunque?"
            : $"Eliminare la piastra '{PiastraSelezionata.CodicePiastra}'?";

        var conferma = MessageBox.Show(
            testo,
            "Conferma eliminazione",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (conferma != MessageBoxResult.Yes) return;

        await _piastreRepo.EliminaLogicamenteAsync(PiastraSelezionata.IdPiastra);
        _tutti.Remove(PiastraSelezionata);
        PiastraSelezionata = null;
        AggiornaFiltro();
    }

    // ─── Aggiungi / rimuovi macchina compatibile ──────────────────────────────

    private async Task ApriAggiungiMacchinaAsync()
    {
        var tutte       = await _macchineRepo.GetAllAsync();
        var idGiaCompat = MacchineCompatibili.Select(c => c.IdMacchinaStandard).ToHashSet();
        var idFormatoPiastra = PiastraSelezionata?.IdFormato;

        MacchineDisponibili.Clear();
        foreach (var m in tutte.Where(m =>
            m.Attiva
            && !idGiaCompat.Contains(m.IdMacchinaStandard)
            && (idFormatoPiastra is null || m.IdFormato == idFormatoPiastra)))
        {
            MacchineDisponibili.Add(m);
        }
        MacchinaCompatibileDaAggiungere = null;
        IsAggiungiMacchinaVisible = true;
    }

    private async Task ConfermaAggiungiMacchinaAsync()
    {
        if (PiastraSelezionata is null || MacchinaCompatibileDaAggiungere is null) return;
        var nuova = new PiastraMacchinaCompatibile
        {
            IdPiastra          = PiastraSelezionata.IdPiastra,
            IdMacchinaStandard = MacchinaCompatibileDaAggiungere.IdMacchinaStandard,
            Attiva             = true
        };
        await _compatRepo.AddAsync(nuova);
        ChiudiAggiungiMacchina();
        await LoadDettaglioAsync();
    }

    private void ChiudiAggiungiMacchina()
    {
        IsAggiungiMacchinaVisible       = false;
        MacchinaCompatibileDaAggiungere = null;
    }

    private async Task RimuoviCompatibilitaAsync(object? param)
    {
        if (param is not PiastraMacchinaCompatibile c) return;

        var conferma = MessageBox.Show(
            $"Rimuovere la compatibilità con '{c.MacchinaStandard?.NomeMacchina}'?",
            "Conferma rimozione",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (conferma != MessageBoxResult.Yes) return;

        await _compatRepo.DeleteAsync(c.IdCompatibilita);
        await LoadDettaglioAsync();
    }

    // ─── Gestione disegno (1:1) ───────────────────────────────────────────────

    private void AprirDisegno()
    {
        var percorso = DisegnoCorrente?.PercorsoFile;
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

    /// <summary>
    /// Associa un file alla piastra (1:1).
    /// Se la piastra ha già un disegno, sovrascrive il percorso del record esistente.
    /// Se è nuovo, archivia il file nella cartella corretta e crea il record Disegno.
    /// </summary>
    public async Task AssociaDisegnoAsync(Piastra piastra, string percorsoFile)
    {
        await _loadDettaglioTask;

        var disegnoEsistente = await _disegniRepo.GetByPiastraAsync(piastra.IdPiastra);

        var codiceCliente = piastra.IdClienteEsclusivo.HasValue
            ? _tuttiClienti.FirstOrDefault(c => c.IdCliente == piastra.IdClienteEsclusivo)?.CodiceClienteGestionale
            : null;

        var percorsoEffettivo = await _fileArchivio.ArchiviaDisegnoAsync(
            percorsoFile, piastra.CodicePiastra, piastra.TipoPiastra, codiceCliente)
            ?? percorsoFile;

        var formato = Path.GetExtension(percorsoEffettivo).TrimStart('.').ToUpper();

        if (disegnoEsistente is not null)
        {
            disegnoEsistente.NomeFile               = Path.GetFileName(percorsoEffettivo);
            disegnoEsistente.PercorsoFile           = percorsoEffettivo;
            disegnoEsistente.Formato                = formato;
            disegnoEsistente.Stato                  = StatoDisegno.DaVerificare;
            disegnoEsistente.DataUltimaModificaFile = DateTime.UtcNow;
            await _disegniRepo.UpdateAsync(disegnoEsistente);
        }
        else
        {
            var nuovoDisegno = new Disegno
            {
                IdPiastra              = piastra.IdPiastra,
                CodiceDisegno          = piastra.CodicePiastra,
                NomeFile               = Path.GetFileName(percorsoEffettivo),
                PercorsoFile           = percorsoEffettivo,
                Formato                = formato,
                Stato                  = StatoDisegno.Attivo,
                DataUltimaModificaFile = DateTime.UtcNow
            };
            await _disegniRepo.AddAsync(nuovoDisegno);
        }

        if (PiastraSelezionata == piastra)
            await LoadDettaglioAsync();
    }

    private async Task RimuoviDisegnoAsync()
    {
        if (DisegnoCorrente is null) return;

        var conferma = MessageBox.Show(
            $"Rimuovere il disegno '{DisegnoCorrente.NomeFile}' da questa piastra?\n\nIl file fisico non verrà eliminato.",
            "Conferma rimozione",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (conferma != MessageBoxResult.Yes) return;

        await _disegniRepo.DeleteAsync(DisegnoCorrente.IdDisegno);
        await LoadDettaglioAsync();
    }

    // ─── Aggiungi / rimuovi cliente associato ─────────────────────────────────

    private async Task ApriAggiungiClienteAsync()
    {
        var tutti          = await _clientiRepo.GetAllAsync();
        var idGiaAssociati = ClientiAssociati.Select(cp => cp.IdCliente).ToHashSet();

        _tuttiClientiDisponibili = tutti.Where(c => !idGiaAssociati.Contains(c.IdCliente)).ToList();

        ClienteSelezionato       = null;
        _filtroCliente           = string.Empty;
        OnPropertyChanged(nameof(FiltroCliente));
        ClientiSuggeriti.Clear();
        OnPropertyChanged(nameof(IsClienteSuggerimentiVisible));
        IsAggiungiClienteVisible = true;
    }

    private async Task ConfermaAggiungiClienteAsync()
    {
        if (PiastraSelezionata is null || ClienteSelezionato is null) return;

        var esiste = await _clientiPiastreRepo.ExistsAsync(ClienteSelezionato.IdCliente, PiastraSelezionata.IdPiastra);
        if (esiste) { ChiudiAggiungiCliente(); return; }

        var nuova = new ClientePiastra
        {
            IdCliente         = ClienteSelezionato.IdCliente,
            IdPiastra         = PiastraSelezionata.IdPiastra,
            DataAssociazione  = DateTime.UtcNow,
            Stato             = Core.Enums.StatoClientePiastra.Attiva,
            Cliente           = ClienteSelezionato,
            Piastra           = PiastraSelezionata
        };
        await _clientiPiastreRepo.AddAsync(nuova);
        ChiudiAggiungiCliente();
        await LoadDettaglioAsync();
    }

    private void ChiudiAggiungiCliente()
    {
        IsAggiungiClienteVisible = false;
        ClienteSelezionato       = null;
        _filtroCliente           = string.Empty;
        OnPropertyChanged(nameof(FiltroCliente));
        ClientiSuggeriti.Clear();
        OnPropertyChanged(nameof(IsClienteSuggerimentiVisible));
        _tuttiClientiDisponibili = [];
    }

    private async Task RimuoviClienteAsync(object? param)
    {
        if (param is not ClientePiastra cp) return;

        var conferma = MessageBox.Show(
            $"Rimuovere l'associazione con '{cp.Cliente?.RagioneSociale}'?",
            "Conferma rimozione",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (conferma != MessageBoxResult.Yes) return;

        await _clientiPiastreRepo.DeleteAsync(cp.IdClientePiastra);
        await LoadDettaglioAsync();
    }

    // ─── Typeahead cliente associato ──────────────────────────────────────────

    private void AggiornaClientiSuggeriti()
    {
        ClientiSuggeriti.Clear();
        var f = _filtroCliente.Trim().ToLower();
        if (!string.IsNullOrEmpty(f))
        {
            foreach (var c in _tuttiClientiDisponibili
                .Where(c => c.CodiceClienteGestionale.ToLower().Contains(f)
                         || c.RagioneSociale.ToLower().Contains(f))
                .Take(8))
                ClientiSuggeriti.Add(c);
        }
        OnPropertyChanged(nameof(IsClienteSuggerimentiVisible));
    }

    // ─── Typeahead cliente esclusivo (nel form) ───────────────────────────────

    private void AggiornaClientiEsclusiviSuggeriti()
    {
        ClientiEsclusiviSuggeriti.Clear();
        var f = _filtroClienteEsclusivo.Trim().ToLower();
        if (!string.IsNullOrEmpty(f))
        {
            foreach (var c in _tuttiClienti
                .Where(c => c.CodiceClienteGestionale.ToLower().Contains(f)
                         || c.RagioneSociale.ToLower().Contains(f))
                .Take(8))
                ClientiEsclusiviSuggeriti.Add(c);
        }
        OnPropertyChanged(nameof(IsClientiEsclusiviSuggerimentiVisible));
    }

    // ─── Typeahead clienti da associare nel form (Standard) ──────────────────

    private void AggiornaFormClientiAssociatiSuggeriti()
    {
        FormClientiAssociatiSuggeriti.Clear();
        var f = _formFiltroClienteAssociato.Trim().ToLower();
        if (!string.IsNullOrEmpty(f))
        {
            var idGiaAggiunti = FormClientiDaAssociare.Select(c => c.IdCliente).ToHashSet();
            foreach (var c in _tuttiClienti
                .Where(c => !idGiaAggiunti.Contains(c.IdCliente)
                         && (c.CodiceClienteGestionale.ToLower().Contains(f)
                          || c.RagioneSociale.ToLower().Contains(f)))
                .Take(8))
                FormClientiAssociatiSuggeriti.Add(c);
        }
        OnPropertyChanged(nameof(IsFormClientiAssociatiSuggerimentiVisible));
    }

    private void AggiungiClienteAssociatoForm(Cliente cliente)
    {
        if (FormClientiDaAssociare.Contains(cliente)) return;
        FormClientiDaAssociare.Add(cliente);
        _formFiltroClienteAssociato = string.Empty;
        OnPropertyChanged(nameof(FormFiltroClienteAssociato));
        FormClientiAssociatiSuggeriti.Clear();
        OnPropertyChanged(nameof(IsFormClientiAssociatiSuggerimentiVisible));
    }

    private void RimuoviClienteAssociatoForm(Cliente cliente)
    {
        FormClientiDaAssociare.Remove(cliente);
    }

    // ─── Sfoglia file nel form ────────────────────────────────────────────────

    private void SfogliaFileForm()
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Seleziona file disegno",
            Filter = "File disegno|*.dwg;*.dxf;*.pdf;*.stp;*.step;*.igs|Tutti i file|*.*"
        };
        if (dlg.ShowDialog() == true)
            PercorsoDisegnoPendente = dlg.FileName;
    }

    private async Task SfogliaFileDettaglioAsync()
    {
        if (PiastraSelezionata is null) return;
        var dlg = new OpenFileDialog
        {
            Title  = "Seleziona file disegno",
            Filter = "File disegno|*.dwg;*.dxf;*.pdf;*.stp;*.step;*.igs|Tutti i file|*.*"
        };
        if (dlg.ShowDialog() == true)
            await AssociaDisegnoAsync(PiastraSelezionata, dlg.FileName);
    }

    // ─── Utility ─────────────────────────────────────────────────────────────

    private static string?  N(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static decimal? ParseDecimal(string s) =>
        decimal.TryParse(s.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
}
