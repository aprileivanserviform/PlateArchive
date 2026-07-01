using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using PlateArchive.Core.Enums;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Services;
using PlateArchive.Wpf.Commands;

namespace PlateArchive.Wpf.ViewModels;

/// <summary>
/// ViewModel per la finestra di importazione rapida di un disegno tecnico.
/// <para>
/// Con la relazione 1:1 Disegno↔Piastra:
/// - Se il file è già nel sistema (stesso nome): mostra la piastra collegata e permette
///   solo di aggiungere/modificare il cliente associato a quella piastra.
/// - Se è nuovo: permette di collegarlo a una piastra esistente (senza disegno) oppure
///   di creare una nuova piastra.
/// </para>
/// </summary>
public class ImportaDisegnoViewModel : ViewModelBase
{
    private readonly IDisegnoRepository          _disegniRepo;
    private readonly IPiastraRepository          _piastreRepo;
    private readonly ICategoriaPiastraRepository _categorieRepo;
    private readonly IFormatoMacchinaRepository  _formatiRepo;
    private readonly IClienteRepository          _clientiRepo;
    private readonly IClientePiastraRepository   _clientiPiastreRepo;
    private readonly IFileArchivioService        _fileArchivio;

    private readonly List<Cliente> _tuttiClienti = [];

    private string       _nomeFile                = string.Empty;
    private string       _percorsoFile            = string.Empty;
    private Disegno?     _disegnoEsistente;
    private bool         _isCreaNuovaPiastraMode;
    private Piastra?     _piastraSelezionata;
    private string       _formCodicePiastra       = string.Empty;
    private string       _formCodiceArticolo      = string.Empty;
    private string       _formDescrizione         = string.Empty;
    private StatoPiastra _formStato               = StatoPiastra.Attiva;
    private CategoriaPiastra?  _formCategoria;
    private FormatoMacchina?   _formFormato;
    private string       _formLarghezza           = string.Empty;
    private string       _formAltezza             = string.Empty;
    private string       _formSpessore            = string.Empty;
    private string       _formDurezza             = string.Empty;
    private string       _formPeso                = string.Empty;
    private string       _formNote                = string.Empty;
    private Cliente?     _formCliente;
    private string       _filtroCliente           = string.Empty;
    private string?      _errore;
    private bool         _confermato;

    public ImportaDisegnoViewModel(
        IDisegnoRepository          disegniRepo,
        IPiastraRepository          piastreRepo,
        ICategoriaPiastraRepository categorieRepo,
        IFormatoMacchinaRepository  formatiRepo,
        IClienteRepository          clientiRepo,
        IClientePiastraRepository   clientiPiastreRepo,
        IFileArchivioService        fileArchivio)
    {
        _disegniRepo        = disegniRepo;
        _piastreRepo        = piastreRepo;
        _categorieRepo      = categorieRepo;
        _formatiRepo        = formatiRepo;
        _clientiRepo        = clientiRepo;
        _clientiPiastreRepo = clientiPiastreRepo;
        _fileArchivio       = fileArchivio;

        ConfermaCommand         = new RelayCommand(async _ => await ConfermaAsync(), _ => PuoConfermare());
        AnnullaCommand          = new RelayCommand(_ => Annulla());
        SelezionaClienteCommand = new RelayCommand(o => { if (o is Cliente c) FormCliente = c; });
        RimuoviClienteCommand   = new RelayCommand(_ => FormCliente = null);
    }

    // ─── Dati file ────────────────────────────────────────────────────────────

    public string NomeFile
    {
        get => _nomeFile;
        private set => SetField(ref _nomeFile, value);
    }

    public string PercorsoFile
    {
        get => _percorsoFile;
        private set => SetField(ref _percorsoFile, value);
    }

    // ─── Stato disegno ────────────────────────────────────────────────────────

    public Disegno? DisegnoEsistente
    {
        get => _disegnoEsistente;
        private set
        {
            if (SetField(ref _disegnoEsistente, value))
            {
                OnPropertyChanged(nameof(IsNuovoDisegno));
                OnPropertyChanged(nameof(IsDisegnoEsistente));
                OnPropertyChanged(nameof(TitoloStato));
                OnPropertyChanged(nameof(IsCreaNuovaPiastraOptionVisible));
                OnPropertyChanged(nameof(TestoConferma));
                OnPropertyChanged(nameof(TitoloSezioneCliente));
                OnPropertyChanged(nameof(PiastraGiaAssociata));
            }
        }
    }

    public bool IsNuovoDisegno                  => _disegnoEsistente is null;
    public bool IsDisegnoEsistente              => _disegnoEsistente is not null;
    public bool IsCreaNuovaPiastraOptionVisible  => _disegnoEsistente is null;
    public string TestoConferma                  => _disegnoEsistente is not null ? "Associa cliente" : "Importa";

    public string TitoloStato => IsNuovoDisegno
        ? "Nuovo disegno — non ancora nel sistema"
        : $"Disegno già nel sistema (collegato a: {_disegnoEsistente?.Piastra?.CodicePiastra ?? "nessuna piastra"})";

    /// <summary>Piastra già collegata al disegno esistente (solo per la visualizzazione).</summary>
    public Piastra? PiastraGiaAssociata => _disegnoEsistente?.Piastra;

    public ObservableCollection<Piastra>          PiastреDisponibili   { get; } = [];
    public ObservableCollection<CategoriaPiastra> CategoriePiastre     { get; } = [];
    public ObservableCollection<FormatoMacchina>  FormatiMacchine      { get; } = [];
    public ObservableCollection<ClientePiastra>   ClientiGiaAssociati  { get; } = [];

    public string TitoloSezioneCliente =>
        IsDisegnoEsistente ? "Aggiungi cliente associato" :
        IsClienteObbligatorio ? "Cliente associato *" :
        "Associa a un cliente (opzionale)";

    // ─── Modalità selezione piastra ───────────────────────────────────────────

    public bool IsCreaNuovaPiastraMode
    {
        get => _isCreaNuovaPiastraMode;
        set
        {
            if (SetField(ref _isCreaNuovaPiastraMode, value))
            {
                OnPropertyChanged(nameof(IsAssociaEsistenteMode));
                if (value) PiastraSelezionata = null;
                else
                {
                    FormCodicePiastra = FormCodiceArticolo = FormDescrizione = string.Empty;
                    FormStato         = StatoPiastra.Attiva;
                    FormCategoria     = CategoriePiastre.FirstOrDefault(c => c.Codice == "STD");
                    FormFormato       = null;
                    FormLarghezza = FormAltezza = FormSpessore = FormDurezza = FormPeso = FormNote = string.Empty;
                }
            }
        }
    }

    public bool IsAssociaEsistenteMode
    {
        get => !_isCreaNuovaPiastraMode;
        set => IsCreaNuovaPiastraMode = !value;
    }

    public Piastra? PiastraSelezionata
    {
        get => _piastraSelezionata;
        set => SetField(ref _piastraSelezionata, value);
    }

    // ─── Campi mini-form nuova piastra ────────────────────────────────────────

    public string FormCodicePiastra
    {
        get => _formCodicePiastra;
        set => SetField(ref _formCodicePiastra, value);
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

    public IEnumerable<StatoPiastra> StatiPiastra { get; } = Enum.GetValues<StatoPiastra>();

    public CategoriaPiastra? FormCategoria
    {
        get => _formCategoria;
        set
        {
            if (SetField(ref _formCategoria, value))
            {
                OnPropertyChanged(nameof(IsClienteObbligatorio));
                OnPropertyChanged(nameof(TitoloSezioneCliente));
            }
        }
    }

    /// <summary>True quando la categoria selezionata è "Speciale Cliente" (SPE) — il cliente diventa obbligatorio.</summary>
    public bool IsClienteObbligatorio => FormCategoria?.Codice == "SPE";

    public FormatoMacchina? FormFormato
    {
        get => _formFormato;
        set => SetField(ref _formFormato, value);
    }

    public string FormLarghezza { get => _formLarghezza; set => SetField(ref _formLarghezza, value); }
    public string FormAltezza   { get => _formAltezza;   set => SetField(ref _formAltezza,   value); }
    public string FormSpessore  { get => _formSpessore;  set => SetField(ref _formSpessore,  value); }
    public string FormDurezza   { get => _formDurezza;   set => SetField(ref _formDurezza,   value); }
    public string FormPeso      { get => _formPeso;      set => SetField(ref _formPeso,      value); }
    public string FormNote      { get => _formNote;      set => SetField(ref _formNote,      value); }

    // ─── Ricerca cliente ──────────────────────────────────────────────────────

    public Cliente? FormCliente
    {
        get => _formCliente;
        set
        {
            if (SetField(ref _formCliente, value))
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

    public ObservableCollection<Cliente> ClientiSuggeriti { get; } = [];

    public bool IsClienteSelezionatoVisible  => _formCliente is not null;
    public bool IsClienteSearchVisible       => _formCliente is null;
    public bool IsClienteSuggerimentiVisible => ClientiSuggeriti.Count > 0;

    private void AggiornaClientiSuggeriti()
    {
        ClientiSuggeriti.Clear();
        var f = _filtroCliente.Trim().ToLower();
        if (!string.IsNullOrEmpty(f))
        {
            foreach (var c in _tuttiClienti
                .Where(c => c.CodiceClienteGestionale.ToLower().Contains(f)
                         || c.RagioneSociale.ToLower().Contains(f))
                .Take(8))
                ClientiSuggeriti.Add(c);
        }
        OnPropertyChanged(nameof(IsClienteSuggerimentiVisible));
    }

    // ─── Errore e risultato ───────────────────────────────────────────────────

    public string? Errore
    {
        get => _errore;
        set { if (SetField(ref _errore, value)) OnPropertyChanged(nameof(IsErroreVisible)); }
    }

    public bool IsErroreVisible => !string.IsNullOrEmpty(_errore);

    public bool Confermato
    {
        get => _confermato;
        private set => SetField(ref _confermato, value);
    }

    public Disegno? DisegnoCreato { get; private set; }

    public event EventHandler? RichiestaChiusura;

    // ─── Comandi ─────────────────────────────────────────────────────────────

    public ICommand ConfermaCommand         { get; }
    public ICommand AnnullaCommand          { get; }
    public ICommand SelezionaClienteCommand { get; }
    public ICommand RimuoviClienteCommand   { get; }

    // ─── Inizializzazione ─────────────────────────────────────────────────────

    public async Task InitAsync(string percorsoFile)
    {
        PercorsoFile      = percorsoFile;
        NomeFile          = Path.GetFileName(percorsoFile);
        FormCodicePiastra = Path.GetFileNameWithoutExtension(percorsoFile);

        DisegnoEsistente = await _disegniRepo.GetByNomeFileAsync(NomeFile);

        if (DisegnoEsistente is not null)
        {
            // Disegno già nel sistema: forza modalità "piastra esistente" (1:1 — non riassegnabile).
            _isCreaNuovaPiastraMode = false;
            OnPropertyChanged(nameof(IsCreaNuovaPiastraMode));
            OnPropertyChanged(nameof(IsAssociaEsistenteMode));

            // Carica i clienti già associati alla piastra collegata
            if (DisegnoEsistente.IdPiastra.HasValue)
            {
                var assoc = await _clientiPiastreRepo.GetByPiastraAsync(DisegnoEsistente.IdPiastra.Value);
                foreach (var cp in assoc) ClientiGiaAssociati.Add(cp);
            }
        }

        // Nella lista "piastre disponibili" mostro solo piastre senza disegno associato.
        var tutte = await _piastreRepo.GetAllAsync();
        foreach (var p in tutte.Where(p => p.Disegno is null))
            PiastреDisponibili.Add(p);

        var categorie = await _categorieRepo.GetAllAsync();
        foreach (var c in categorie) CategoriePiastre.Add(c);
        FormCategoria = CategoriePiastre.FirstOrDefault(c => c.Codice == "STD");

        var formati = await _formatiRepo.GetAllAsync();
        foreach (var f in formati) FormatiMacchine.Add(f);

        var clienti = await _clientiRepo.GetAllAsync();
        _tuttiClienti.AddRange(clienti.OrderBy(c => c.RagioneSociale));

        OnPropertyChanged(nameof(TitoloStato));
    }

    // ─── Logica di conferma ───────────────────────────────────────────────────

    private bool PuoConfermare()
    {
        // Disegno già nel sistema: deve selezionare un cliente da associare alla piastra.
        if (_disegnoEsistente is not null)
            return FormCliente is not null && PiastraGiaAssociata is not null;

        // Categoria Speciale Cliente (SPE): il cliente è obbligatorio
        if (IsCreaNuovaPiastraMode && IsClienteObbligatorio && FormCliente is null)
            return false;

        return IsAssociaEsistenteMode
            ? PiastraSelezionata is not null
            : !string.IsNullOrWhiteSpace(FormCodicePiastra);
    }

    private async Task ConfermaAsync()
    {
        Errore = null;

        Piastra piastra;

        if (_disegnoEsistente is not null)
        {
            // Disegno già nel sistema: associa solo il cliente alla piastra collegata.
            if (PiastraGiaAssociata is null) return;
            piastra = PiastraGiaAssociata;
            DisegnoCreato = _disegnoEsistente;
        }
        else if (IsCreaNuovaPiastraMode)
        {
            var isSpeciale = IsClienteObbligatorio;
            var nuova = new Piastra
            {
                CodicePiastra              = FormCodicePiastra.Trim(),
                CodiceArticoloGestionale   = string.IsNullOrWhiteSpace(FormCodiceArticolo) ? null : FormCodiceArticolo.Trim(),
                Descrizione                = string.IsNullOrWhiteSpace(FormDescrizione)    ? null : FormDescrizione.Trim(),
                Stato                      = FormStato,
                TipoPiastra                = isSpeciale ? TipoPiastra.SpecialeCliente : TipoPiastra.Standard,
                IdClienteEsclusivo         = isSpeciale ? FormCliente?.IdCliente : null,
                ClienteEsclusivo           = isSpeciale ? FormCliente : null,
                IdCategoriaPiastra         = FormCategoria?.IdCategoriaPiastra,
                Categoria                  = FormCategoria,
                IdFormato                  = FormFormato?.IdFormato,
                Formato                    = FormFormato,
                LarghezzaMm                = decimal.TryParse(FormLarghezza, out var l) ? l : null,
                AltezzaMm                  = decimal.TryParse(FormAltezza,   out var a) ? a : null,
                SpessoreMm                 = decimal.TryParse(FormSpessore,  out var s) ? s : null,
                Durezza                    = decimal.TryParse(FormDurezza,   out var d) ? d : null,
                Peso                       = decimal.TryParse(FormPeso,      out var p) ? p : null,
                Note                       = string.IsNullOrWhiteSpace(FormNote) ? null : FormNote.Trim(),
            };
            try   { await _piastreRepo.AddAsync(nuova); }
            catch (Exception ex) { Errore = $"Impossibile creare la piastra: {ex.Message}"; return; }
            piastra = nuova;

            var percorsoEffettivo = await _fileArchivio.ArchiviaDisegnoAsync(
                PercorsoFile, piastra.CodicePiastra, piastra.TipoPiastra)
                ?? PercorsoFile;

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
            try   { await _disegniRepo.AddAsync(nuovoDisegno); }
            catch (Exception ex) { Errore = $"Impossibile salvare il disegno: {ex.Message}"; return; }
            DisegnoCreato = nuovoDisegno;
        }
        else
        {
            if (PiastraSelezionata is null) return;
            piastra = PiastraSelezionata;

            var percorsoEffettivo = await _fileArchivio.ArchiviaDisegnoAsync(
                PercorsoFile, piastra.CodicePiastra, piastra.TipoPiastra)
                ?? PercorsoFile;

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
            try   { await _disegniRepo.AddAsync(nuovoDisegno); }
            catch (Exception ex) { Errore = $"Impossibile salvare il disegno: {ex.Message}"; return; }
            DisegnoCreato = nuovoDisegno;
        }

        if (FormCliente is not null)
        {
            try
            {
                await _clientiPiastreRepo.AddAsync(new ClientePiastra
                {
                    IdCliente        = FormCliente.IdCliente,
                    IdPiastra        = piastra.IdPiastra,
                    DataAssociazione = DateTime.UtcNow,
                    Stato            = StatoClientePiastra.Attiva
                });
            }
            catch (Exception ex) { Errore = $"Impossibile associare il cliente: {ex.Message}"; return; }
        }

        Confermato = true;
        RichiestaChiusura?.Invoke(this, EventArgs.Empty);
    }

    private void Annulla()
    {
        Confermato = false;
        RichiestaChiusura?.Invoke(this, EventArgs.Empty);
    }
}
