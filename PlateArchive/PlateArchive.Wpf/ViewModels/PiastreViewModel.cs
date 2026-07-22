using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PlateArchive.Core.Enums;
using PlateArchive.Core.Models;
using PlateArchive.Core.Servizi;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Services;
using PlateArchive.Wpf.Commands;
using PlateArchive.Wpf.Services;

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
    private readonly IDurezzaStandardRepository  _durezzaRepo;

    private readonly ObservableCollection<Piastra> _tutti = [];
    private List<Cliente> _tuttiClienti = [];

    // Pre-impostazione cliente (da ClienteDettaglioView → "Nuova piastra")
    private Cliente? _clientePreimpostato;
    public  Action?  DopoCreazionePiastra { get; set; }

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
    private CategoriaPiastra?  _formCategoriaSelezionata;
    private FormatoMacchina?   _formFormatoSelezionato;
    private string             _formLarghezza            = string.Empty;
    private string             _formAltezza              = string.Empty;
    private decimal?           _formSpessore;
    private DurezzaStandard?   _formDurezzaSelezionata;
    private string             _formPeso                 = string.Empty;
    private string             _formNote                 = string.Empty;
    private string?            _erroreCodiceDuplicato;
    private string?            _erroreDisegno;
    private string?            _percorsoDisegnoPendente;
    private bool                _isCodicePiastraNonValido;
    private bool                _isClienteEsclusivoNonValido;

    // ── Form: metadati disegno associato (modifica in-place nel dettaglio) ────
    private string       _formRevisioneDisegno = string.Empty;
    private string       _formFormatoDisegno   = string.Empty;
    private StatoDisegno _formStatoDisegno     = StatoDisegno.DaVerificare;
    private string       _formNoteDisegno      = string.Empty;

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

    // ── Form: macchine compatibili da associare in creazione (stile tabella) ──
    private bool              _isFormAggiungiMacchinaVisible;
    private MacchinaStandard? _formMacchinaDaAggiungere;

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
        IClienteRepository          clientiRepo,
        IDurezzaStandardRepository  durezzaRepo,
        IArticoliGestionaleService  articoliService)
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
        _durezzaRepo        = durezzaRepo;

        SelettoreArticolo = new SelettoreArticoloGestionaleViewModel(articoliService);
        SelettoreArticolo.ArticoloScelto += ProponiFormatoDaArticolo;

        // Registra tutti i filtri colonna → riesegui AggiornaFiltro al cambio
        foreach (var f in new[] {
            FiltroCodice, FiltroDescrizione, FiltroArtGestionale,
            FiltroCategoria, FiltroFormato, FiltroTipo, FiltroStato,
            FiltroLarghezza, FiltroAltezza, FiltroSpessore, FiltroDurezza, FiltroPeso,
            FiltroDataCreazione, FiltroDataModifica })
        {
            f.Cambiato += AggiornaFiltro;
        }

        NuovaCommand                    = new RelayCommand(async _ => await ApriFormNuovaAsync());
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
        SalvaDisegnoCommand             = new RelayCommand(async _ => await SalvaDisegnoAsync(), _ => DisegnoCorrente is not null);
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
        AggiungiMacchinaFormCommand             = new RelayCommand(async _ => await ApriFormAggiungiMacchinaAsync());
        ConfermaAggiungiMacchinaFormCommand     = new RelayCommand(_ => ConfermaFormAggiungiMacchina(), _ => FormMacchinaDaAggiungere is not null);
        AnnullaAggiungiMacchinaFormCommand      = new RelayCommand(_ => ChiudiFormAggiungiMacchina());
        RimuoviMacchinaFormCommand              = new RelayCommand(p => RimuoviMacchinaForm((MacchinaStandard)p!));
        SfogliaFileDettaglioCommand             = new RelayCommand(async _ => await SfogliaFileDettaglioAsync(), _ => PiastraSelezionata is not null && DisegnoCorrente is null);
    }

    // ─── Lookup per i ComboBox del form ──────────────────────────────────────

    public ObservableCollection<CategoriaPiastra> CategoriePiastre { get; } = [];
    public ObservableCollection<FormatoMacchina>  FormatiMacchine  { get; } = [];
    public ObservableCollection<DurezzaStandard>  DurezzePiastre   { get; } = [];

    /// <summary>Selettore articolo gestionale reale (TASK-19), mostrato al posto del TextBox
    /// libero quando la categoria è Speciale Cliente.</summary>
    public SelettoreArticoloGestionaleViewModel SelettoreArticolo { get; }

    /// <summary>Alla scelta di un articolo reale propone il formato derivato dalle pos 7-10
    /// del codice, solo se il campo formato è ancora vuoto (spessore e durezza non si toccano:
    /// la stessa piastra serve più articoli con spessori/durezze diversi).</summary>
    private void ProponiFormatoDaArticolo(ArticoloGestionale articolo)
    {
        SelettoreArticolo.Avviso = null;
        if (FormFormatoSelezionato is not null) return;
        if (!CodiceArticoloPanthera.TryEstraiFormato(articolo.Codice, out var formato) || formato == 0) return;

        var corrispondente = FormatiMacchine
            .FirstOrDefault(f => CodiceArticoloPanthera.FormatoCompatibile(formato, f.NomeFormato));
        if (corrispondente is not null)
            FormFormatoSelezionato = corrispondente;
        else
            SelettoreArticolo.Avviso =
                $"Il formato {formato:0.#} del codice scelto non è tra i Formati macchina: " +
                "crearlo in Impostazioni e impostarlo sulla piastra per il match negli ordini.";
    }

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
    public FiltroColonna FiltroStato         { get; } = new("Stato piastra",   FiltroColonnaTipo.Enum);
    public FiltroColonna FiltroLarghezza     { get; } = new("Larghezza",       FiltroColonnaTipo.Numerico);
    public FiltroColonna FiltroAltezza       { get; } = new("Altezza",         FiltroColonnaTipo.Numerico);
    public FiltroColonna FiltroSpessore      { get; } = new("Spessore",        FiltroColonnaTipo.Numerico);
    public FiltroColonna FiltroDurezza       { get; } = new("Durezza",         FiltroColonnaTipo.Testo);
    public FiltroColonna FiltroPeso          { get; } = new("Peso",            FiltroColonnaTipo.Numerico);
    public FiltroColonna FiltroDataCreazione { get; } = new("Data creazione",  FiltroColonnaTipo.Data);
    public FiltroColonna FiltroDataModifica  { get; } = new("Ultima modifica", FiltroColonnaTipo.Data);

    public IEnumerable<StatoPiastra> StatiPiastra { get; } = Enum.GetValues<StatoPiastra>();

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

    private Disegno?       _disegnoCorrente;
    private BitmapSource?  _anteprimaDisegno;
    private bool           _isCaricamentoAnteprima;
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
                OnPropertyChanged(nameof(IsNoAnteprima));
            }
        }
    }

    public bool IsDisegnoPresente     => DisegnoCorrente is not null;
    public bool IsDisegnoAssente      => DisegnoCorrente is null;
    // Nel form: in modifica mostra il disegno esistente; in creazione mostra sempre la drop zone
    public bool IsFormDisegnoPresente => IsModifica && DisegnoCorrente is not null;
    public bool IsFormDisegnoAssente  => !IsModifica || DisegnoCorrente is null;

    public BitmapSource? AnteprimaDisegno
    {
        get => _anteprimaDisegno;
        set
        {
            if (SetField(ref _anteprimaDisegno, value))
            {
                OnPropertyChanged(nameof(IsAnteprimaVisible));
                OnPropertyChanged(nameof(IsNoAnteprima));
            }
        }
    }
    public bool IsAnteprimaVisible => _anteprimaDisegno is not null;

    public bool IsCaricamentoAnteprima
    {
        get => _isCaricamentoAnteprima;
        private set { if (SetField(ref _isCaricamentoAnteprima, value)) OnPropertyChanged(nameof(IsNoAnteprima)); }
    }
    public bool IsNoAnteprima => !IsAnteprimaVisible && !IsCaricamentoAnteprima && DisegnoCorrente is not null;

    // ─── Metadati disegno (modifica in-place nel dettaglio) ───────────────────

    public string FormRevisioneDisegno
    {
        get => _formRevisioneDisegno;
        set => SetField(ref _formRevisioneDisegno, value);
    }

    public string FormFormatoDisegno
    {
        get => _formFormatoDisegno;
        set => SetField(ref _formFormatoDisegno, value);
    }

    public StatoDisegno FormStatoDisegno
    {
        get => _formStatoDisegno;
        set => SetField(ref _formStatoDisegno, value);
    }

    public string FormNoteDisegno
    {
        get => _formNoteDisegno;
        set => SetField(ref _formNoteDisegno, value);
    }

    public IEnumerable<StatoDisegno> StatiDisegno       { get; } = Enum.GetValues<StatoDisegno>();
    public IEnumerable<string>       FormatiDisponibili { get; } = ["DWG", "DXF", "PDF", "STP", "STEP", "IGS"];

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
                OnPropertyChanged(nameof(IsCodicePiastraReadOnly));
            }
        }
    }

    /// <summary>True in creazione (codice auto-generato, non modificabile); false in modifica.</summary>
    public bool IsCodicePiastraReadOnly => !IsModifica;

    public bool   IsDetailVisible     => PiastraSelezionata is not null && !IsFormVisible;
    public bool   IsPannelloDxVisible => IsDetailVisible || IsFormVisible;
    public string FormTitolo          => IsModifica ? "Modifica piastra" : "Nuova piastra";

    // ─── Campi form creazione/modifica ────────────────────────────────────────

    public string FormCodicePiastra
    {
        get => _formCodicePiastra;
        set
        {
            if (SetField(ref _formCodicePiastra, value))
            {
                ControllaDuplicato(value);
                if (IsCodicePiastraNonValido && !string.IsNullOrWhiteSpace(value))
                    IsCodicePiastraNonValido = false;
            }
        }
    }

    /// <summary>True quando "Salva" è stato premuto senza aver compilato il codice piastra.</summary>
    public bool IsCodicePiastraNonValido
    {
        get => _isCodicePiastraNonValido;
        set => SetField(ref _isCodicePiastraNonValido, value);
    }

    /// <summary>True quando "Salva" è stato premuto senza aver selezionato il cliente esclusivo
    /// richiesto per le piastre di tipo SpecialeCliente.</summary>
    public bool IsClienteEsclusivoNonValido
    {
        get => _isClienteEsclusivoNonValido;
        set => SetField(ref _isClienteEsclusivoNonValido, value);
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

    /// <summary>
    /// True quando la categoria selezionata è "Speciale Cliente" (Codice "SPE").
    /// In tal caso il cliente esclusivo diventa obbligatorio e la piastra viene salvata
    /// come <see cref="TipoPiastra.SpecialeCliente"/>. Tipo e categoria coincidono:
    /// la categoria SPE è l'unico driver (nessun dropdown "Tipo piastra" separato).
    /// </summary>
    public bool IsSpecialeCliente => FormCategoriaSelezionata?.Codice == "SPE";

    /// <summary>True se la sezione "cliente esclusivo" deve essere visibile nel form.</summary>
    public bool IsClienteEsclusivoVisible    => IsSpecialeCliente;
    /// <summary>True se la sezione "associa a clienti" (opzionale) deve essere visibile nel form.</summary>
    public bool IsAssociazioneClientiVisible => !IsSpecialeCliente;

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
                    IsClienteEsclusivoNonValido = false;
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

    // ── Form: macchine compatibili da associare in creazione (stile tabella) ──

    /// <summary>Macchine già selezionate come compatibili nel form (persistite al salvataggio).</summary>
    public ObservableCollection<MacchinaStandard> FormMacchineDaAssociare { get; } = [];

    /// <summary>Macchine disponibili nel ComboBox inline (attive, non ancora aggiunte).</summary>
    public ObservableCollection<MacchinaStandard> FormMacchineDisponibili { get; } = [];

    public bool IsFormAggiungiMacchinaVisible
    {
        get => _isFormAggiungiMacchinaVisible;
        set => SetField(ref _isFormAggiungiMacchinaVisible, value);
    }

    public MacchinaStandard? FormMacchinaDaAggiungere
    {
        get => _formMacchinaDaAggiungere;
        set => SetField(ref _formMacchinaDaAggiungere, value);
    }

    public CategoriaPiastra? FormCategoriaSelezionata
    {
        get => _formCategoriaSelezionata;
        set
        {
            if (SetField(ref _formCategoriaSelezionata, value))
            {
                // Uscendo dalla categoria "Speciale Cliente" azzera il cliente esclusivo.
                if (!IsSpecialeCliente)
                {
                    FormClienteEsclusivo        = null;
                    IsClienteEsclusivoNonValido = false;
                }
                OnPropertyChanged(nameof(IsSpecialeCliente));
                OnPropertyChanged(nameof(IsClienteEsclusivoVisible));
                OnPropertyChanged(nameof(IsAssociazioneClientiVisible));
            }
        }
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

    public decimal? FormSpessore
    {
        get => _formSpessore;
        set => SetField(ref _formSpessore, value);
    }

    public DurezzaStandard? FormDurezzaSelezionata
    {
        get => _formDurezzaSelezionata;
        set => SetField(ref _formDurezzaSelezionata, value);
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
    public ICommand SalvaDisegnoCommand             { get; }
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
    public ICommand AggiungiMacchinaFormCommand             { get; }
    public ICommand ConfermaAggiungiMacchinaFormCommand     { get; }
    public ICommand AnnullaAggiungiMacchinaFormCommand      { get; }
    public ICommand RimuoviMacchinaFormCommand              { get; }
    public ICommand SfogliaFileDettaglioCommand             { get; }

    // ─── Inizializzazione navigazione ─────────────────────────────────────────

    /// <summary>
    /// Chiamato da ClienteDettaglioViewModel prima della navigazione per aprire
    /// il form "Nuova piastra" pre-compilato con categoria SPE e cliente esclusivo.
    /// </summary>
    public void PreimpostaClienteNuovaPiastra(Cliente c) => _clientePreimpostato = c;

    public override async Task OnNavigatedAsync()
    {
        await LoadAsync();

        if (_clientePreimpostato is not null)
        {
            await ApriFormNuovaAsync();
            FormCategoriaSelezionata = CategoriePiastre.FirstOrDefault(c => c.Codice == "SPE");
            FormClienteEsclusivo     = _clientePreimpostato;
        }
    }

    // ─── Caricamento ─────────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        var categorie = await _categorieRepo.GetAllAsync();
        foreach (var c in categorie) CategoriePiastre.Add(c);

        var formati = await _formatiRepo.GetAllAsync();
        foreach (var f in formati) FormatiMacchine.Add(f);

        var durezze = await _durezzaRepo.GetAllAsync();
        foreach (var d in durezze) DurezzePiastre.Add(d);

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
        DisegnoCorrente  = null;
        AnteprimaDisegno = null;

        if (PiastraSelezionata is null) return;

        var id = PiastraSelezionata.IdPiastra;
        var macchine = await _compatRepo.GetByPiastraAsync(id);
        var clienti  = await _clientiPiastreRepo.GetByPiastraAsync(id);
        var disegno  = await _disegniRepo.GetByPiastraAsync(id);

        foreach (var m in macchine) MacchineCompatibili.Add(m);
        foreach (var c in clienti)  ClientiAssociati.Add(c);
        DisegnoCorrente = disegno;
        CaricaFormDisegno();

        if (disegno is not null)
        {
            IsCaricamentoAnteprima = true;
            try   { AnteprimaDisegno = await DwgThumbnailReader.EstraiAnteprimaAsync(disegno.PercorsoFile); }
            finally { IsCaricamentoAnteprima = false; }
        }
    }

    /// <summary>Ricarica l'elenco piastre dal repository e riseleziona la piastra indicata.
    /// Usata dopo il flusso "Importa disegno" (drop sulla tabella generale), che può
    /// creare una nuova piastra al volo o modificarne una esistente.</summary>
    public async Task RicaricaEApriPiastraAsync(int? idPiastra)
    {
        _tutti.Clear();
        var piastre = await _piastreRepo.GetAllAsync();
        foreach (var p in piastre) _tutti.Add(p);
        AggiornaFiltro();

        if (idPiastra.HasValue)
            PiastraSelezionata = _tutti.FirstOrDefault(p => p.IdPiastra == idPiastra.Value);
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
            && FiltroDurezza.ApplicaA(p.DurezzaStandard?.Valore)
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

    private async Task ApriFormNuovaAsync()
    {
        _idPiastraInModifica = 0;
        IsModifica      = false;
        DisegnoCorrente = null;
        ResetForm();
        // Fire-and-forget: la prima lettura articoli via VPN può essere lenta,
        // il form si apre subito e i suggerimenti arrivano appena pronti.
        _ = SelettoreArticolo.InitAsync();
        FormCodicePiastra = await _piastreRepo.GetNextCodiceSuggerito();
        IsFormVisible = true;
    }

    private void ApriFormModifica()
    {
        if (PiastraSelezionata is null) return;
        _idPiastraInModifica     = PiastraSelezionata.IdPiastra;
        FormCodicePiastra        = PiastraSelezionata.CodicePiastra;
        FormCodiceArticolo       = PiastraSelezionata.CodiceArticoloGestionale  ?? string.Empty;
        _ = SelettoreArticolo.InitAsync(PiastraSelezionata.CodiceArticoloGestionale);
        FormDescrizione          = PiastraSelezionata.Descrizione                ?? string.Empty;
        FormStato                = PiastraSelezionata.Stato;
        FormCategoriaSelezionata = CategoriePiastre.FirstOrDefault(c => c.IdCategoriaPiastra == PiastraSelezionata.IdCategoriaPiastra);
        FormFormatoSelezionato   = FormatiMacchine.FirstOrDefault(f => f.IdFormato == PiastraSelezionata.IdFormato);
        FormLarghezza            = PiastraSelezionata.LarghezzaMm?.ToString("F1")  ?? string.Empty;
        FormAltezza              = PiastraSelezionata.AltezzaMm?.ToString("F1")    ?? string.Empty;
        FormSpessore             = PiastraSelezionata.SpessoreMm;
        FormDurezzaSelezionata   = DurezzePiastre.FirstOrDefault(d => d.IdDurezza == PiastraSelezionata.IdDurezza);
        FormPeso                 = PiastraSelezionata.Peso?.ToString("F3")         ?? string.Empty;
        FormNote                 = PiastraSelezionata.Note                          ?? string.Empty;
        FormClienteEsclusivo     = PiastraSelezionata.IdClienteEsclusivo.HasValue
            ? _tuttiClienti.FirstOrDefault(c => c.IdCliente == PiastraSelezionata.IdClienteEsclusivo)
            : null;
        ErroreCodiceDuplicato        = null;
        IsCodicePiastraNonValido     = false;
        IsClienteEsclusivoNonValido  = false;
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
        FormLarghezza = FormAltezza = FormPeso = string.Empty;
        FormSpessore  = null;
        FormDurezzaSelezionata = null;
        FormStato                = StatoPiastra.Attiva;
        FormCategoriaSelezionata = CategoriePiastre.FirstOrDefault(c => c.Codice == "STD");
        FormFormatoSelezionato   = null;
        FormClienteEsclusivo     = null;
        ErroreCodiceDuplicato    = null;
        PercorsoDisegnoPendente  = null;
        IsCodicePiastraNonValido    = false;
        IsClienteEsclusivoNonValido = false;
        _filtroClienteEsclusivo         = string.Empty;
        _formFiltroClienteAssociato     = string.Empty;
        OnPropertyChanged(nameof(FiltroClienteEsclusivo));
        OnPropertyChanged(nameof(FormFiltroClienteAssociato));
        ClientiEsclusiviSuggeriti.Clear();
        FormClientiDaAssociare.Clear();
        FormClientiAssociatiSuggeriti.Clear();
        OnPropertyChanged(nameof(IsClientiEsclusiviSuggerimentiVisible));
        OnPropertyChanged(nameof(IsFormClientiAssociatiSuggerimentiVisible));
        FormMacchineDaAssociare.Clear();
        IsFormAggiungiMacchinaVisible = false;
        FormMacchinaDaAggiungere      = null;
        FormMacchineDisponibili.Clear();
    }

    private async Task SalvaAsync()
    {
        IsCodicePiastraNonValido    = string.IsNullOrWhiteSpace(FormCodicePiastra);
        IsClienteEsclusivoNonValido = IsSpecialeCliente && FormClienteEsclusivo is null;
        if (IsCodicePiastraNonValido || IsClienteEsclusivoNonValido) return;
        if (IsErroreVisible) return;

        // Tipo derivato dalla categoria: SPE ⇒ SpecialeCliente, altrimenti Standard.
        var tipo = IsSpecialeCliente ? TipoPiastra.SpecialeCliente : TipoPiastra.Standard;

        // Per le Speciale Cliente il codice viene dal selettore articoli reali (TASK-19);
        // per le Standard resta il testo libero.
        var codiceArticolo = IsSpecialeCliente ? SelettoreArticolo.CodiceCorrente : N(FormCodiceArticolo);

        Piastra piastraSalvata;
        if (IsModifica)
        {
            var p = _tutti.FirstOrDefault(x => x.IdPiastra == _idPiastraInModifica);
            if (p is null) return;
            p.CodicePiastra            = FormCodicePiastra.Trim();
            p.CodiceArticoloGestionale = codiceArticolo;
            p.Descrizione              = N(FormDescrizione);
            p.Stato                    = FormStato;
            p.TipoPiastra              = tipo;
            p.IdClienteEsclusivo       = IsSpecialeCliente ? FormClienteEsclusivo?.IdCliente : null;
            p.ClienteEsclusivo         = IsSpecialeCliente ? FormClienteEsclusivo : null;
            p.IdCategoriaPiastra       = FormCategoriaSelezionata?.IdCategoriaPiastra;
            p.Categoria                = FormCategoriaSelezionata;
            p.IdFormato                = FormFormatoSelezionato?.IdFormato;
            p.Formato                  = FormFormatoSelezionato;
            p.LarghezzaMm              = ParseDecimal(FormLarghezza);
            p.AltezzaMm                = ParseDecimal(FormAltezza);
            p.SpessoreMm               = FormSpessore;
            p.IdDurezza                = FormDurezzaSelezionata?.IdDurezza;
            p.DurezzaStandard          = FormDurezzaSelezionata;
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
                CodiceArticoloGestionale = codiceArticolo,
                Descrizione              = N(FormDescrizione),
                Stato                    = FormStato,
                TipoPiastra              = tipo,
                IdClienteEsclusivo       = IsSpecialeCliente ? FormClienteEsclusivo?.IdCliente : null,
                ClienteEsclusivo         = IsSpecialeCliente ? FormClienteEsclusivo : null,
                IdCategoriaPiastra       = FormCategoriaSelezionata?.IdCategoriaPiastra,
                Categoria                = FormCategoriaSelezionata,
                IdFormato                = FormFormatoSelezionato?.IdFormato,
                Formato                  = FormFormatoSelezionato,
                LarghezzaMm              = ParseDecimal(FormLarghezza),
                AltezzaMm                = ParseDecimal(FormAltezza),
                SpessoreMm               = FormSpessore,
                IdDurezza                = FormDurezzaSelezionata?.IdDurezza,
                DurezzaStandard          = FormDurezzaSelezionata,
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
            if (IsSpecialeCliente && FormClienteEsclusivo is not null
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

            // Associa le macchine compatibili selezionate nel form (solo in creazione;
            // in modifica si usa il pannello dettaglio).
            foreach (var macchina in FormMacchineDaAssociare)
            {
                // Vincolo: se la piastra è Standard, salta le macchine che ne hanno già una.
                if (tipo == TipoPiastra.Standard
                    && await _compatRepo.HasPiastraStandardAsync(macchina.IdMacchinaStandard))
                    continue;

                var compat = new PiastraMacchinaCompatibile
                {
                    IdPiastra          = piastraSalvata.IdPiastra,
                    IdMacchinaStandard = macchina.IdMacchinaStandard,
                    Attiva             = true
                };
                await _compatRepo.AddAsync(compat);
            }
        }

        AggiornaFiltro();
        ChiudiForm();
        PiastraSelezionata = piastraSalvata;

        // Se aperto da ClienteDettaglioView, torna al cliente dopo la creazione.
        if (!IsModifica)
            DopoCreazionePiastra?.Invoke();
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

        // Se questa piastra è Standard, escludi le macchine che ne hanno già una:
        // una macchina può avere al più una piastra Standard.
        var idMacchineConStandard = PiastraSelezionata?.TipoPiastra == TipoPiastra.Standard
            ? (await _compatRepo.GetIdMacchineConPiastraStandardAsync()).ToHashSet()
            : [];

        MacchineDisponibili.Clear();
        foreach (var m in tutte.Where(m =>
            m.Attiva
            && !idGiaCompat.Contains(m.IdMacchinaStandard)
            && !idMacchineConStandard.Contains(m.IdMacchinaStandard)
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

        // Vincolo: una macchina può avere al più una piastra Standard.
        if (PiastraSelezionata.TipoPiastra == TipoPiastra.Standard
            && await _compatRepo.HasPiastraStandardAsync(MacchinaCompatibileDaAggiungere.IdMacchinaStandard))
        {
            MessageBox.Show(
                $"La macchina '{MacchinaCompatibileDaAggiungere.NomeMacchina}' ha già una piastra Standard associata.",
                "Piastra Standard già presente",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

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

        var clienteEsclusivo  = piastra.IdClienteEsclusivo.HasValue
            ? _tuttiClienti.FirstOrDefault(c => c.IdCliente == piastra.IdClienteEsclusivo)
            : null;
        var codiceCliente  = clienteEsclusivo?.CodiceClienteGestionale;
        var ragioneSociale = clienteEsclusivo?.RagioneSociale;

        var destinazione = _fileArchivio.GetPercorsoDestinazioneDisegno(
            percorsoFile, piastra.TipoPiastra, codiceCliente, ragioneSociale);

        // Avvisa se esiste già un file con lo stesso nome in destinazione, a meno che non sia
        // il percorso del disegno attuale di questa stessa piastra (sostituzione in-place).
        if (destinazione is not null && File.Exists(destinazione)
            && !destinazione.Equals(disegnoEsistente?.PercorsoFile, StringComparison.OrdinalIgnoreCase))
        {
            var risposta = MessageBox.Show(
                $"Nella cartella di archiviazione esiste già un file con lo stesso nome:\n\n" +
                $"{Path.GetFileName(percorsoFile)}\n\n" +
                $"Sovrascriverlo?",
                "File già presente",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (risposta != MessageBoxResult.Yes) return;
        }

        var percorsoEffettivo = await _fileArchivio.ArchiviaDisegnoAsync(
            percorsoFile, piastra.CodicePiastra, piastra.TipoPiastra, codiceCliente, ragioneSociale)
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

    private void CaricaFormDisegno()
    {
        if (DisegnoCorrente is null)
        {
            FormRevisioneDisegno = FormFormatoDisegno = FormNoteDisegno = string.Empty;
            FormStatoDisegno     = StatoDisegno.DaVerificare;
            return;
        }
        FormRevisioneDisegno = DisegnoCorrente.Revisione ?? string.Empty;
        FormFormatoDisegno   = DisegnoCorrente.Formato   ?? string.Empty;
        FormStatoDisegno     = DisegnoCorrente.Stato;
        FormNoteDisegno      = DisegnoCorrente.Note      ?? string.Empty;
    }

    private async Task SalvaDisegnoAsync()
    {
        if (DisegnoCorrente is null) return;

        DisegnoCorrente.Revisione = N(FormRevisioneDisegno);
        DisegnoCorrente.Formato   = N(FormFormatoDisegno);
        DisegnoCorrente.Stato     = FormStatoDisegno;
        DisegnoCorrente.Note      = N(FormNoteDisegno);

        await _disegniRepo.UpdateAsync(DisegnoCorrente);
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

    // ─── Macchine compatibili nel form (creazione, stile tabella) ─────────────

    private async Task ApriFormAggiungiMacchinaAsync()
    {
        var tutte      = await _macchineRepo.GetAllAsync();
        var idGiaScelte = FormMacchineDaAssociare.Select(m => m.IdMacchinaStandard).ToHashSet();
        // Se è stato scelto un formato, mostra solo le macchine di quel formato.
        var idFormato   = FormFormatoSelezionato?.IdFormato;

        // La piastra in creazione è Standard quando non è SpecialeCliente: in quel caso
        // escludi le macchine che hanno già una piastra Standard (al più una per macchina).
        var idMacchineConStandard = !IsSpecialeCliente
            ? (await _compatRepo.GetIdMacchineConPiastraStandardAsync()).ToHashSet()
            : [];

        FormMacchineDisponibili.Clear();
        foreach (var m in tutte.Where(m =>
            m.Attiva
            && !idGiaScelte.Contains(m.IdMacchinaStandard)
            && !idMacchineConStandard.Contains(m.IdMacchinaStandard)
            && (idFormato is null || m.IdFormato == idFormato)))
        {
            FormMacchineDisponibili.Add(m);
        }
        FormMacchinaDaAggiungere      = null;
        IsFormAggiungiMacchinaVisible = true;
    }

    private void ConfermaFormAggiungiMacchina()
    {
        if (FormMacchinaDaAggiungere is null) return;
        if (!FormMacchineDaAssociare.Contains(FormMacchinaDaAggiungere))
            FormMacchineDaAssociare.Add(FormMacchinaDaAggiungere);
        ChiudiFormAggiungiMacchina();
    }

    private void ChiudiFormAggiungiMacchina()
    {
        IsFormAggiungiMacchinaVisible = false;
        FormMacchinaDaAggiungere      = null;
        FormMacchineDisponibili.Clear();
    }

    private void RimuoviMacchinaForm(MacchinaStandard macchina)
    {
        FormMacchineDaAssociare.Remove(macchina);
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
