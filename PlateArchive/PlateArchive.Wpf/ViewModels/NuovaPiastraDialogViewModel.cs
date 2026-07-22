using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using PlateArchive.Core.Enums;
using PlateArchive.Core.Models;
using PlateArchive.Core.Servizi;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Services;
using PlateArchive.Wpf.Commands;

namespace PlateArchive.Wpf.ViewModels;

/// <summary>
/// ViewModel del dialog "Nuova piastra" aperto dal dettaglio cliente.
/// Gestisce solo la creazione (non la modifica): categoria SPE e cliente pre-impostati.
/// </summary>
public class NuovaPiastraDialogViewModel : ViewModelBase
{
    private readonly IPiastraRepository          _piastreRepo;
    private readonly ICategoriaPiastraRepository _categorieRepo;
    private readonly IFormatoMacchinaRepository  _formatiRepo;
    private readonly IDurezzaStandardRepository  _durezzaRepo;
    private readonly IDisegnoRepository          _disegniRepo;
    private readonly IFileArchivioService        _fileArchivio;
    private readonly IClientePiastraRepository   _clientiPiastreRepo;

    // ── Form backing fields ───────────────────────────────────────────────────
    private string            _formCodicePiastra        = string.Empty;
    private string            _formCodiceArticolo       = string.Empty;
    private string            _formDescrizione          = string.Empty;
    private StatoPiastra      _formStato                = StatoPiastra.Attiva;
    private CategoriaPiastra? _formCategoriaSelezionata;
    private FormatoMacchina?  _formFormatoSelezionato;
    private string            _formLarghezza            = string.Empty;
    private string            _formAltezza              = string.Empty;
    private decimal?          _formSpessore;
    private DurezzaStandard?  _formDurezzaSelezionata;
    private string            _formPeso                 = string.Empty;
    private string            _formNote                 = string.Empty;
    private string?           _percorsoDisegnoPendente;

    private bool    _isCodicePiastraNonValido;
    private string? _erroreCodiceDuplicato;

    private Cliente? _clientePreimpostato;

    public NuovaPiastraDialogViewModel(
        IPiastraRepository          piastreRepo,
        ICategoriaPiastraRepository categorieRepo,
        IFormatoMacchinaRepository  formatiRepo,
        IDurezzaStandardRepository  durezzaRepo,
        IDisegnoRepository          disegniRepo,
        IFileArchivioService        fileArchivio,
        IClientePiastraRepository   clientiPiastreRepo,
        IArticoliGestionaleService  articoliService)
    {
        _piastreRepo        = piastreRepo;
        _categorieRepo      = categorieRepo;
        _formatiRepo        = formatiRepo;
        _durezzaRepo        = durezzaRepo;
        _disegniRepo        = disegniRepo;
        _fileArchivio       = fileArchivio;
        _clientiPiastreRepo = clientiPiastreRepo;

        SelettoreArticolo = new SelettoreArticoloGestionaleViewModel(articoliService);
        SelettoreArticolo.ArticoloScelto += ProponiFormatoDaArticolo;

        SalvaCommand      = new RelayCommand(async _ => await SalvaAsync(), _ => !IsCodicePiastraNonValido && ErroreCodiceDuplicato is null);
        AnnullaCommand    = new RelayCommand(_ => ChiudiDialog?.Invoke(false));
        SfogliaFileCommand = new RelayCommand(_ => SfogliaFile());
    }

    // ── Output ────────────────────────────────────────────────────────────────

    /// <summary>Callback impostato dal chiamante per chiudere la finestra.</summary>
    public Action<bool>? ChiudiDialog { get; set; }

    /// <summary>Piastra creata — valorizzata dopo salvataggio riuscito.</summary>
    public Piastra? PiastraCreata { get; private set; }

    // ── Lookup ────────────────────────────────────────────────────────────────

    public ObservableCollection<CategoriaPiastra> CategoriePiastre { get; } = [];
    public ObservableCollection<FormatoMacchina>  FormatiMacchine  { get; } = [];
    public ObservableCollection<DurezzaStandard>  DurezzePiastre   { get; } = [];
    public IEnumerable<StatoPiastra>              StatiPiastra     { get; } = Enum.GetValues<StatoPiastra>();

    // ── Cliente preimpostato (readonly nel form) ───────────────────────────────

    public Cliente? ClientePreimpostato
    {
        get => _clientePreimpostato;
        private set => SetField(ref _clientePreimpostato, value);
    }

    public void PreimpostaCliente(Cliente c) => ClientePreimpostato = c;

    // ── Form properties ───────────────────────────────────────────────────────

    public string FormCodicePiastra
    {
        get => _formCodicePiastra;
        set
        {
            if (SetField(ref _formCodicePiastra, value))
            {
                IsCodicePiastraNonValido = string.IsNullOrWhiteSpace(value);
                ControllaDuplicato(value);
            }
        }
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

    public CategoriaPiastra? FormCategoriaSelezionata
    {
        get => _formCategoriaSelezionata;
        set
        {
            if (SetField(ref _formCategoriaSelezionata, value))
                OnPropertyChanged(nameof(IsSpecialeCliente));
        }
    }

    /// <summary>True quando la categoria è Speciale Cliente: solo allora il codice articolo
    /// si sceglie dal selettore gestionale (TASK-19), altrimenti resta testo libero.</summary>
    public bool IsSpecialeCliente => FormCategoriaSelezionata?.Codice == "SPE";

    /// <summary>Selettore articolo gestionale reale, usato per le piastre Speciale Cliente.</summary>
    public SelettoreArticoloGestionaleViewModel SelettoreArticolo { get; }

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

    // ── Validazione ───────────────────────────────────────────────────────────

    public bool IsCodicePiastraNonValido
    {
        get => _isCodicePiastraNonValido;
        set => SetField(ref _isCodicePiastraNonValido, value);
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

    // ── File disegno pendente ─────────────────────────────────────────────────

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

    // ── Comandi ───────────────────────────────────────────────────────────────

    public ICommand SalvaCommand      { get; }
    public ICommand AnnullaCommand    { get; }
    public ICommand SfogliaFileCommand { get; }

    // ── Caricamento ───────────────────────────────────────────────────────────

    public async Task LoadAsync()
    {
        var categorie = await _categorieRepo.GetAllAsync();
        foreach (var c in categorie) CategoriePiastre.Add(c);

        var formati = await _formatiRepo.GetAllAsync();
        foreach (var f in formati) FormatiMacchine.Add(f);

        var durezze = await _durezzaRepo.GetAllAsync();
        foreach (var d in durezze) DurezzePiastre.Add(d);

        FormCategoriaSelezionata = CategoriePiastre.FirstOrDefault(c => c.Codice == "SPE")
                                   ?? CategoriePiastre.FirstOrDefault();

        FormCodicePiastra = await _piastreRepo.GetNextCodiceSuggerito();

        await SelettoreArticolo.InitAsync();
    }

    // ── Persistenza ───────────────────────────────────────────────────────────

    private async Task SalvaAsync()
    {
        IsCodicePiastraNonValido = string.IsNullOrWhiteSpace(FormCodicePiastra);
        if (IsCodicePiastraNonValido || IsErroreVisible) return;

        var isSpeciale = IsSpecialeCliente;
        var tipo       = isSpeciale ? TipoPiastra.SpecialeCliente : TipoPiastra.Standard;

        // Speciale Cliente: codice dal selettore articoli reali (TASK-19); altrimenti testo libero.
        var codiceArticolo = isSpeciale ? SelettoreArticolo.CodiceCorrente : N(FormCodiceArticolo);

        var nuova = new Piastra
        {
            CodicePiastra            = FormCodicePiastra.Trim(),
            CodiceArticoloGestionale = codiceArticolo,
            Descrizione              = N(FormDescrizione),
            Stato                    = FormStato,
            TipoPiastra              = tipo,
            // Solo FK: non impostare le navigation properties per evitare che EF Core
            // tenti di inserire entità provenienti da scope DI diversi (es. ClienteEsclusivo
            // viene da ClienteDettaglioViewModel e non è tracciato da questo DbContext).
            IdClienteEsclusivo       = isSpeciale ? _clientePreimpostato?.IdCliente : null,
            IdCategoriaPiastra       = FormCategoriaSelezionata?.IdCategoriaPiastra,
            IdFormato                = FormFormatoSelezionato?.IdFormato,
            LarghezzaMm              = ParseDecimal(FormLarghezza),
            AltezzaMm                = ParseDecimal(FormAltezza),
            SpessoreMm               = FormSpessore,
            IdDurezza                = FormDurezzaSelezionata?.IdDurezza,
            Peso                     = ParseDecimal(FormPeso),
            Note                     = N(FormNote)
        };

        await _piastreRepo.AddAsync(nuova);
        PiastraCreata = nuova;

        if (_clientePreimpostato is not null)
            await _clientiPiastreRepo.AddAsync(new ClientePiastra
            {
                IdCliente        = _clientePreimpostato.IdCliente,
                IdPiastra        = nuova.IdPiastra,
                DataAssociazione = DateTime.UtcNow,
                Stato            = StatoClientePiastra.Attiva
            });

        if (!string.IsNullOrEmpty(_percorsoDisegnoPendente))
            await AssociaDisegnoAsync(nuova, _percorsoDisegnoPendente);

        ChiudiDialog?.Invoke(true);
    }

    private async Task AssociaDisegnoAsync(Piastra piastra, string percorsoFile)
    {
        var codiceCliente  = _clientePreimpostato?.CodiceClienteGestionale;
        var ragioneSociale = _clientePreimpostato?.RagioneSociale;

        var destinazione = _fileArchivio.GetPercorsoDestinazioneDisegno(
            percorsoFile, piastra.TipoPiastra, codiceCliente, ragioneSociale);

        if (destinazione is not null && File.Exists(destinazione))
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

        var nuovoDisegno = new Disegno
        {
            IdPiastra              = piastra.IdPiastra,
            CodiceDisegno          = piastra.CodicePiastra,
            NomeFile               = Path.GetFileName(percorsoEffettivo),
            PercorsoFile           = percorsoEffettivo,
            Formato                = Path.GetExtension(percorsoEffettivo).TrimStart('.').ToUpper(),
            Stato                  = StatoDisegno.Attivo,
            DataUltimaModificaFile = DateTime.UtcNow
        };
        await _disegniRepo.AddAsync(nuovoDisegno);
    }

    private void SfogliaFile()
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Seleziona file disegno",
            Filter = "File disegno|*.dwg;*.dxf;*.pdf;*.stp;*.step;*.igs|Tutti i file|*.*"
        };
        if (dlg.ShowDialog() == true)
            PercorsoDisegnoPendente = dlg.FileName;
    }

    private void ControllaDuplicato(string codice)
    {
        if (string.IsNullOrWhiteSpace(codice)) { ErroreCodiceDuplicato = null; return; }
        // Verifica solo in-memory se il codice è identico al prossimo suggerito; la duplicazione
        // vera verrà rilevata dall'unique constraint e mostrata come eccezione dal repo.
        ErroreCodiceDuplicato = null;
    }

    private static string?  N(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private static decimal? ParseDecimal(string s) =>
        decimal.TryParse(s.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var v) ? v : null;
}
