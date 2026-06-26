using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Wpf.Commands;

namespace PlateArchive.Wpf.ViewModels;

public class FormatiMacchinaViewModel : ViewModelBase
{
    private readonly IFormatoMacchinaRepository _formatiRepo;

    private string           _formNome         = string.Empty;
    private string           _formNote         = string.Empty;
    private string?          _errore;
    private bool             _isFormVisible;
    private bool             _isModifica;
    private int              _idInModifica;
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

    public ObservableCollection<FormatoMacchina> Formati { get; } = [];

    public FormatoMacchina? FormatoSelezionato
    {
        get => _formatoSelezionato;
        set
        {
            if (SetField(ref _formatoSelezionato, value))
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

    public bool   IsDetailVisible => FormatoSelezionato is not null && !IsFormVisible;
    public string FormTitolo      => IsModifica ? "Modifica formato" : "Nuovo formato";

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

    public ICommand NuovoCommand    { get; }
    public ICommand SalvaCommand    { get; }
    public ICommand AnnullaCommand  { get; }
    public ICommand ModificaCommand { get; }
    public ICommand EliminaCommand  { get; }

    private async Task LoadAsync()
    {
        var formati = await _formatiRepo.GetAllAsync();
        Formati.Clear();
        foreach (var f in formati) Formati.Add(f);
    }

    private void ControllaDuplicato(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome)) { Errore = null; return; }
        var dup = Formati.FirstOrDefault(f =>
            f.NomeFormato.Equals(nome.Trim(), StringComparison.OrdinalIgnoreCase)
            && f.IdFormato != _idInModifica);
        Errore = dup is not null ? $"Formato '{dup.NomeFormato}' già presente." : null;
    }

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

    private async Task SalvaAsync()
    {
        var nome = FormNome.Trim();
        if (string.IsNullOrWhiteSpace(nome) || IsErroreVisible) return;

        if (IsModifica)
        {
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
        await LoadAsync();
    }

    private async Task EliminaAsync()
    {
        if (FormatoSelezionato is null) return;

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

        await _formatiRepo.EliminaLogicamenteAsync(FormatoSelezionato.IdFormato);
        Formati.Remove(FormatoSelezionato);
        FormatoSelezionato = null;
        ChiudiForm();
    }

    private static string? N(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
