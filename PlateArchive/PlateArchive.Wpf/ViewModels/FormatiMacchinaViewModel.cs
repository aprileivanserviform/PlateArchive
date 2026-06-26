using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Wpf.Commands;

namespace PlateArchive.Wpf.ViewModels;

/// <summary>
/// ViewModel della schermata "Formati Macchina" (sezione Impostazioni).
/// Gestisce il CRUD della tabella lookup <see cref="FormatoMacchina"/>:
/// i formati determinano la compatibilità tra piastre e macchine.
/// <para>
/// Layout: lista a sinistra | pannello destra (azioni sul selezionato OPPURE form).
/// - <see cref="IsDetailVisible"/> = elemento selezionato + nessun form aperto → mostra pulsanti Modifica/Elimina
/// - <see cref="IsFormVisible"/>   = form aperto (nuovo o modifica) → mostra i campi di testo
/// </para>
/// </summary>
public class FormatiMacchinaViewModel : ViewModelBase
{
    private readonly IFormatoMacchinaRepository _formatiRepo;

    private string           _formNome         = string.Empty;
    private string           _formNote         = string.Empty;
    private string?          _errore;
    private bool             _isFormVisible;
    private bool             _isModifica;
    private int              _idInModifica;  // 0 = nuovo record
    private FormatoMacchina? _formatoSelezionato;

    public FormatiMacchinaViewModel(IFormatoMacchinaRepository formatiRepo)
    {
        _formatiRepo = formatiRepo;

        NuovoCommand    = new RelayCommand(_ => ApriFormNuovo());
        SalvaCommand    = new RelayCommand(async _ => await SalvaAsync(), _ => !string.IsNullOrWhiteSpace(FormNome) && !IsErroreVisible);
        AnnullaCommand  = new RelayCommand(_ => ChiudiForm());
        ModificaCommand = new RelayCommand(_ => ApriFormModifica(), _ => FormatoSelezionato is not null);
        EliminaCommand  = new RelayCommand(async _ => await EliminaAsync(), _ => FormatoSelezionato is not null);

        _ = LoadAsync();
    }

    // ─── Lista formati ────────────────────────────────────────────────────────

    /// <summary>Tutti i formati non eliminati — bound alla DataGrid.</summary>
    public ObservableCollection<FormatoMacchina> Formati { get; } = [];

    public FormatoMacchina? FormatoSelezionato
    {
        get => _formatoSelezionato;
        set
        {
            if (SetField(ref _formatoSelezionato, value))
                // IsDetailVisible dipende sia da FormatoSelezionato che da IsFormVisible.
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

    /// <summary>True quando c'è un formato selezionato E il form non è aperto → mostra azioni.</summary>
    public bool   IsDetailVisible => FormatoSelezionato is not null && !IsFormVisible;
    public string FormTitolo      => IsModifica ? "Modifica formato" : "Nuovo formato";

    // ─── Campi form ───────────────────────────────────────────────────────────

    public string FormNome
    {
        get => _formNome;
        // La validazione duplicati scatta ad ogni carattere digitato (real-time).
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
        var formati = await _formatiRepo.GetAllAsync();
        Formati.Clear();
        foreach (var f in formati) Formati.Add(f);
    }

    // ─── Validazione ──────────────────────────────────────────────────────────

    /// <summary>
    /// Verifica in memoria se esiste già un formato con lo stesso nome (case-insensitive),
    /// escludendo quello che si sta modificando (se _idInModifica != 0).
    /// </summary>
    private void ControllaDuplicato(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome)) { Errore = null; return; }
        var dup = Formati.FirstOrDefault(f =>
            f.NomeFormato.Equals(nome.Trim(), StringComparison.OrdinalIgnoreCase)
            && f.IdFormato != _idInModifica);
        Errore = dup is not null ? $"Formato '{dup.NomeFormato}' già presente." : null;
    }

    // ─── Gestione form ────────────────────────────────────────────────────────

    private void ApriFormNuovo()
    {
        _idInModifica = 0;  // 0 = nuovo record (nessun ID da escludere dalla validazione duplicati)
        IsModifica    = false;
        FormNome      = string.Empty;
        FormNote      = string.Empty;
        Errore        = null;
        IsFormVisible = true;
    }

    private void ApriFormModifica()
    {
        if (FormatoSelezionato is null) return;
        _idInModifica = FormatoSelezionato.IdFormato;
        FormNome      = FormatoSelezionato.NomeFormato;
        FormNote      = FormatoSelezionato.Note ?? string.Empty;
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
            // Modifica in-place sull'oggetto già in lista (EF Core lo traccia).
            var f = Formati.FirstOrDefault(x => x.IdFormato == _idInModifica);
            if (f is null) return;
            f.NomeFormato = nome;
            f.Note        = N(FormNote);
            await _formatiRepo.UpdateAsync(f);
        }
        else
        {
            var nuovo = new FormatoMacchina { NomeFormato = nome, Note = N(FormNote) };
            await _formatiRepo.AddAsync(nuovo);
            Formati.Add(nuovo);
        }

        ChiudiForm();
        await LoadAsync();  // Ricarica per aggiornare l'ordinamento e l'ID del nuovo record.
    }

    private async Task EliminaAsync()
    {
        if (FormatoSelezionato is null) return;

        // Prima di eliminare, verifica se il formato è ancora usato da macchine o piastre.
        // Se sì, mostra un messaggio e blocca l'eliminazione (integrità referenziale soft).
        var haAssociazioni = await _formatiRepo.HasMacchineAssociateAsync(FormatoSelezionato.IdFormato);
        if (haAssociazioni)
        {
            MessageBox.Show(
                $"Impossibile eliminare '{FormatoSelezionato.NomeFormato}':\nè associato a macchine o piastre esistenti.",
                "Eliminazione non consentita",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var conferma = MessageBox.Show(
            $"Eliminare il formato '{FormatoSelezionato.NomeFormato}'?",
            "Conferma eliminazione",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (conferma != MessageBoxResult.Yes) return;

        // Soft-delete: imposta IsEliminata = true, non cancella il record.
        await _formatiRepo.EliminaLogicamenteAsync(FormatoSelezionato.IdFormato);
        Formati.Remove(FormatoSelezionato);
        FormatoSelezionato = null;
        ChiudiForm();
    }

    private static string? N(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
