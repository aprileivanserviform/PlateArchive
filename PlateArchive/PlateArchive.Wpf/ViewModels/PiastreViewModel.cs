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

/// <summary>
/// ViewModel della schermata Piastre — entità centrale del sistema.
/// Layout: lista con 4 filtri (ricerca, stato, categoria, formato) | pannello destra.
/// <para>
/// Il pannello destra mostra alternativamente:
/// - <b>Form creazione/modifica</b> quando <see cref="IsFormVisible"/> = true
/// - <b>Dettaglio piastra</b> quando <see cref="IsDetailVisible"/> = true
/// </para>
/// Il dettaglio include macchine compatibili (con "+ Aggiungi") e clienti associati.
/// La piastra può avere un disegno tecnico collegato, associabile via drag &amp; drop.
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

    // Lista completa in memoria — filtrata in PiastreFiltrate.
    private readonly ObservableCollection<Piastra> _tutti = [];

    private string    _filtroRicerca              = string.Empty;
    private string    _filtroStatoSelezionato     = "Tutti";
    private string    _filtroCategoriaSelezionato = "Tutti";
    private string    _filtroFormatoSelezionato   = "Tutti";
    private Piastra?  _piastraSelezionata;
    private bool      _isFormVisible;
    private bool      _isModifica;
    private int       _idPiastraInModifica;

    // Campi del form creazione/modifica
    private string             _formCodicePiastra        = string.Empty;
    private string             _formCodiceArticolo       = string.Empty;
    private string             _formDescrizione          = string.Empty;
    private StatoPiastra       _formStato                = StatoPiastra.Attiva;
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
    // File disegno trascinato sulla riga nel form: viene archiviato dopo il salvataggio della piastra.
    private string?            _percorsoDisegnoPendente;

    // Aggiungi macchina compatibile (pannello inline nel dettaglio)
    private bool              _isAggiungiMacchinaVisible;
    private MacchinaStandard? _macchinaCompatibileDaAggiungere;

    public PiastreViewModel(
        IPiastraRepository          piastreRepo,
        ICompatibilitaRepository    compatRepo,
        IClientePiastraRepository   clientiPiastreRepo,
        IMacchinaStandardRepository macchineRepo,
        IDisegnoRepository          disegniRepo,
        IFileArchivioService        fileArchivio,
        ICategoriaPiastraRepository categorieRepo,
        IFormatoMacchinaRepository  formatiRepo)
    {
        _piastreRepo        = piastreRepo;
        _compatRepo         = compatRepo;
        _clientiPiastreRepo = clientiPiastreRepo;
        _macchineRepo       = macchineRepo;
        _disegniRepo        = disegniRepo;
        _fileArchivio       = fileArchivio;
        _categorieRepo      = categorieRepo;
        _formatiRepo        = formatiRepo;

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

    // ─── Lookup per i ComboBox ────────────────────────────────────────────────

    /// <summary>Categorie piastre — usate per il ComboBox filtro e nel form.</summary>
    public ObservableCollection<CategoriaPiastra> CategoriePiastre { get; } = [];
    public ObservableCollection<FormatoMacchina>  FormatiMacchine  { get; } = [];

    /// <summary>"Tutti" + nomi categorie — per il ComboBox filtro nella lista.</summary>
    public IEnumerable<string> CategorieFiltro =>
        Enumerable.Prepend(CategoriePiastre.Select(c => c.Descrizione), "Tutti");

    public IEnumerable<string> FormatiFiltro =>
        Enumerable.Prepend(FormatiMacchine.Select(f => f.NomeFormato), "Tutti");

    // ─── Filtri lista ─────────────────────────────────────────────────────────

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

    public IEnumerable<string>       StatiFiltro  { get; } = ["Tutti", "Attiva", "Obsoleta", "Da verificare"];
    public IEnumerable<StatoPiastra> StatiPiastra { get; } = Enum.GetValues<StatoPiastra>();

    /// <summary>Subset filtrato di _tutti — bound alla DataGrid.</summary>
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
                OnPropertyChanged(nameof(IsDisegnoPresente));
                OnPropertyChanged(nameof(IsDisegnoAssente));
                // Se il form di modifica era aperto, aggiorna i campi con la nuova selezione.
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

    // ─── Pannello dettaglio ───────────────────────────────────────────────────

    /// <summary>Macchine con cui questa piastra è tecnicamente compatibile.</summary>
    public ObservableCollection<PiastraMacchinaCompatibile> MacchineCompatibili { get; } = [];

    /// <summary>Clienti che possiedono questa piastra (associazione commerciale).</summary>
    public ObservableCollection<ClientePiastra>             ClientiAssociati    { get; } = [];

    /// <summary>Macchine disponibili da aggiungere come compatibili (già escluse quelle associate).</summary>
    public ObservableCollection<MacchinaStandard>           MacchineDisponibili { get; } = [];

    public bool IsDisegnoPresente => PiastraSelezionata?.Disegno is not null;
    public bool IsDisegnoAssente  => PiastraSelezionata?.Disegno is null;

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

    // ─── Stato pannello destra ────────────────────────────────────────────────

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

    // ─── Campi form creazione/modifica ────────────────────────────────────────

    public string FormCodicePiastra
    {
        get => _formCodicePiastra;
        // Controllo duplicati real-time ad ogni carattere.
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

    /// <summary>Blocca il salvataggio se il codice è già presente in _tutti.</summary>
    public bool IsErroreVisible => !string.IsNullOrEmpty(_erroreCodiceDuplicato);

    /// <summary>
    /// Percorso del file disegno trascinato nel form (non ancora salvato).
    /// Viene passato ad <see cref="AssociaDisegnoAsync"/> dopo il salvataggio della piastra,
    /// in modo che l'IdPiastra sia già disponibile.
    /// </summary>
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
    /// <summary>Solo il nome del file (senza percorso) — mostrato nel form come anteprima.</summary>
    public string? NomeFilePendente         => Path.GetFileName(_percorsoDisegnoPendente);

    // ─── Comandi ─────────────────────────────────────────────────────────────

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

    // ─── Caricamento ─────────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        var categorie = await _categorieRepo.GetAllAsync();
        foreach (var c in categorie) CategoriePiastre.Add(c);
        // Notifica CategorieFiltro perché è calcolata da CategoriePiastre.
        OnPropertyChanged(nameof(CategorieFiltro));

        var formati = await _formatiRepo.GetAllAsync();
        foreach (var f in formati) FormatiMacchine.Add(f);
        OnPropertyChanged(nameof(FormatiFiltro));

        var piastre = await _piastreRepo.GetAllAsync();
        foreach (var p in piastre) _tutti.Add(p);
        AggiornaFiltro();
    }

    /// <summary>Carica macchine compatibili e clienti per la piastra selezionata.</summary>
    private async Task LoadDettaglioAsync()
    {
        MacchineCompatibili.Clear();
        ClientiAssociati.Clear();
        if (PiastraSelezionata is null) return;

        var id = PiastraSelezionata.IdPiastra;
        // Le due query sono indipendenti — eseguite in parallelo per velocità.
        var (macchine, clienti) = (
            await _compatRepo.GetByPiastraAsync(id),
            await _clientiPiastreRepo.GetByPiastraAsync(id));

        foreach (var m in macchine) MacchineCompatibili.Add(m);
        foreach (var c in clienti)  ClientiAssociati.Add(c);
    }

    // ─── Filtro lista (in memoria, senza query al DB) ─────────────────────────

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

        var fmtFiltro = FiltroFormatoSelezionato == "Tutti"
            ? null
            : FormatiMacchine.FirstOrDefault(f => f.NomeFormato == FiltroFormatoSelezionato);

        var f = FiltroRicerca.Trim().ToLower();

        PiastreFiltrate.Clear();
        foreach (var p in _tutti.Where(p =>
            (statoFiltro is null || p.Stato == statoFiltro)
            && (catFiltro is null || p.IdCategoriaPiastra == catFiltro.IdCategoriaPiastra)
            && (fmtFiltro is null || p.IdFormato == fmtFiltro.IdFormato)
            && (string.IsNullOrEmpty(f)
                || p.CodicePiastra.ToLower().Contains(f)
                || (p.CodiceArticoloGestionale?.ToLower().Contains(f) ?? false)
                || (p.Descrizione?.ToLower().Contains(f) ?? false))))
        {
            PiastreFiltrate.Add(p);
        }
    }

    // ─── Validazione duplicato codice ─────────────────────────────────────────

    private void ControllaDuplicato(string codice)
    {
        if (string.IsNullOrWhiteSpace(codice)) { ErroreCodiceDuplicato = null; return; }
        // Esclude sé stesso durante la modifica (_idPiastraInModifica = 0 per nuove piastre).
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
        _idPiastraInModifica = 0;  // 0 = nuova piastra (nessun ID da escludere dalla validazione)
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
        FormFormatoSelezionato   = FormatiMacchine.FirstOrDefault(f => f.IdFormato == PiastraSelezionata.IdFormato);
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
        FormFormatoSelezionato   = null;
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
            // Aggiorna anche le navigazioni per il binding nella lista.
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

        // Salva il percorso pendente prima di ChiudiForm (che lo azzera).
        var filePendente = _percorsoDisegnoPendente;
        AggiornaFiltro();
        ChiudiForm();
        PiastraSelezionata = piastraSalvata;

        // Associa il disegno dopo aver salvato (serve IdPiastra per il record Disegno).
        if (!string.IsNullOrEmpty(filePendente))
            await AssociaDisegnoAsync(piastraSalvata, filePendente);
    }

    // ─── Eliminazione logica ──────────────────────────────────────────────────

    private async Task EliminaAsync()
    {
        if (PiastraSelezionata is null) return;

        // Blocca l'eliminazione se la piastra è associata a clienti (integrità commerciale).
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

        // Se ha macchine compatibili, avvisa ma NON blocca (compatibilità è eliminabile).
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

        // Soft-delete: imposta IsEliminata = true. EF query filter la escluderà automaticamente.
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
        // Se la piastra ha un formato, mostra solo le macchine dello stesso formato.
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
        // Ricarica il dettaglio per includere la nuova compatibilità.
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
        // Ricarica invece di rimuovere in memoria: garantisce coerenza con il DB.
        await LoadDettaglioAsync();
    }

    // ─── Apertura file disegno ────────────────────────────────────────────────

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
            // UseShellExecute = true apre con l'applicazione predefinita del SO (AutoCAD, Adobe, ecc.)
            Process.Start(new ProcessStartInfo(percorso) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ErroreDisegno = $"Impossibile aprire il file: {ex.Message}";
        }
    }

    // ─── Associazione disegno via drag & drop ─────────────────────────────────

    /// <summary>
    /// Archivia il file nel percorso di archivio (FileArchivioService) e crea/aggiorna
    /// il record Disegno associato alla piastra.
    /// Chiamato da PiastreView.xaml.cs quando l'utente trascina un file sulla riga
    /// (drag &amp; drop), oppure dopo il salvataggio se era presente un file pendente.
    /// </summary>
    public async Task AssociaDisegnoAsync(Piastra piastra, string percorsoFile)
    {
        // Archivia il file (copia nella cartella archivio) — se fallisce usa il percorso originale.
        var percorsoEffettivo = await _fileArchivio.ArchiviaDisegnoAsync(percorsoFile, piastra.CodicePiastra)
                                ?? percorsoFile;

        var formato = Path.GetExtension(percorsoEffettivo).TrimStart('.').ToUpper();

        if (piastra.Disegno is null)
        {
            // Prima associazione: crea un nuovo record Disegno.
            var nuovoDisegno = new Disegno
            {
                IdPiastra              = piastra.IdPiastra,
                CodiceDisegno          = piastra.CodicePiastra,
                NomeFile               = Path.GetFileName(percorsoEffettivo),
                PercorsoFile           = percorsoEffettivo,
                Formato                = formato,
                // Il disegno appena aggiunto è "Da verificare" fino a revisione manuale.
                Stato                  = StatoDisegno.DaVerificare,
                DataUltimaModificaFile = DateTime.UtcNow
            };
            await _disegniRepo.AddAsync(nuovoDisegno);
            piastra.Disegno = nuovoDisegno;
        }
        else
        {
            // Sostituzione: aggiorna il record esistente con il nuovo file.
            piastra.Disegno.NomeFile               = Path.GetFileName(percorsoEffettivo);
            piastra.Disegno.PercorsoFile           = percorsoEffettivo;
            piastra.Disegno.Formato                = formato;
            piastra.Disegno.DataUltimaModificaFile = DateTime.UtcNow;
            await _disegniRepo.UpdateAsync(piastra.Disegno);
        }

        // Notifica la View solo se la piastra modificata è quella selezionata.
        if (PiastraSelezionata == piastra)
        {
            OnPropertyChanged(nameof(PiastraSelezionata));
            OnPropertyChanged(nameof(IsDisegnoPresente));
            OnPropertyChanged(nameof(IsDisegnoAssente));
        }
    }

    // ─── Utility ─────────────────────────────────────────────────────────────

    private static string?  N(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static decimal? ParseDecimal(string s) =>
        decimal.TryParse(s.Replace(',', '.'),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;
}
