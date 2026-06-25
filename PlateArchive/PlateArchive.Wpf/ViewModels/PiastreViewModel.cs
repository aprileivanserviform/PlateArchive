using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
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
    private readonly ICategoriaPiastraRepository _categorieRepo;

    private readonly ObservableCollection<Piastra> _tutti = [];

    private string    _filtroRicerca              = string.Empty;
    private string    _filtroStatoSelezionato     = "Tutti";
    private string    _filtroCategoriaSelezionato = "Tutti";
    private Piastra?  _piastraSelezionata;
    private bool      _isFormVisible;
    private bool      _isModifica;
    private int       _idPiastraInModifica;

    // Campi form
    private string             _formCodicePiastra        = string.Empty;
    private string             _formCodiceArticolo       = string.Empty;
    private string             _formDescrizione          = string.Empty;
    private StatoPiastra       _formStato                = StatoPiastra.Attiva;
    private CategoriaPiastra?  _formCategoriaSelezionata;
    private string             _formLarghezza            = string.Empty;
    private string             _formAltezza              = string.Empty;
    private string             _formSpessore             = string.Empty;
    private string             _formDurezza              = string.Empty;
    private string             _formPeso                 = string.Empty;
    private string             _formNote                 = string.Empty;
    private string?            _erroreCodiceDuplicato;
    private string?            _erroreDisegno;
    private string?            _percorsoDisegnoPendente;

    // Aggiungi macchina compatibile
    private bool              _isAggiungiMacchinaVisible;
    private MacchinaStandard? _macchinaCompatibileDaAggiungere;

    public PiastreViewModel(
        IPiastraRepository          piastreRepo,
        ICompatibilitaRepository    compatRepo,
        IClientePiastraRepository   clientiPiastreRepo,
        IMacchinaStandardRepository macchineRepo,
        IDisegnoRepository          disegniRepo,
        IFileArchivioService        fileArchivio,
        ICategoriaPiastraRepository categorieRepo)
    {
        _piastreRepo        = piastreRepo;
        _compatRepo         = compatRepo;
        _clientiPiastreRepo = clientiPiastreRepo;
        _macchineRepo       = macchineRepo;
        _disegniRepo        = disegniRepo;
        _fileArchivio       = fileArchivio;
        _categorieRepo      = categorieRepo;

        NuovaCommand                    = new RelayCommand(_ => ApriFormNuova());
        ModificaCommand                 = new RelayCommand(_ => ApriFormModifica(),                          _ => PiastraSelezionata is not null);
        SalvaCommand                    = new RelayCommand(async _ => await SalvaAsync());
        AnnullaFormCommand              = new RelayCommand(_ => ChiudiForm());
        EliminaCommand                  = new RelayCommand(async _ => await EliminaAsync(),                  _ => PiastraSelezionata is not null);
        AggiungiMacchinaCommand         = new RelayCommand(async _ => await ApriAggiungiMacchinaAsync(),     _ => PiastraSelezionata is not null);
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

    public IEnumerable<string>       StatiFiltro  { get; } = ["Tutti", "Attiva", "Obsoleta", "Da verificare"];
    public IEnumerable<StatoPiastra> StatiPiastra { get; } = Enum.GetValues<StatoPiastra>();

    // Categorie da DB — usate sia nel filtro che nel form
    public ObservableCollection<CategoriaPiastra> CategoriePiastre { get; } = [];

    // Filtro: "Tutti" + descrizioni caricate da DB
    public IEnumerable<string> CategorieFiltro =>
        Enumerable.Prepend(CategoriePiastre.Select(c => c.Descrizione), "Tutti");

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

    public bool   IsDetailVisible => PiastraSelezionata is not null && !IsFormVisible;
    public string FormTitolo      => IsModifica ? "Modifica piastra" : "Nuova piastra";

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

    public CategoriaPiastra? FormCategoriaSelezionata
    {
        get => _formCategoriaSelezionata;
        set => SetField(ref _formCategoriaSelezionata, value);
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

    // ─── Comandi ─────────────────────────────────────────────────

    public ICommand NuovaCommand                    { get; }
    public ICommand ModificaCommand                 { get; }
    public ICommand SalvaCommand                    { get; }
    public ICommand AnnullaFormCommand              { get; }
    public ICommand EliminaCommand                  { get; }
    public ICommand AggiungiMacchinaCommand         { get; }
    public ICommand ConfermaAggiungiMacchinaCommand { get; }
    public ICommand AnnullaAggiungiMacchinaCommand  { get; }
    public ICommand RimuoviCompatibilitaCommand     { get; }
    public ICommand AprirDisegnoCommand             { get; }

    // ─── Caricamento ─────────────────────────────────────────────

    private async Task LoadAsync()
    {
        var categorie = await _categorieRepo.GetAllAsync();
        foreach (var c in categorie) CategoriePiastre.Add(c);
        OnPropertyChanged(nameof(CategorieFiltro));

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

        var catFiltro = FiltroCategoriaSelezionato == "Tutti"
            ? null
            : CategoriePiastre.FirstOrDefault(c => c.Descrizione == FiltroCategoriaSelezionato);

        var f = FiltroRicerca.Trim().ToLower();

        PiastreFiltrate.Clear();
        foreach (var p in _tutti.Where(p =>
            (statoFiltro is null || p.Stato == statoFiltro)
            && (catFiltro is null || p.IdCategoriaPiastra == catFiltro.IdCategoriaPiastra)
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
        _idPiastraInModifica     = PiastraSelezionata.IdPiastra;
        FormCodicePiastra        = PiastraSelezionata.CodicePiastra;
        FormCodiceArticolo       = PiastraSelezionata.CodiceArticoloGestionale  ?? string.Empty;
        FormDescrizione          = PiastraSelezionata.Descrizione                ?? string.Empty;
        FormStato                = PiastraSelezionata.Stato;
        FormCategoriaSelezionata = CategoriePiastre.FirstOrDefault(c => c.IdCategoriaPiastra == PiastraSelezionata.IdCategoriaPiastra);
        FormLarghezza            = PiastraSelezionata.LarghezzaMm?.ToString("F1")  ?? string.Empty;
        FormAltezza              = PiastraSelezionata.AltezzaMm?.ToString("F1")    ?? string.Empty;
        FormSpessore             = PiastraSelezionata.SpessoreMm?.ToString("F2")   ?? string.Empty;
        FormDurezza              = PiastraSelezionata.Durezza?.ToString("F1")      ?? string.Empty;
        FormPeso                 = PiastraSelezionata.Peso?.ToString("F3")         ?? string.Empty;
        FormNote                 = PiastraSelezionata.Note                          ?? string.Empty;
        ErroreCodiceDuplicato    = null;
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
        FormLarghezza = FormAltezza = FormSpessore = FormDurezza = FormPeso = string.Empty;
        FormStato                = StatoPiastra.Attiva;
        FormCategoriaSelezionata = null;
        ErroreCodiceDuplicato    = null;
        PercorsoDisegnoPendente  = null;
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
            p.IdCategoriaPiastra       = FormCategoriaSelezionata?.IdCategoriaPiastra;
            p.Categoria                = FormCategoriaSelezionata;
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
                IdCategoriaPiastra       = FormCategoriaSelezionata?.IdCategoriaPiastra,
                Categoria                = FormCategoriaSelezionata,
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
        AggiornaFiltro();
        ChiudiForm();
        PiastraSelezionata = piastraSalvata;

        if (!string.IsNullOrEmpty(filePendente))
            await AssociaDisegnoAsync(piastraSalvata, filePendente);
    }

    // ─── Eliminazione logica ─────────────────────────────────────

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

    // ─── Aggiungi / rimuovi macchina compatibile ─────────────────

    private async Task ApriAggiungiMacchinaAsync()
    {
        var tutte       = await _macchineRepo.GetAllAsync();
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

    private static string?  N(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static decimal? ParseDecimal(string s) =>
        decimal.TryParse(s.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
}
