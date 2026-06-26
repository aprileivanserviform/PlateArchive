using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Wpf.Commands;

namespace PlateArchive.Wpf.ViewModels;

public class MacchineViewModel : ViewModelBase
{
    private readonly IMacchinaStandardRepository     _macchineRepo;
    private readonly ICompatibilitaRepository        _compatRepo;
    private readonly IClienteMacchinaRepository      _clientiMacchineRepo;
    private readonly IFormatoMacchinaRepository      _formatiRepo;
    private readonly IProduttoreMacchinaRepository   _produttoriRepo;

    private readonly ObservableCollection<MacchinaStandard> _tutti = [];

    private string  _filtroRicerca  = string.Empty;
    private bool    _soloAttive     = true;
    private string  _filtroFormato  = "Tutti";
    private MacchinaStandard? _macchinaSelezionata;
    private bool    _isFormVisible;
    private bool    _isModifica;
    private int     _idMacchinaInModifica;

    // Campi form
    private string              _formCodiceMacchina      = string.Empty;
    private string              _formNomeMacchina        = string.Empty;
    private FormatoMacchina?    _formFormatoSelezionato;
    private ProduttoreMacchina? _formProduttoreSelezionato;
    private string              _formLarghezza           = string.Empty;
    private string              _formAltezza             = string.Empty;
    private string              _formVersione            = string.Empty;
    private string              _formNote                = string.Empty;
    private string?             _avvisoDuplicato;

    private CancellationTokenSource? _debounceCts;

    public MacchineViewModel(
        IMacchinaStandardRepository   macchineRepo,
        ICompatibilitaRepository      compatRepo,
        IClienteMacchinaRepository    clientiMacchineRepo,
        IFormatoMacchinaRepository    formatiRepo,
        IProduttoreMacchinaRepository produttoriRepo)
    {
        _macchineRepo        = macchineRepo;
        _compatRepo          = compatRepo;
        _clientiMacchineRepo = clientiMacchineRepo;
        _formatiRepo         = formatiRepo;
        _produttoriRepo      = produttoriRepo;

        NuovaCommand        = new RelayCommand(_ => ApriFormNuova());
        ModificaCommand     = new RelayCommand(_ => ApriFormModifica(),  _ => MacchinaSelezionata is not null);
        SalvaCommand        = new RelayCommand(async _ => await SalvaAsync());
        AnnullaFormCommand  = new RelayCommand(_ => ChiudiForm());
        ToggleAttivaCommand = new RelayCommand(async _ => await ToggleAttivaAsync(), _ => MacchinaSelezionata is not null);

        _ = LoadAsync();
    }

    // ─── Lookup ──────────────────────────────────────────────────

    public ObservableCollection<FormatoMacchina>    FormatiMacchine    { get; } = [];
    public ObservableCollection<ProduttoreMacchina> ProduttoriMacchine { get; } = [];

    public IEnumerable<string> FormatiFiltro =>
        Enumerable.Prepend(FormatiMacchine.Select(f => f.NomeFormato), "Tutti");

    // ─── Proprietà lista ─────────────────────────────────────────

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

    public ObservableCollection<MacchinaStandard> MacchineFiltrate { get; } = [];

    public MacchinaStandard? MacchinaSelezionata
    {
        get => _macchinaSelezionata;
        set
        {
            if (SetField(ref _macchinaSelezionata, value))
            {
                OnPropertyChanged(nameof(IsDetailVisible));
                OnPropertyChanged(nameof(ToggleAttivaLabel));
                if (IsFormVisible && IsModifica && value is not null)
                    ApriFormModifica();
                _ = LoadDettaglioAsync();
            }
        }
    }

    // ─── Proprietà pannello dettaglio ────────────────────────────

    public ObservableCollection<PiastraMacchinaCompatibile> PiastreCompatibili { get; } = [];
    public ObservableCollection<ClienteMacchina>            ClientiAssociati   { get; } = [];

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

    public bool IsDetailVisible => MacchinaSelezionata is not null && !IsFormVisible;
    public string FormTitolo        => IsModifica ? "Modifica macchina" : "Nuova macchina";
    public string ToggleAttivaLabel => MacchinaSelezionata?.Attiva == true ? "Disabilita" : "Abilita";

    public string FormCodiceMacchina
    {
        get => _formCodiceMacchina;
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

    public bool IsAvvisoDuplicatoVisible => !string.IsNullOrEmpty(_avvisoDuplicato);

    // ─── Comandi ─────────────────────────────────────────────────

    public ICommand NuovaCommand        { get; }
    public ICommand ModificaCommand     { get; }
    public ICommand SalvaCommand        { get; }
    public ICommand AnnullaFormCommand  { get; }
    public ICommand ToggleAttivaCommand { get; }

    // ─── Caricamento ─────────────────────────────────────────────

    private async Task LoadAsync()
    {
        var formati    = await _formatiRepo.GetAllAsync();
        var produttori = await _produttoriRepo.GetAllAsync();
        var macchine   = await _macchineRepo.GetAllAsync();

        foreach (var f in formati)    FormatiMacchine.Add(f);
        foreach (var p in produttori) ProduttoriMacchine.Add(p);
        foreach (var m in macchine)   _tutti.Add(m);

        OnPropertyChanged(nameof(FormatiFiltro));
        AggiornaFiltro();
    }

    private async Task LoadDettaglioAsync()
    {
        PiastreCompatibili.Clear();
        ClientiAssociati.Clear();
        if (MacchinaSelezionata is null) return;

        var id = MacchinaSelezionata.IdMacchinaStandard;
        var piastre = await _compatRepo.GetByMacchinaAsync(id);
        var clienti = await _clientiMacchineRepo.GetByMacchinaAsync(id);

        foreach (var p in piastre) PiastreCompatibili.Add(p);
        foreach (var c in clienti) ClientiAssociati.Add(c);
    }

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

    // ─── Controllo duplicati (debounce 300 ms) ───────────────────

    private async Task ControllaDuplicatoAsync(string codice)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        try
        {
            await Task.Delay(300, _debounceCts.Token);
            if (string.IsNullOrWhiteSpace(codice)) { AvvisoDuplicato = null; return; }
            var norm = Normalizza(codice);
            var simile = _tutti.FirstOrDefault(m =>
                Normalizza(m.CodiceMacchina) == norm
                && m.IdMacchinaStandard != _idMacchinaInModifica);
            AvvisoDuplicato = simile is not null
                ? $"Attenzione: esiste già '{simile.CodiceMacchina}'. Procedere comunque?"
                : null;
        }
        catch (OperationCanceledException) { }
    }

    private static string Normalizza(string s) =>
        s.ToLower().Replace(" ", "").Replace("_", "").Replace("-", "");

    // ─── Gestione form ───────────────────────────────────────────

    private void ApriFormNuova()
    {
        _idMacchinaInModifica = 0;
        IsModifica = false;
        ResetForm();
        IsFormVisible = true;
    }

    private void ApriFormModifica()
    {
        if (MacchinaSelezionata is null) return;
        _idMacchinaInModifica      = MacchinaSelezionata.IdMacchinaStandard;
        FormCodiceMacchina         = MacchinaSelezionata.CodiceMacchina;
        FormNomeMacchina           = MacchinaSelezionata.NomeMacchina;
        FormFormatoSelezionato     = FormatiMacchine.FirstOrDefault(f => f.IdFormato    == MacchinaSelezionata.IdFormato);
        FormProduttoreSelezionato  = ProduttoriMacchine.FirstOrDefault(p => p.IdProduttore == MacchinaSelezionata.IdProduttore);
        FormLarghezza              = MacchinaSelezionata.LarghezzaMm?.ToString("F2") ?? string.Empty;
        FormAltezza                = MacchinaSelezionata.AltezzaMm?.ToString("F2")   ?? string.Empty;
        FormVersione               = MacchinaSelezionata.Versione ?? string.Empty;
        FormNote                   = MacchinaSelezionata.Note    ?? string.Empty;
        AvvisoDuplicato            = null;
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
        AggiornaFiltro();
    }

    private static decimal? ParseDecimal(string s) =>
        decimal.TryParse(s.Replace(',', '.'), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;

    private static string? N(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
