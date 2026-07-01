using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Wpf.Commands;

namespace PlateArchive.Wpf.ViewModels;

/// <summary>
/// ViewModel della schermata "Categorie Piastre" (sezione Impostazioni).
/// Gestisce il CRUD della tabella lookup <see cref="CategoriaPiastra"/>:
/// ogni piastra appartiene a una categoria (es. Flessografica, Offset).
/// <para>
/// La cancellazione è fisica (hard delete), ma bloccata se esistono piastre associate.
/// </para>
/// </summary>
public class CategoriePiastreViewModel : ViewModelBase
{
    private readonly ICategoriaPiastraRepository _repo;

    private string          _formCodice      = string.Empty;
    private string          _formDescrizione = string.Empty;
    private string          _formOrdine      = string.Empty;
    private string?         _errore;
    private bool            _isFormVisible;
    private bool            _isModifica;
    private int             _idInModifica;
    private CategoriaPiastra? _selezionata;

    public CategoriePiastreViewModel(ICategoriaPiastraRepository repo)
    {
        _repo = repo;

        NuovoCommand    = new RelayCommand(_ => ApriFormNuovo());
        SalvaCommand    = new RelayCommand(async _ => await SalvaAsync(),
                            _ => !string.IsNullOrWhiteSpace(FormCodice)
                              && !string.IsNullOrWhiteSpace(FormDescrizione)
                              && !IsErroreVisible);
        AnnullaCommand  = new RelayCommand(_ => ChiudiForm());
        ModificaCommand = new RelayCommand(_ => ApriFormModifica(), _ => Selezionata is not null);
        EliminaCommand  = new RelayCommand(async _ => await EliminaAsync(), _ => Selezionata is not null);
    }

    // ─── Inizializzazione navigazione ─────────────────────────────────────────

    public override Task OnNavigatedAsync() => LoadAsync();

    // ─── Lista ───────────────────────────────────────────────────────────────

    public ObservableCollection<CategoriaPiastra> Categorie { get; } = [];

    public CategoriaPiastra? Selezionata
    {
        get => _selezionata;
        set
        {
            if (SetField(ref _selezionata, value))
                OnPropertyChanged(nameof(IsDetailVisible));
        }
    }

    // ─── Stato pannello destra ────────────────────────────────────────────────

    public bool IsFormVisible
    {
        get => _isFormVisible;
        set
        {
            if (SetField(ref _isFormVisible, value))
            {
                OnPropertyChanged(nameof(IsDetailVisible));
                OnPropertyChanged(nameof(FormTitolo));
            }
        }
    }

    public bool IsModifica
    {
        get => _isModifica;
        set { if (SetField(ref _isModifica, value)) OnPropertyChanged(nameof(FormTitolo)); }
    }

    public bool   IsDetailVisible => Selezionata is not null && !IsFormVisible;
    public string FormTitolo      => IsModifica ? "Modifica categoria" : "Nuova categoria";

    // ─── Campi form ───────────────────────────────────────────────────────────

    public string FormCodice
    {
        get => _formCodice;
        set { if (SetField(ref _formCodice, value)) ControllaDuplicato(); }
    }

    public string FormDescrizione
    {
        get => _formDescrizione;
        set { if (SetField(ref _formDescrizione, value)) ControllaDuplicato(); }
    }

    public string FormOrdine
    {
        get => _formOrdine;
        set => SetField(ref _formOrdine, value);
    }

    public string? Errore
    {
        get => _errore;
        set { if (SetField(ref _errore, value)) OnPropertyChanged(nameof(IsErroreVisible)); }
    }

    public bool IsErroreVisible => !string.IsNullOrEmpty(_errore);

    // ─── Comandi ─────────────────────────────────────────────────────────────

    public ICommand NuovoCommand    { get; }
    public ICommand SalvaCommand    { get; }
    public ICommand AnnullaCommand  { get; }
    public ICommand ModificaCommand { get; }
    public ICommand EliminaCommand  { get; }

    // ─── Caricamento ─────────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        var categorie = await _repo.GetAllAsync();
        Categorie.Clear();
        foreach (var c in categorie) Categorie.Add(c);
    }

    // ─── Validazione ──────────────────────────────────────────────────────────

    private void ControllaDuplicato()
    {
        if (string.IsNullOrWhiteSpace(FormCodice) && string.IsNullOrWhiteSpace(FormDescrizione))
        {
            Errore = null;
            return;
        }

        var dupCodice = Categorie.FirstOrDefault(c =>
            !string.IsNullOrWhiteSpace(FormCodice)
            && c.Codice.Equals(FormCodice.Trim(), StringComparison.OrdinalIgnoreCase)
            && c.IdCategoriaPiastra != _idInModifica);

        if (dupCodice is not null) { Errore = $"Codice '{dupCodice.Codice}' già presente."; return; }

        var dupDesc = Categorie.FirstOrDefault(c =>
            !string.IsNullOrWhiteSpace(FormDescrizione)
            && c.Descrizione.Equals(FormDescrizione.Trim(), StringComparison.OrdinalIgnoreCase)
            && c.IdCategoriaPiastra != _idInModifica);

        Errore = dupDesc is not null ? $"Descrizione '{dupDesc.Descrizione}' già presente." : null;
    }

    // ─── Gestione form ────────────────────────────────────────────────────────

    private void ApriFormNuovo()
    {
        _idInModifica    = 0;
        IsModifica       = false;
        FormCodice       = string.Empty;
        FormDescrizione  = string.Empty;
        FormOrdine       = Categorie.Count > 0 ? (Categorie.Max(c => c.Ordine) + 10).ToString() : "10";
        Errore           = null;
        IsFormVisible    = true;
    }

    private void ApriFormModifica()
    {
        if (Selezionata is null) return;
        _idInModifica   = Selezionata.IdCategoriaPiastra;
        FormCodice      = Selezionata.Codice;
        FormDescrizione = Selezionata.Descrizione;
        FormOrdine      = Selezionata.Ordine.ToString();
        Errore          = null;
        IsModifica      = true;
        IsFormVisible   = true;
    }

    private void ChiudiForm()
    {
        IsFormVisible   = false;
        FormCodice      = FormDescrizione = FormOrdine = string.Empty;
        Errore          = null;
    }

    // ─── Persistenza ─────────────────────────────────────────────────────────

    private async Task SalvaAsync()
    {
        var codice = FormCodice.Trim().ToUpper();
        var desc   = FormDescrizione.Trim();
        if (string.IsNullOrWhiteSpace(codice) || string.IsNullOrWhiteSpace(desc)) return;
        if (IsErroreVisible) return;

        var ordine = int.TryParse(FormOrdine, out var o) ? o : (Categorie.Count + 1) * 10;

        if (IsModifica)
        {
            var c = Categorie.FirstOrDefault(x => x.IdCategoriaPiastra == _idInModifica);
            if (c is null) return;
            c.Codice      = codice;
            c.Descrizione = desc;
            c.Ordine      = ordine;
            await _repo.UpdateAsync(c);
        }
        else
        {
            var nuova = new CategoriaPiastra { Codice = codice, Descrizione = desc, Ordine = ordine };
            await _repo.AddAsync(nuova);
            Categorie.Add(nuova);
        }

        ChiudiForm();
        await LoadAsync();
    }

    private async Task EliminaAsync()
    {
        if (Selezionata is null) return;

        var hasPiastre = await _repo.HasPiastreAssociateAsync(Selezionata.IdCategoriaPiastra);
        if (hasPiastre)
        {
            MessageBox.Show(
                $"Impossibile eliminare '{Selezionata.Descrizione}':\nè associata ad almeno una piastra.",
                "Eliminazione non consentita",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var conferma = MessageBox.Show(
            $"Eliminare la categoria '{Selezionata.Descrizione}'?",
            "Conferma eliminazione",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (conferma != MessageBoxResult.Yes) return;

        await _repo.DeleteAsync(Selezionata.IdCategoriaPiastra);
        Categorie.Remove(Selezionata);
        Selezionata = null;
        ChiudiForm();
    }
}
