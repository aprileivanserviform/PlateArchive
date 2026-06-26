using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Wpf.Commands;

namespace PlateArchive.Wpf.ViewModels;

/// <summary>
/// ViewModel della schermata Macchine (catalogo modelli standard).
/// Layout: lista a sinistra con filtri | pannello destra (dettaglio OPPURE form CRUD).
/// <para>
/// Il pannello destra mostra alternativamente:
/// - <b>Form creazione/modifica</b> quando <see cref="IsFormVisible"/> = true
/// - <b>Dettaglio macchina</b> quando <see cref="IsDetailVisible"/> = true (selezione + no form)
/// </para>
/// Il dettaglio include le piastre compatibili (con pulsante "+ Aggiungi" per aggiungerne)
/// e i clienti che possiedono quel modello (solo lettura).
/// </summary>
public class MacchineViewModel : ViewModelBase
{
    private readonly IMacchinaStandardRepository   _macchineRepo;
    private readonly ICompatibilitaRepository      _compatRepo;
    private readonly IClienteMacchinaRepository    _clientiMacchineRepo;
    private readonly IFormatoMacchinaRepository    _formatiRepo;
    private readonly IProduttoreMacchinaRepository _produttoriRepo;
    private readonly IPiastraRepository            _piastreRepo;

    // Lista completa in memoria — filtrata in MacchineFiltrate.
    private readonly ObservableCollection<MacchinaStandard> _tutti = [];

    private string  _filtroRicerca  = string.Empty;
    private bool    _soloAttive     = true;
    private string  _filtroFormato  = "Tutti";
    private MacchinaStandard? _macchinaSelezionata;
    private bool    _isFormVisible;
    private bool    _isModifica;
    private int     _idMacchinaInModifica;

    // Campi del form creazione/modifica
    private string              _formCodiceMacchina      = string.Empty;
    private string              _formNomeMacchina        = string.Empty;
    private FormatoMacchina?    _formFormatoSelezionato;
    private ProduttoreMacchina? _formProduttoreSelezionato;
    private string              _formLarghezza           = string.Empty;
    private string              _formAltezza             = string.Empty;
    private string              _formVersione            = string.Empty;
    private string              _formNote                = string.Empty;
    private string?             _avvisoDuplicato;

    // Aggiunta piastra compatibile (pannello inline nel dettaglio)
    private bool     _isAggiungiPiastraVisible;
    private Piastra? _piastraCompatibileDaAggiungere;

    private CancellationTokenSource? _debounceCts;

    public MacchineViewModel(
        IMacchinaStandardRepository   macchineRepo,
        ICompatibilitaRepository      compatRepo,
        IClienteMacchinaRepository    clientiMacchineRepo,
        IFormatoMacchinaRepository    formatiRepo,
        IProduttoreMacchinaRepository produttoriRepo,
        IPiastraRepository            piastreRepo)
    {
        _macchineRepo        = macchineRepo;
        _compatRepo          = compatRepo;
        _clientiMacchineRepo = clientiMacchineRepo;
        _formatiRepo         = formatiRepo;
        _produttoriRepo      = produttoriRepo;
        _piastreRepo         = piastreRepo;

        NuovaCommand        = new RelayCommand(_ => ApriFormNuova());
        ModificaCommand     = new RelayCommand(_ => ApriFormModifica(),  _ => MacchinaSelezionata is not null);
        SalvaCommand        = new RelayCommand(async _ => await SalvaAsync());
        AnnullaFormCommand  = new RelayCommand(_ => ChiudiForm());
        ToggleAttivaCommand = new RelayCommand(async _ => await ToggleAttivaAsync(), _ => MacchinaSelezionata is not null);

        AggiungiPiastraCommand         = new RelayCommand(async _ => await ApriAggiungiPiastraAsync(), _ => MacchinaSelezionata is not null);
        ConfermaAggiungiPiastraCommand = new RelayCommand(async _ => await ConfermaAggiungiPiastraAsync(), _ => PiastraCompatibileDaAggiungere is not null);
        AnnullaAggiungiPiastraCommand  = new RelayCommand(_ => AnnullaAggiungiPiastra());
        RimuoviCompatibilitaCommand    = new RelayCommand(async o => await RimuoviCompatibilitaAsync(o));

        _ = LoadAsync();
    }

    // ─── Lookup per i ComboBox del form ──────────────────────────────────────

    /// <summary>Tutti i formati non eliminati — usati nel ComboBox Formato del form.</summary>
    public ObservableCollection<FormatoMacchina>    FormatiMacchine    { get; } = [];
    public ObservableCollection<ProduttoreMacchina> ProduttoriMacchine { get; } = [];

    /// <summary>Valori per il ComboBox filtro formato nella lista: "Tutti" + nomi formati.</summary>
    public IEnumerable<string> FormatiFiltro =>
        Enumerable.Prepend(FormatiMacchine.Select(f => f.NomeFormato), "Tutti");

    // ─── Filtri lista ─────────────────────────────────────────────────────────

    public string FiltroRicerca
    {
        get => _filtroRicerca;
        set { if (SetField(ref _filtroRicerca, value)) AggiornaFiltro(); }
    }

    public bool SoloAttive
    {
        get => _soloAttive;
        set { if (SetField(ref _soloAttive, value)) AggiornaFiltro(); }
    }

    public string FiltroFormato
    {
        get => _filtroFormato;
        set { if (SetField(ref _filtroFormato, value)) AggiornaFiltro(); }
    }

    /// <summary>Subset filtrato di _tutti — bound alla DataGrid.</summary>
    public ObservableCollection<MacchinaStandard> MacchineFiltrate { get; } = [];

    // ─── Selezione ───────────────────────────────────────────────────────────

    public MacchinaStandard? MacchinaSelezionata
    {
        get => _macchinaSelezionata;
        set
        {
            if (SetField(ref _macchinaSelezionata, value))
            {
                OnPropertyChanged(nameof(IsDetailVisible));
                OnPropertyChanged(nameof(ToggleAttivaLabel));
                // Chiude il pannello "aggiungi piastra" se l'utente cambia macchina.
                if (IsAggiungiPiastraVisible) AnnullaAggiungiPiastra();
                // Se il form di modifica era aperto, aggiorna i campi con la nuova selezione.
                if (IsFormVisible && IsModifica && value is not null)
                    ApriFormModifica();
                _ = LoadDettaglioAsync();
            }
        }
    }

    // ─── Pannello dettaglio ───────────────────────────────────────────────────

    /// <summary>Piastre tecnicamente compatibili con la macchina selezionata.</summary>
    public ObservableCollection<PiastraMacchinaCompatibile> PiastreCompatibili { get; } = [];

    /// <summary>Clienti che possiedono la macchina selezionata (solo lettura).</summary>
    public ObservableCollection<ClienteMacchina>            ClientiAssociati   { get; } = [];

    /// <summary>Piastre disponibili da aggiungere come compatibili (già escluse quelle associate).</summary>
    public ObservableCollection<Piastra>                    PiastreDisponibili { get; } = [];

    // ─── Pannello aggiungi piastra compatibile ────────────────────────────────

    public bool IsAggiungiPiastraVisible
    {
        get => _isAggiungiPiastraVisible;
        set => SetField(ref _isAggiungiPiastraVisible, value);
    }

    public Piastra? PiastraCompatibileDaAggiungere
    {
        get => _piastraCompatibileDaAggiungere;
        set => SetField(ref _piastraCompatibileDaAggiungere, value);
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

    public bool   IsDetailVisible   => MacchinaSelezionata is not null && !IsFormVisible;
    public string FormTitolo        => IsModifica ? "Modifica macchina" : "Nuova macchina";
    public string ToggleAttivaLabel => MacchinaSelezionata?.Attiva == true ? "Disabilita" : "Abilita";

    // ─── Campi form creazione/modifica ────────────────────────────────────────

    public string FormCodiceMacchina
    {
        get => _formCodiceMacchina;
        // Debounce 300ms: non esegue la ricerca duplicati ad ogni tasto, ma aspetta la pausa.
        set { if (SetField(ref _formCodiceMacchina, value)) _ = ControllaDuplicatoAsync(value); }
    }

    public string FormNomeMacchina
    {
        get => _formNomeMacchina;
        set => SetField(ref _formNomeMacchina, value);
    }

    public FormatoMacchina? FormFormatoSelezionato
    {
        get => _formFormatoSelezionato;
        set => SetField(ref _formFormatoSelezionato, value);
    }

    public ProduttoreMacchina? FormProduttoreSelezionato
    {
        get => _formProduttoreSelezionato;
        set => SetField(ref _formProduttoreSelezionato, value);
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

    public string FormVersione
    {
        get => _formVersione;
        set => SetField(ref _formVersione, value);
    }

    public string FormNote
    {
        get => _formNote;
        set => SetField(ref _formNote, value);
    }

    public string? AvvisoDuplicato
    {
        get => _avvisoDuplicato;
        set
        {
            if (SetField(ref _avvisoDuplicato, value))
                OnPropertyChanged(nameof(IsAvvisoDuplicatoVisible));
        }
    }

    /// <summary>Mostra un banner di avviso (non blocca il salvataggio) se esiste un codice simile.</summary>
    public bool IsAvvisoDuplicatoVisible => !string.IsNullOrEmpty(_avvisoDuplicato);

    // ─── Comandi ─────────────────────────────────────────────────────────────

    public ICommand NuovaCommand                   { get; }
    public ICommand ModificaCommand                { get; }
    public ICommand SalvaCommand                   { get; }
    public ICommand AnnullaFormCommand             { get; }
    public ICommand ToggleAttivaCommand            { get; }
    public ICommand AggiungiPiastraCommand         { get; }
    public ICommand ConfermaAggiungiPiastraCommand { get; }
    public ICommand AnnullaAggiungiPiastraCommand  { get; }
    public ICommand RimuoviCompatibilitaCommand    { get; }

    // ─── Caricamento ─────────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        var formati    = await _formatiRepo.GetAllAsync();
        var produttori = await _produttoriRepo.GetAllAsync();
        var macchine   = await _macchineRepo.GetAllAsync();

        foreach (var f in formati)    FormatiMacchine.Add(f);
        foreach (var p in produttori) ProduttoriMacchine.Add(p);
        foreach (var m in macchine)   _tutti.Add(m);

        // Aggiorna FormatiFiltro perché dipende da FormatiMacchine.
        OnPropertyChanged(nameof(FormatiFiltro));
        AggiornaFiltro();
    }

    /// <summary>Carica le piastre compatibili e i clienti per la macchina attualmente selezionata.</summary>
    private async Task LoadDettaglioAsync()
    {
        PiastreCompatibili.Clear();
        ClientiAssociati.Clear();
        if (MacchinaSelezionata is null) return;

        var id      = MacchinaSelezionata.IdMacchinaStandard;
        var piastre = await _compatRepo.GetByMacchinaAsync(id);
        var clienti = await _clientiMacchineRepo.GetByMacchinaAsync(id);

        foreach (var p in piastre) PiastreCompatibili.Add(p);
        foreach (var c in clienti) ClientiAssociati.Add(c);
    }

    // ─── Filtro lista ─────────────────────────────────────────────────────────

    private void AggiornaFiltro()
    {
        MacchineFiltrate.Clear();
        var formatoFiltro = FiltroFormato == "Tutti"
            ? null
            : FormatiMacchine.FirstOrDefault(f => f.NomeFormato == FiltroFormato);

        var f = FiltroRicerca.Trim().ToLower();
        foreach (var m in _tutti.Where(m =>
            (!SoloAttive || m.Attiva)
            && (formatoFiltro is null || m.IdFormato == formatoFiltro.IdFormato)
            && (string.IsNullOrEmpty(f)
                || m.CodiceMacchina.ToLower().Contains(f)
                || m.NomeMacchina.ToLower().Contains(f)
                || (m.Formato?.NomeFormato.ToLower().Contains(f) ?? false))))
        {
            MacchineFiltrate.Add(m);
        }
    }

    // ─── Validazione codice duplicato (con debounce) ──────────────────────────

    private async Task ControllaDuplicatoAsync(string codice)
    {
        // Annulla il controllo precedente se l'utente continua a digitare.
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(300, _debounceCts.Token);
            if (string.IsNullOrWhiteSpace(codice)) { AvvisoDuplicato = null; return; }
            var norm   = Normalizza(codice);
            var simile = _tutti.FirstOrDefault(m =>
                Normalizza(m.CodiceMacchina) == norm
                && m.IdMacchinaStandard != _idMacchinaInModifica);
            AvvisoDuplicato = simile is not null
                ? $"Attenzione: esiste già '{simile.CodiceMacchina}'. Procedere comunque?"
                : null;
        }
        catch (OperationCanceledException) { /* annullato da un nuovo keystroke */ }
    }

    // Normalizza il codice per confronto fuzzy: ignora spazi, trattini, underscore e case.
    private static string Normalizza(string s) =>
        s.ToLower().Replace(" ", "").Replace("_", "").Replace("-", "");

    // ─── Gestione form creazione/modifica ─────────────────────────────────────

    private void ApriFormNuova()
    {
        _idMacchinaInModifica = 0;
        IsModifica  = false;
        ResetForm();
        IsFormVisible = true;
    }

    private void ApriFormModifica()
    {
        if (MacchinaSelezionata is null) return;
        _idMacchinaInModifica     = MacchinaSelezionata.IdMacchinaStandard;
        FormCodiceMacchina        = MacchinaSelezionata.CodiceMacchina;
        FormNomeMacchina          = MacchinaSelezionata.NomeMacchina;
        FormFormatoSelezionato    = FormatiMacchine.FirstOrDefault(f => f.IdFormato    == MacchinaSelezionata.IdFormato);
        FormProduttoreSelezionato = ProduttoriMacchine.FirstOrDefault(p => p.IdProduttore == MacchinaSelezionata.IdProduttore);
        FormLarghezza             = MacchinaSelezionata.LarghezzaMm?.ToString("F2") ?? string.Empty;
        FormAltezza               = MacchinaSelezionata.AltezzaMm?.ToString("F2")   ?? string.Empty;
        FormVersione              = MacchinaSelezionata.Versione ?? string.Empty;
        FormNote                  = MacchinaSelezionata.Note    ?? string.Empty;
        AvvisoDuplicato           = null;
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
        FormCodiceMacchina        = FormNomeMacchina = FormLarghezza =
        FormAltezza = FormVersione = FormNote        = string.Empty;
        FormFormatoSelezionato    = null;
        FormProduttoreSelezionato = null;
        AvvisoDuplicato           = null;
    }

    private async Task SalvaAsync()
    {
        if (string.IsNullOrWhiteSpace(FormCodiceMacchina) ||
            string.IsNullOrWhiteSpace(FormNomeMacchina)) return;

        if (IsModifica)
        {
            // Modifica in-place dell'oggetto già in _tutti (EF Core lo traccia).
            var m = _tutti.FirstOrDefault(x => x.IdMacchinaStandard == _idMacchinaInModifica);
            if (m is null) return;
            m.CodiceMacchina = FormCodiceMacchina.Trim();
            m.NomeMacchina   = FormNomeMacchina.Trim();
            m.IdFormato      = FormFormatoSelezionato?.IdFormato;
            m.IdProduttore   = FormProduttoreSelezionato?.IdProduttore;
            m.LarghezzaMm    = ParseDecimal(FormLarghezza);
            m.AltezzaMm      = ParseDecimal(FormAltezza);
            m.Versione       = N(FormVersione);
            m.Note           = N(FormNote);
            // Aggiorna anche le navigazioni in memoria per il binding nella lista.
            m.Formato        = FormFormatoSelezionato;
            m.Produttore     = FormProduttoreSelezionato;
            await _macchineRepo.UpdateAsync(m);
            MacchinaSelezionata = m;
        }
        else
        {
            var nuova = new MacchinaStandard
            {
                CodiceMacchina = FormCodiceMacchina.Trim(),
                NomeMacchina   = FormNomeMacchina.Trim(),
                IdFormato      = FormFormatoSelezionato?.IdFormato,
                IdProduttore   = FormProduttoreSelezionato?.IdProduttore,
                LarghezzaMm    = ParseDecimal(FormLarghezza),
                AltezzaMm      = ParseDecimal(FormAltezza),
                Versione       = N(FormVersione),
                Note           = N(FormNote),
                Attiva         = true
            };
            await _macchineRepo.AddAsync(nuova);
            nuova.Formato    = FormFormatoSelezionato;
            nuova.Produttore = FormProduttoreSelezionato;
            _tutti.Add(nuova);
            MacchinaSelezionata = nuova;
        }

        AggiornaFiltro();
        ChiudiForm();
    }

    private async Task ToggleAttivaAsync()
    {
        if (MacchinaSelezionata is null) return;
        MacchinaSelezionata.Attiva = !MacchinaSelezionata.Attiva;
        await _macchineRepo.UpdateAsync(MacchinaSelezionata);
        OnPropertyChanged(nameof(MacchinaSelezionata));
        OnPropertyChanged(nameof(ToggleAttivaLabel));
        // Ricalcola il filtro perché la macchina potrebbe sparire dalla lista (SoloAttive = true).
        AggiornaFiltro();
    }

    // ─── Aggiungi piastra compatibile ─────────────────────────────────────────

    private async Task ApriAggiungiPiastraAsync()
    {
        var tutte       = await _piastreRepo.GetAllAsync();
        // Esclude le piastre già compatibili.
        var idGiaCompat = PiastreCompatibili.Select(c => c.IdPiastra).ToHashSet();
        // Se la macchina ha un formato, mostra solo le piastre dello stesso formato.
        var idFormato   = MacchinaSelezionata?.IdFormato;

        PiastreDisponibili.Clear();
        foreach (var p in tutte.Where(p =>
            !idGiaCompat.Contains(p.IdPiastra)
            && (idFormato is null || p.IdFormato == idFormato)))
        {
            PiastreDisponibili.Add(p);
        }
        PiastraCompatibileDaAggiungere = null;
        IsAggiungiPiastraVisible       = true;
    }

    private async Task ConfermaAggiungiPiastraAsync()
    {
        if (MacchinaSelezionata is null || PiastraCompatibileDaAggiungere is null) return;
        var nuova = new PiastraMacchinaCompatibile
        {
            IdPiastra          = PiastraCompatibileDaAggiungere.IdPiastra,
            IdMacchinaStandard = MacchinaSelezionata.IdMacchinaStandard,
            Attiva             = true
        };
        await _compatRepo.AddAsync(nuova);
        AnnullaAggiungiPiastra();
        // Ricarica il dettaglio per includere la nuova compatibilità.
        await LoadDettaglioAsync();
    }

    private void AnnullaAggiungiPiastra()
    {
        IsAggiungiPiastraVisible       = false;
        PiastraCompatibileDaAggiungere = null;
    }

    private async Task RimuoviCompatibilitaAsync(object? param)
    {
        if (param is not PiastraMacchinaCompatibile c) return;
        await _compatRepo.DeleteAsync(c.IdCompatibilita);
        // Aggiorna la lista in memoria senza ricaricare tutto dal DB.
        PiastreCompatibili.Remove(c);
    }

    // ─── Utility ─────────────────────────────────────────────────────────────

    private static decimal? ParseDecimal(string s) =>
        decimal.TryParse(s.Replace(',', '.'), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;

    private static string? N(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
