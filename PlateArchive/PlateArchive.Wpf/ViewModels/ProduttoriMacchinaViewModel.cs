using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Wpf.Commands;

namespace PlateArchive.Wpf.ViewModels;

/// <summary>
/// ViewModel della schermata "Produttori Macchina" (sezione Impostazioni).
/// Gestisce il CRUD della tabella lookup <see cref="ProduttoreMacchina"/>:
/// i produttori raggruppano i modelli di macchine per marca (es. Mark Andy, Nilpeter).
/// <para>
/// Layout: lista a sinistra | pannello destra (azioni sul selezionato OPPURE form).
/// - <see cref="IsDetailVisible"/> = elemento selezionato + nessun form aperto → mostra pulsanti Modifica/Elimina
/// - <see cref="IsFormVisible"/>   = form aperto (nuovo o modifica) → mostra i campi di testo
/// </para>
/// </summary>
public class ProduttoriMacchinaViewModel : ViewModelBase
{
    private readonly IProduttoreMacchinaRepository _produttoriRepo;

    private string              _formNome           = string.Empty;
    private string              _formNote           = string.Empty;
    private string?             _errore;
    private bool                _isFormVisible;
    private bool                _isModifica;
    private int                 _idInModifica;  // 0 = nuovo record
    private ProduttoreMacchina? _produttoreSelezionato;

    public ProduttoriMacchinaViewModel(IProduttoreMacchinaRepository produttoriRepo)
    {
        _produttoriRepo = produttoriRepo;

        NuovoCommand    = new RelayCommand(_ => ApriFormNuovo());
        SalvaCommand    = new RelayCommand(async _ => await SalvaAsync(), _ => !string.IsNullOrWhiteSpace(FormNome) && !IsErroreVisible);
        AnnullaCommand  = new RelayCommand(_ => ChiudiForm());
        ModificaCommand = new RelayCommand(_ => ApriFormModifica(), _ => ProduttoreSelezionato is not null);
        EliminaCommand  = new RelayCommand(async _ => await EliminaAsync(), _ => ProduttoreSelezionato is not null);
    }

    // ─── Inizializzazione navigazione ─────────────────────────────────────────

    public override Task OnNavigatedAsync() => LoadAsync();

    // ─── Lista produttori ─────────────────────────────────────────────────────

    /// <summary>Tutti i produttori non eliminati — bound alla DataGrid.</summary>
    public ObservableCollection<ProduttoreMacchina> Produttori { get; } = [];

    public ProduttoreMacchina? ProduttoreSelezionato
    {
        get => _produttoreSelezionato;
        set
        {
            if (SetField(ref _produttoreSelezionato, value))
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

    /// <summary>True quando c'è un produttore selezionato E il form non è aperto → mostra azioni.</summary>
    public bool   IsDetailVisible => ProduttoreSelezionato is not null && !IsFormVisible;
    public string FormTitolo      => IsModifica ? "Modifica produttore" : "Nuovo produttore";

    // ─── Campi form ───────────────────────────────────────────────────────────

    public string FormNome
    {
        get => _formNome;
        set { if (SetField(ref _formNome, value)) ControllaDuplicato(value); }
    }

    public string FormNote
    {
        get => _formNote;
        set => SetField(ref _formNote, value);
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
        var produttori = await _produttoriRepo.GetAllAsync();
        Produttori.Clear();
        foreach (var p in produttori) Produttori.Add(p);
    }

    // ─── Validazione ──────────────────────────────────────────────────────────

    private void ControllaDuplicato(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome)) { Errore = null; return; }
        var dup = Produttori.FirstOrDefault(p =>
            p.NomeProduttore.Equals(nome.Trim(), StringComparison.OrdinalIgnoreCase)
            && p.IdProduttore != _idInModifica);
        Errore = dup is not null ? $"Produttore '{dup.NomeProduttore}' già presente." : null;
    }

    // ─── Gestione form ────────────────────────────────────────────────────────

    private void ApriFormNuovo()
    {
        _idInModifica = 0;
        IsModifica    = false;
        FormNome      = string.Empty;
        FormNote      = string.Empty;
        Errore        = null;
        IsFormVisible = true;
    }

    private void ApriFormModifica()
    {
        if (ProduttoreSelezionato is null) return;
        _idInModifica = ProduttoreSelezionato.IdProduttore;
        FormNome      = ProduttoreSelezionato.NomeProduttore;
        FormNote      = ProduttoreSelezionato.Note ?? string.Empty;
        Errore        = null;
        IsModifica    = true;
        IsFormVisible = true;
    }

    private void ChiudiForm()
    {
        IsFormVisible = false;
        FormNome = FormNote = string.Empty;
        Errore   = null;
    }

    // ─── Persistenza ─────────────────────────────────────────────────────────

    private async Task SalvaAsync()
    {
        var nome = FormNome.Trim();
        if (string.IsNullOrWhiteSpace(nome) || IsErroreVisible) return;

        if (IsModifica)
        {
            var p = Produttori.FirstOrDefault(x => x.IdProduttore == _idInModifica);
            if (p is null) return;
            p.NomeProduttore = nome;
            p.Note           = N(FormNote);
            await _produttoriRepo.UpdateAsync(p);
        }
        else
        {
            var nuovo = new ProduttoreMacchina { NomeProduttore = nome, Note = N(FormNote) };
            await _produttoriRepo.AddAsync(nuovo);
            Produttori.Add(nuovo);
        }

        ChiudiForm();
        await LoadAsync();
    }

    private async Task EliminaAsync()
    {
        if (ProduttoreSelezionato is null) return;

        var haAssociazioni = await _produttoriRepo.HasMacchineAssociateAsync(ProduttoreSelezionato.IdProduttore);
        if (haAssociazioni)
        {
            MessageBox.Show(
                $"Impossibile eliminare '{ProduttoreSelezionato.NomeProduttore}':\nè associato a macchine esistenti.",
                "Eliminazione non consentita",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var conferma = MessageBox.Show(
            $"Eliminare il produttore '{ProduttoreSelezionato.NomeProduttore}'?",
            "Conferma eliminazione",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (conferma != MessageBoxResult.Yes) return;

        await _produttoriRepo.EliminaLogicamenteAsync(ProduttoreSelezionato.IdProduttore);
        Produttori.Remove(ProduttoreSelezionato);
        ProduttoreSelezionato = null;
        ChiudiForm();
    }

    private static string? N(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
