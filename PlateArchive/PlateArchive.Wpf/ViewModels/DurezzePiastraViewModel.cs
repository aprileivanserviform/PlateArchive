using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Wpf.Commands;

namespace PlateArchive.Wpf.ViewModels;

/// <summary>
/// ViewModel della schermata "Durezze Piastra" (sezione Impostazioni).
/// Gestisce il CRUD della tabella lookup <see cref="DurezzaStandard"/>:
/// i valori di durezza selezionabili nel form piastra (es. "60 ShA", "65 ShD").
/// <para>
/// Layout: lista a sinistra | pannello destra (azioni OPPURE form).
/// - <see cref="IsDetailVisible"/> = elemento selezionato + nessun form aperto → mostra Modifica/Elimina
/// - <see cref="IsFormVisible"/>   = form aperto → mostra i campi
/// </para>
/// </summary>
public class DurezzePiastraViewModel : ViewModelBase
{
    private readonly IDurezzaStandardRepository _durezzaRepo;

    private string           _formValore     = string.Empty;
    private string           _formNote       = string.Empty;
    private string?          _errore;
    private bool             _isFormVisible;
    private bool             _isModifica;
    private int              _idInModifica;
    private DurezzaStandard? _durezzaSelezionata;

    public DurezzePiastraViewModel(IDurezzaStandardRepository durezzaRepo)
    {
        _durezzaRepo = durezzaRepo;

        NuovoCommand    = new RelayCommand(_ => ApriFormNuovo());
        SalvaCommand    = new RelayCommand(async _ => await SalvaAsync(), _ => !string.IsNullOrWhiteSpace(FormValore) && !IsErroreVisible);
        AnnullaCommand  = new RelayCommand(_ => ChiudiForm());
        ModificaCommand = new RelayCommand(_ => ApriFormModifica(), _ => DurezzaSelezionata is not null);
        EliminaCommand  = new RelayCommand(async _ => await EliminaAsync(), _ => DurezzaSelezionata is not null);
    }

    public override Task OnNavigatedAsync() => LoadAsync();

    public ObservableCollection<DurezzaStandard> Durezze { get; } = [];

    public DurezzaStandard? DurezzaSelezionata
    {
        get => _durezzaSelezionata;
        set
        {
            if (SetField(ref _durezzaSelezionata, value))
                OnPropertyChanged(nameof(IsDetailVisible));
        }
    }

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

    public bool   IsDetailVisible => DurezzaSelezionata is not null && !IsFormVisible;
    public string FormTitolo      => IsModifica ? "Modifica durezza" : "Nuova durezza";

    public string FormValore
    {
        get => _formValore;
        set { if (SetField(ref _formValore, value)) ControllaDuplicato(value); }
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

    public ICommand NuovoCommand    { get; }
    public ICommand SalvaCommand    { get; }
    public ICommand AnnullaCommand  { get; }
    public ICommand ModificaCommand { get; }
    public ICommand EliminaCommand  { get; }

    private async Task LoadAsync()
    {
        var durezze = await _durezzaRepo.GetAllAsync();
        Durezze.Clear();
        foreach (var d in durezze) Durezze.Add(d);
    }

    private void ControllaDuplicato(string valore)
    {
        if (string.IsNullOrWhiteSpace(valore)) { Errore = null; return; }
        var dup = Durezze.FirstOrDefault(d =>
            d.Valore.Equals(valore.Trim(), StringComparison.OrdinalIgnoreCase)
            && d.IdDurezza != _idInModifica);
        Errore = dup is not null ? $"Valore '{dup.Valore}' già presente." : null;
    }

    private void ApriFormNuovo()
    {
        _idInModifica = 0;
        IsModifica    = false;
        FormValore    = string.Empty;
        FormNote      = string.Empty;
        Errore        = null;
        IsFormVisible = true;
    }

    private void ApriFormModifica()
    {
        if (DurezzaSelezionata is null) return;
        _idInModifica = DurezzaSelezionata.IdDurezza;
        FormValore    = DurezzaSelezionata.Valore;
        FormNote      = DurezzaSelezionata.Note ?? string.Empty;
        Errore        = null;
        IsModifica    = true;
        IsFormVisible = true;
    }

    private void ChiudiForm()
    {
        IsFormVisible = false;
        FormValore = FormNote = string.Empty;
        Errore     = null;
    }

    private async Task SalvaAsync()
    {
        var valore = FormValore.Trim();
        if (string.IsNullOrWhiteSpace(valore) || IsErroreVisible) return;

        if (IsModifica)
        {
            var d = Durezze.FirstOrDefault(x => x.IdDurezza == _idInModifica);
            if (d is null) return;
            d.Valore = valore;
            d.Note   = N(FormNote);
            await _durezzaRepo.UpdateAsync(d);
        }
        else
        {
            var nuova = new DurezzaStandard { Valore = valore, Note = N(FormNote) };
            await _durezzaRepo.AddAsync(nuova);
            Durezze.Add(nuova);
        }

        ChiudiForm();
        await LoadAsync();
    }

    private async Task EliminaAsync()
    {
        if (DurezzaSelezionata is null) return;

        var haPiastre = await _durezzaRepo.HasPiastreAssociateAsync(DurezzaSelezionata.IdDurezza);
        if (haPiastre)
        {
            MessageBox.Show(
                $"Impossibile eliminare '{DurezzaSelezionata.Valore}':\nè associata a piastre esistenti.",
                "Eliminazione non consentita",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var conferma = MessageBox.Show(
            $"Eliminare la durezza '{DurezzaSelezionata.Valore}'?",
            "Conferma eliminazione",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);

        if (conferma != MessageBoxResult.Yes) return;

        await _durezzaRepo.EliminaLogicamenteAsync(DurezzaSelezionata.IdDurezza);
        Durezze.Remove(DurezzaSelezionata);
        DurezzaSelezionata = null;
        ChiudiForm();
    }

    private static string? N(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
