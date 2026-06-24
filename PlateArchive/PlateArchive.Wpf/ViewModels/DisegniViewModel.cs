using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using PlateArchive.Core.Enums;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Wpf.Commands;

namespace PlateArchive.Wpf.ViewModels;

public class DisegniViewModel : ViewModelBase
{
    private readonly IDisegnoRepository _disegniRepo;

    private readonly ObservableCollection<Disegno> _tutti = [];

    private string   _filtroRicerca          = string.Empty;
    private string   _filtroStatoSelezionato = "Tutti";
    private Disegno? _disegnoSelezionato;

    // Campi form modifica
    private string       _formPercorsoFile = string.Empty;
    private string       _formRevisione    = string.Empty;
    private string       _formFormato      = string.Empty;
    private StatoDisegno _formStato        = StatoDisegno.DaVerificare;
    private string       _formNote         = string.Empty;
    private string?      _erroreFileNonTrovato;

    public DisegniViewModel(IDisegnoRepository disegniRepo)
    {
        _disegniRepo = disegniRepo;

        SalvaCommand    = new RelayCommand(async _ => await SalvaAsync(), _ => DisegnoSelezionato is not null);
        AprirFileCommand = new RelayCommand(_ => AprirFile(), _ => !string.IsNullOrWhiteSpace(FormPercorsoFile));
        SfogliaFileCommand = new RelayCommand(_ => SfogliaFile(), _ => DisegnoSelezionato is not null);

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

    public IEnumerable<string> StatiFiltro { get; } = ["Tutti", "Attivo", "Da verificare", "Obsoleto"];

    public ObservableCollection<Disegno> DisegniFiltrati { get; } = [];

    public Disegno? DisegnoSelezionato
    {
        get => _disegnoSelezionato;
        set
        {
            if (SetField(ref _disegnoSelezionato, value))
            {
                OnPropertyChanged(nameof(IsDetailVisible));
                OnPropertyChanged(nameof(IsNessunDisegnoSelezionato));
                CaricaForm();
            }
        }
    }

    public bool IsDetailVisible           => DisegnoSelezionato is not null;
    public bool IsNessunDisegnoSelezionato => DisegnoSelezionato is null;

    // ─── Proprietà form modifica ─────────────────────────────────

    public string FormPercorsoFile
    {
        get => _formPercorsoFile;
        set { if (SetField(ref _formPercorsoFile, value)) ErroreFileNonTrovato = null; }
    }

    public string FormRevisione
    {
        get => _formRevisione;
        set => SetField(ref _formRevisione, value);
    }

    public string FormFormato
    {
        get => _formFormato;
        set => SetField(ref _formFormato, value);
    }

    public StatoDisegno FormStato
    {
        get => _formStato;
        set => SetField(ref _formStato, value);
    }

    public string FormNote
    {
        get => _formNote;
        set => SetField(ref _formNote, value);
    }

    public string? ErroreFileNonTrovato
    {
        get => _erroreFileNonTrovato;
        set
        {
            if (SetField(ref _erroreFileNonTrovato, value))
                OnPropertyChanged(nameof(IsErroreFileVisible));
        }
    }

    public bool IsErroreFileVisible => !string.IsNullOrEmpty(_erroreFileNonTrovato);

    public IEnumerable<StatoDisegno> StatiDisegno { get; } = Enum.GetValues<StatoDisegno>();

    public IEnumerable<string> FormatiDisponibili { get; } = ["DWG", "DXF", "PDF", "STP", "STEP", "IGS"];

    // ─── Comandi ─────────────────────────────────────────────────

    public ICommand SalvaCommand      { get; }
    public ICommand AprirFileCommand  { get; }
    public ICommand SfogliaFileCommand { get; }

    // ─── Caricamento ─────────────────────────────────────────────

    private async Task LoadAsync()
    {
        var disegni = await _disegniRepo.GetAllAsync();
        foreach (var d in disegni) _tutti.Add(d);
        AggiornaFiltro();
    }

    private void AggiornaFiltro()
    {
        StatoDisegno? statoFiltro = FiltroStatoSelezionato switch
        {
            "Attivo"        => StatoDisegno.Attivo,
            "Da verificare" => StatoDisegno.DaVerificare,
            "Obsoleto"      => StatoDisegno.Obsoleto,
            _               => null
        };

        var f = FiltroRicerca.Trim().ToLower();

        var filtrati = _tutti
            .Where(d =>
                (statoFiltro is null || d.Stato == statoFiltro)
                && (string.IsNullOrEmpty(f)
                    || (d.CodiceDisegno?.ToLower().Contains(f) ?? false)
                    || (d.Piastra?.CodicePiastra.ToLower().Contains(f) ?? false)))
            .OrderBy(d => d.Stato == StatoDisegno.DaVerificare ? 0 : 1)
            .ThenBy(d => d.CodiceDisegno);

        DisegniFiltrati.Clear();
        foreach (var d in filtrati) DisegniFiltrati.Add(d);
    }

    private void CaricaForm()
    {
        if (DisegnoSelezionato is null) return;
        FormPercorsoFile     = DisegnoSelezionato.PercorsoFile ?? string.Empty;
        FormRevisione        = DisegnoSelezionato.Revisione    ?? string.Empty;
        FormFormato          = DisegnoSelezionato.Formato      ?? string.Empty;
        FormStato            = DisegnoSelezionato.Stato;
        FormNote             = DisegnoSelezionato.Note         ?? string.Empty;
        ErroreFileNonTrovato = null;
    }

    // ─── Salvataggio ─────────────────────────────────────────────

    private async Task SalvaAsync()
    {
        if (DisegnoSelezionato is null) return;

        DisegnoSelezionato.PercorsoFile = N(FormPercorsoFile);
        DisegnoSelezionato.Revisione    = N(FormRevisione);
        DisegnoSelezionato.Formato      = N(FormFormato);
        DisegnoSelezionato.Stato        = FormStato;
        DisegnoSelezionato.Note         = N(FormNote);

        await _disegniRepo.UpdateAsync(DisegnoSelezionato);

        // Riordina la lista (lo stato potrebbe essere cambiato)
        AggiornaFiltro();
    }

    // ─── Apertura e selezione file ───────────────────────────────

    private void AprirFile()
    {
        var percorso = FormPercorsoFile.Trim();
        if (string.IsNullOrEmpty(percorso)) return;

        if (!File.Exists(percorso))
        {
            ErroreFileNonTrovato = "File non trovato al percorso indicato.";
            return;
        }

        ErroreFileNonTrovato = null;
        try
        {
            Process.Start(new ProcessStartInfo(percorso) { UseShellExecute = true });
        }
        catch
        {
            ErroreFileNonTrovato = "Impossibile aprire il file con l'applicazione predefinita.";
        }
    }

    private void SfogliaFile()
    {
        var dlg = new OpenFileDialog
        {
            Title  = "Seleziona file disegno",
            Filter = "File disegno|*.dwg;*.dxf;*.pdf;*.stp;*.step;*.igs|Tutti i file|*.*"
        };
        if (dlg.ShowDialog() == true)
            FormPercorsoFile = dlg.FileName;
    }

    private static string? N(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
