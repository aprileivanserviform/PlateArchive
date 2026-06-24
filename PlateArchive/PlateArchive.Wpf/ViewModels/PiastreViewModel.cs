using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using PlateArchive.Core.Enums;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Services;
using PlateArchive.Wpf.Commands;

namespace PlateArchive.Wpf.ViewModels;

public class PiastreViewModel : ViewModelBase
{
    private readonly IPiastraRepository          _piastreRepo;
    private readonly ICompatibilitaRepository    _compatRepo;
    private readonly IClientePiastraRepository   _clientiPiastreRepo;
    private readonly IMacchinaStandardRepository _macchineRepo;
    private readonly IDisegnoRepository          _disegniRepo;
    private readonly IFileArchivioService        _fileArchivio;

    private readonly ObservableCollection<Piastra> _tutti = [];

    private string    _filtroRicerca              = string.Empty;
    private string    _filtroStatoSelezionato     = "Tutti";
    private string    _filtroCategoriaSelezionato = "Tutti";
    private string    _filtroFormatoSelezionato   = "Tutti";
    private Piastra?  _piastraSelezionata;
    private bool      _isFormVisible;
    private bool      _isModifica;
    private int       _idPiastraInModifica;

    // Campi form
    private string       _formCodicePiastra   = string.Empty;
    private string       _formCodiceArticolo  = string.Empty;
    private string       _formDescrizione     = string.Empty;
    private StatoPiastra _formStato           = StatoPiastra.Attiva;
    private string       _formCategoria       = "—";
    private string       _formFormatoMacchina = string.Empty;
    private string       _formLarghezza       = string.Empty;
    private string       _formLunghezza       = string.Empty;
    private string       _formSpessore        = string.Empty;
    private string       _formNote            = string.Empty;
    private string?      _erroreCodiceDuplicato;
    private string?      _erroreDisegno;
    private string?      _percorsoDisegnoPendente;

    // Aggiungi macchina compatibile
    private bool              _isAggiungiMacchinaVisible;
    private MacchinaStandard? _macchinaCompatibileDaAggiungere;

    public PiastreViewModel(
        IPiastraRepository          piastreRepo,
        ICompatibilitaRepository    compatRepo,
        IClientePiastraRepository   clientiPiastreRepo,
        IMacchinaStandardRepository macchineRepo,
        IDisegnoRepository          disegniRepo,
        IFileArchivioService        fileArchivio)
    {
        _piastreRepo        = piastreRepo;
        _compatRepo         = compatRepo;
        _clientiPiastreRepo = clientiPiastreRepo;
        _macchineRepo       = macchineRepo;
        _disegniRepo        = disegniRepo;
        _fileArchivio       = fileArchivio;

        NuovaCommand                    = new RelayCommand(_ => ApriFormNuova());
        ModificaCommand                 = new RelayCommand(_ => ApriFormModifica(),             _ => PiastraSelezionata is not null);
        SalvaCommand                    = new RelayCommand(async _ => await SalvaAsync());
        AnnullaFormCommand              = new RelayCommand(_ => ChiudiForm());
        AggiungiMacchinaCommand         = new RelayCommand(async _ => await ApriAggiungiMacchinaAsync(), _ => PiastraSelezionata is not null);
        ConfermaAggiungiMacchinaCommand = new RelayCommand(async _ => await ConfermaAggiungiMacchinaAsync(), _ => MacchinaCompatibileDaAggiungere is not null);
        AnnullaAggiungiMacchinaCommand  = new RelayCommand(_ => ChiudiAggiungiMacchina());
        RimuoviCompatibilitaCommand     = new RelayCommand(async p => await RimuoviCompatibilitaAsync(p));
        AprirDisegnoCommand             = new RelayCommand(_ => AprirDisegno(), _ => !string.IsNullOrEmpty(PiastraSelezionata?.Disegno?.PercorsoFile));

        _ = LoadAsync();
    }

    // ─── Proprietà lista ─────────────────────────────────────────

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

    public string FiltroCategoriaSelezionato
    {
        get => _filtroCategoriaSelezionato;
        set { if (SetField(ref _filtroCategoriaSelezionato, value)) AggiornaFiltro(); }
    }

    public string FiltroFormatoSelezionato
    {
        get => _filtroFormatoSelezionato;
        set { if (SetField(ref _filtroFormatoSelezionato, value)) AggiornaFiltro(); }
    }

    public IEnumerable<string> StatiFiltro        { get; } = ["Tutti", "Attiva", "Obsoleta", "Da verificare"];
    public IEnumerable<string> CategorieFiltro    { get; } = ["Tutti", "Standard", "Speciale"];
    public IEnumerable<string> FormatiMacchinaFiltro { get; } = ["Tutti", "102", "106", "145"];
    public IEnumerable<string> CategorieForm      { get; } = ["—", "Standard", "Speciale"];
    public IEnumerable<string> FormatiMacchina    { get; } = ["", "102", "106", "145"];

    public ObservableCollection<Piastra> PiastreFiltrate { get; } = [];

    public Piastra? PiastraSelezionata
    {
        get => _piastraSelezionata;
        set
        {
            if (SetField(ref _piastraSelezionata, value))
            {
                ErroreDisegno = null;
                OnPropertyChanged(nameof(IsDetailVisible));
                OnPropertyChanged(nameof(IsDisegnoPresente));
                OnPropertyChanged(nameof(IsDisegnoAssente));
                if (IsFormVisible && IsModifica && value is not null)
                    ApriFormModifica();
                _ = LoadDettaglioAsync();
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

    // ─── Proprietà pannello dettaglio ────────────────────────────

    public ObservableCollection<PiastraMacchinaCompatibile> MacchineCompatibili { get; } = [];
    public ObservableCollection<ClientePiastra>             ClientiAssociati    { get; } = [];
    public ObservableCollection<MacchinaStandard>           MacchineDisponibili { get; } = [];

    public bool IsDisegnoPresente => PiastraSelezionata?.Disegno is not null;
    public bool IsDisegnoAssente  => PiastraSelezionata?.Disegno is null;

    // ─── Aggiungi macchina compatibile ───────────────────────────

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

    // ─── Proprietà form ──────────────────────────────────────────

    public bool IsFormVisible
    {
        get => _isFormVisible;
        set { if (SetField(ref _isFormVisible, value)) OnPropertyChanged(nameof(IsDetailVisible)); }
    }

    public bool IsModifica
    {
        get => _isModifica;
        set { if (SetField(ref _isModifica, value)) OnPropertyChanged(nameof(FormTitolo)); }
    }

    public bool IsDetailVisible => PiastraSelezionata is not null && !IsFormVisible;

    public string FormTitolo => IsModifica ? "Modifica piastra" : "Nuova piastra";

    public IEnumerable<StatoPiastra> StatiPiastra { get; } = Enum.GetValues<StatoPiastra>();

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

    public string FormCategoria
    {
        get => _formCategoria;
        set => SetField(ref _formCategoria, value);
    }

    public string FormFormatoMacchina
    {
        get => _formFormatoMacchina;
        set => SetField(ref _formFormatoMacchina, value);
    }

    public string FormLarghezza
    {
        get => _formLarghezza;
        set => SetField(ref _formLarghezza, value);
    }

    public string FormLunghezza
    {
        get => _formLunghezza;
        set => SetField(ref _formLunghezza, value);
    }

    public string FormSpessore
    {
        get => _formSpessore;
        set => SetField(ref _formSpessore, value);
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

    // ─── Comandi ─────────────────────────────────────────────────

    public ICommand NuovaCommand                    { get; }
    public ICommand ModificaCommand                 { get; }
    public ICommand SalvaCommand                    { get; }
    public ICommand AnnullaFormCommand              { get; }
    public ICommand AggiungiMacchinaCommand         { get; }
    public ICommand ConfermaAggiungiMacchinaCommand { get; }
    public ICommand AnnullaAggiungiMacchinaCommand  { get; }
    public ICommand RimuoviCompatibilitaCommand     { get; }
    public ICommand AprirDisegnoCommand             { get; }

    // ─── Caricamento ─────────────────────────────────────────────

    private async Task LoadAsync()
    {
        var piastre = await _piastreRepo.GetAllAsync();
        foreach (var p in piastre) _tutti.Add(p);
        AggiornaFiltro();
    }

    private async Task LoadDettaglioAsync()
    {
        MacchineCompatibili.Clear();
        ClientiAssociati.Clear();
        if (PiastraSelezionata is null) return;

        var id = PiastraSelezionata.IdPiastra;
        var (macchine, clienti) = (
            await _compatRepo.GetByPiastraAsync(id),
            await _clientiPiastreRepo.GetByPiastraAsync(id));

        foreach (var m in macchine) MacchineCompatibili.Add(m);
        foreach (var c in clienti)  ClientiAssociati.Add(c);
    }

    private void AggiornaFiltro()
    {
        StatoPiastra? statoFiltro = FiltroStatoSelezionato switch
        {
            "Attiva"        => StatoPiastra.Attiva,
            "Obsoleta"      => StatoPiastra.Obsoleta,
            "Da verificare" => StatoPiastra.DaVerificare,
            _               => null
        };

        CategoriaPiastra? catFiltro = FiltroCategoriaSelezionato switch
        {
            "Standard" => CategoriaPiastra.Standard,
            "Speciale"  => CategoriaPiastra.Speciale,
            _           => null
        };

        var formatoFiltro = FiltroFormatoSelezionato == "Tutti" ? null : FiltroFormatoSelezionato;
        var f = FiltroRicerca.Trim().ToLower();

        PiastreFiltrate.Clear();
        foreach (var p in _tutti.Where(p =>
            (statoFiltro is null || p.Stato == statoFiltro)
            && (catFiltro is null || p.Categoria == catFiltro)
            && (formatoFiltro is null || p.FormatoMacchina == formatoFiltro)
            && (string.IsNullOrEmpty(f)
                || p.CodicePiastra.ToLower().Contains(f)
                || (p.CodiceArticoloGestionale?.ToLower().Contains(f) ?? false)
                || (p.Descrizione?.ToLower().Contains(f) ?? false))))
        {
            PiastreFiltrate.Add(p);
        }
    }

    // ─── Controllo duplicato codice ──────────────────────────────

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

    // ─── Gestione form ───────────────────────────────────────────

    private void ApriFormNuova()
    {
        _idPiastraInModifica = 0;
        IsModifica = false;
        ResetForm();
        IsFormVisible = true;
    }

    private void ApriFormModifica()
    {
        if (PiastraSelezionata is null) return;
        _idPiastraInModifica = PiastraSelezionata.IdPiastra;
        FormCodicePiastra   = PiastraSelezionata.CodicePiastra;
        FormCodiceArticolo  = PiastraSelezionata.CodiceArticoloGestionale ?? string.Empty;
        FormDescrizione     = PiastraSelezionata.Descrizione               ?? string.Empty;
        FormStato           = PiastraSelezionata.Stato;
        FormCategoria       = PiastraSelezionata.Categoria switch
        {
            CategoriaPiastra.Standard => "Standard",
            CategoriaPiastra.Speciale  => "Speciale",
            _                          => "—"
        };
        FormFormatoMacchina = PiastraSelezionata.FormatoMacchina ?? string.Empty;
        FormLarghezza       = PiastraSelezionata.LarghezzaMm?.ToString("F1") ?? string.Empty;
        FormLunghezza       = PiastraSelezionata.LunghezzaMm?.ToString("F1") ?? string.Empty;
        FormSpessore        = PiastraSelezionata.SpessoreMm?.ToString("F2")  ?? string.Empty;
        FormNote            = PiastraSelezionata.Note                         ?? string.Empty;
        ErroreCodiceDuplicato = null;
        IsModifica    = true;
        IsFormVisible = true;
    }

    private void ChiudiForm()
    {
        IsFormVisible = false;
        ResetForm();
    }

    private void ResetForm()
    {
        FormCodicePiastra = FormCodiceArticolo = FormDescrizione = FormNote = string.Empty;
        FormFormatoMacchina = FormLarghezza = FormLunghezza = FormSpessore = string.Empty;
        FormStato     = StatoPiastra.Attiva;
        FormCategoria = "—";
        ErroreCodiceDuplicato   = null;
        PercorsoDisegnoPendente = null;
    }

    private async Task SalvaAsync()
    {
        if (string.IsNullOrWhiteSpace(FormCodicePiastra)) return;
        if (IsErroreVisible) return;

        Piastra piastraSalvata;
        if (IsModifica)
        {
            var p = _tutti.FirstOrDefault(x => x.IdPiastra == _idPiastraInModifica);
            if (p is null) return;
            p.CodicePiastra            = FormCodicePiastra.Trim();
            p.CodiceArticoloGestionale = N(FormCodiceArticolo);
            p.Descrizione              = N(FormDescrizione);
            p.Stato                    = FormStato;
            p.Categoria                = ParseCategoria(FormCategoria);
            p.FormatoMacchina          = N(FormFormatoMacchina);
            p.LarghezzaMm              = ParseMm(FormLarghezza);
            p.LunghezzaMm              = ParseMm(FormLunghezza);
            p.SpessoreMm               = ParseMm(FormSpessore);
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
                Categoria                = ParseCategoria(FormCategoria),
                FormatoMacchina          = N(FormFormatoMacchina),
                LarghezzaMm              = ParseMm(FormLarghezza),
                LunghezzaMm              = ParseMm(FormLunghezza),
                SpessoreMm               = ParseMm(FormSpessore),
                Note                     = N(FormNote)
            };
            await _piastreRepo.AddAsync(nuova);
            _tutti.Add(nuova);
            piastraSalvata = nuova;
        }

        // Associa il disegno trascinato nel form (se presente)
        var filePendente = _percorsoDisegnoPendente;
        AggiornaFiltro();
        ChiudiForm();
        PiastraSelezionata = piastraSalvata;

        if (!string.IsNullOrEmpty(filePendente))
            await AssociaDisegnoAsync(piastraSalvata, filePendente);
    }

    // ─── Aggiungi / rimuovi macchina compatibile ─────────────────

    private async Task ApriAggiungiMacchinaAsync()
    {
        var tutte = await _macchineRepo.GetAllAsync();
        var idGiaCompat = MacchineCompatibili.Select(c => c.IdMacchinaStandard).ToHashSet();
        MacchineDisponibili.Clear();
        foreach (var m in tutte.Where(m => m.Attiva && !idGiaCompat.Contains(m.IdMacchinaStandard)))
            MacchineDisponibili.Add(m);
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
        await _compatRepo.DeleteAsync(c.IdCompatibilita);
        await LoadDettaglioAsync();
    }

    // ─── Apertura file disegno ───────────────────────────────────

    private void AprirDisegno()
    {
        var percorso = PiastraSelezionata?.Disegno?.PercorsoFile;
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

    // ─── Associazione disegno via drag & drop ────────────────────

    public async Task AssociaDisegnoAsync(Piastra piastra, string percorsoFile)
    {
        // Copia nella cartella condivisa se configurata; altrimenti usa il percorso originale
        var percorsoEffettivo = await _fileArchivio.ArchiviaDisegnoAsync(percorsoFile, piastra.CodicePiastra)
                                ?? percorsoFile;

        var formato = Path.GetExtension(percorsoEffettivo).TrimStart('.').ToUpper();

        if (piastra.Disegno is null)
        {
            var nuovoDisegno = new Disegno
            {
                IdPiastra              = piastra.IdPiastra,
                CodiceDisegno          = piastra.CodicePiastra,
                NomeFile               = Path.GetFileName(percorsoEffettivo),
                PercorsoFile           = percorsoEffettivo,
                Formato                = formato,
                Stato                  = StatoDisegno.DaVerificare,
                DataUltimaModificaFile = DateTime.UtcNow
            };
            await _disegniRepo.AddAsync(nuovoDisegno);
            piastra.Disegno = nuovoDisegno;
        }
        else
        {
            piastra.Disegno.NomeFile               = Path.GetFileName(percorsoEffettivo);
            piastra.Disegno.PercorsoFile           = percorsoEffettivo;
            piastra.Disegno.Formato                = formato;
            piastra.Disegno.DataUltimaModificaFile = DateTime.UtcNow;
            await _disegniRepo.UpdateAsync(piastra.Disegno);
        }

        if (PiastraSelezionata == piastra)
        {
            OnPropertyChanged(nameof(PiastraSelezionata));
            OnPropertyChanged(nameof(IsDisegnoPresente));
            OnPropertyChanged(nameof(IsDisegnoAssente));
        }
    }

    private static string? N(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static CategoriaPiastra? ParseCategoria(string s) => s switch
    {
        "Standard" => CategoriaPiastra.Standard,
        "Speciale"  => CategoriaPiastra.Speciale,
        _           => null
    };

    private static decimal? ParseMm(string s) =>
        decimal.TryParse(s.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
}
