using System.Collections.ObjectModel;
using System.Windows.Input;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Wpf.Commands;

namespace PlateArchive.Wpf.ViewModels;

public class MacchineViewModel : ViewModelBase
{
    private readonly IMacchinaStandardRepository _macchineRepo;
    private readonly ICompatibilitaRepository    _compatRepo;
    private readonly IClienteMacchinaRepository  _clientiMacchineRepo;

    private readonly ObservableCollection<MacchinaStandard> _tutti = [];

    private string  _filtroRicerca        = string.Empty;
    private bool    _soloAttive           = true;
    private MacchinaStandard? _macchinaSelezionata;
    private bool    _isFormVisible;
    private bool    _isModifica;
    private int     _idMacchinaInModifica;

    // Campi form
    private string _formCodiceMacchina = string.Empty;
    private string _formNomeMacchina   = string.Empty;
    private string _formFamiglia       = string.Empty;
    private string _formFormato        = string.Empty;
    private string _formVersione       = string.Empty;
    private string _formProduttore     = string.Empty;
    private string _formNote           = string.Empty;
    private string? _avvisoDuplicato;

    private CancellationTokenSource? _debounceCts;

    public MacchineViewModel(
        IMacchinaStandardRepository macchineRepo,
        ICompatibilitaRepository    compatRepo,
        IClienteMacchinaRepository  clientiMacchineRepo)
    {
        _macchineRepo        = macchineRepo;
        _compatRepo          = compatRepo;
        _clientiMacchineRepo = clientiMacchineRepo;

        NuovaCommand         = new RelayCommand(_ => ApriFormNuova());
        ModificaCommand      = new RelayCommand(_ => ApriFormModifica(),  _ => MacchinaSelezionata is not null);
        SalvaCommand         = new RelayCommand(async _ => await SalvaAsync());
        AnnullaFormCommand   = new RelayCommand(_ => ChiudiForm());
        ToggleAttivaCommand  = new RelayCommand(async _ => await ToggleAttivaAsync(), _ => MacchinaSelezionata is not null);

        _ = LoadAsync();
    }

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

    // IsDetailVisible = macchina selezionata AND form non visibile
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

    public string FormFamiglia
    {
        get => _formFamiglia;
        set => SetField(ref _formFamiglia, value);
    }

    public string FormFormato
    {
        get => _formFormato;
        set => SetField(ref _formFormato, value);
    }

    public string FormVersione
    {
        get => _formVersione;
        set => SetField(ref _formVersione, value);
    }

    public string FormProduttore
    {
        get => _formProduttore;
        set => SetField(ref _formProduttore, value);
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
        var macchine = await _macchineRepo.GetAllAsync();
        foreach (var m in macchine) _tutti.Add(m);
        AggiornaFiltro();
    }

    private async Task LoadDettaglioAsync()
    {
        PiastreCompatibili.Clear();
        ClientiAssociati.Clear();
        if (MacchinaSelezionata is null) return;

        var id = MacchinaSelezionata.IdMacchinaStandard;
        var (piastre, clienti) = (
            await _compatRepo.GetByMacchinaAsync(id),
            await _clientiMacchineRepo.GetByMacchinaAsync(id));

        foreach (var p in piastre)   PiastreCompatibili.Add(p);
        foreach (var c in clienti)   ClientiAssociati.Add(c);
    }

    private void AggiornaFiltro()
    {
        MacchineFiltrate.Clear();
        var f = FiltroRicerca.Trim().ToLower();
        foreach (var m in _tutti.Where(m =>
            (!SoloAttive || m.Attiva)
            && (string.IsNullOrEmpty(f)
                || m.CodiceMacchina.ToLower().Contains(f)
                || m.NomeMacchina.ToLower().Contains(f)
                || (m.Famiglia?.ToLower().Contains(f) ?? false))))
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
        _idMacchinaInModifica = MacchinaSelezionata.IdMacchinaStandard;
        FormCodiceMacchina = MacchinaSelezionata.CodiceMacchina;
        FormNomeMacchina   = MacchinaSelezionata.NomeMacchina;
        FormFamiglia       = MacchinaSelezionata.Famiglia   ?? string.Empty;
        FormFormato        = MacchinaSelezionata.Formato    ?? string.Empty;
        FormVersione       = MacchinaSelezionata.Versione   ?? string.Empty;
        FormProduttore     = MacchinaSelezionata.Produttore ?? string.Empty;
        FormNote           = MacchinaSelezionata.Note       ?? string.Empty;
        AvvisoDuplicato    = null;
        IsModifica         = true;
        IsFormVisible      = true;
    }

    private void ChiudiForm()
    {
        IsFormVisible = false;
        ResetForm();
    }

    private void ResetForm()
    {
        FormCodiceMacchina = FormNomeMacchina = FormFamiglia =
        FormFormato = FormVersione = FormProduttore = FormNote = string.Empty;
        AvvisoDuplicato = null;
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
            m.Famiglia       = N(FormFamiglia);
            m.Formato        = N(FormFormato);
            m.Versione       = N(FormVersione);
            m.Produttore     = N(FormProduttore);
            m.Note           = N(FormNote);
            await _macchineRepo.UpdateAsync(m);
            MacchinaSelezionata = m;
        }
        else
        {
            var nuova = new MacchinaStandard
            {
                CodiceMacchina = FormCodiceMacchina.Trim(),
                NomeMacchina   = FormNomeMacchina.Trim(),
                Famiglia       = N(FormFamiglia),
                Formato        = N(FormFormato),
                Versione       = N(FormVersione),
                Produttore     = N(FormProduttore),
                Note           = N(FormNote),
                Attiva         = true
            };
            await _macchineRepo.AddAsync(nuova);
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

    private static string? N(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
