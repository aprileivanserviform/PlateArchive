using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using PlateArchive.Core.Enums;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Wpf.Commands;
using PlateArchive.Wpf.Services;

namespace PlateArchive.Wpf.ViewModels;

/// <summary>
/// ViewModel della schermata Disegni.
/// Mostra tutti i disegni tecnici presenti nel sistema (uno per piastra)
/// con filtro per stato e ricerca testuale.
/// Permette di modificare i metadati del disegno (percorso file, revisione, stato)
/// e di aprire il file con l'applicazione predefinita del sistema operativo.
/// <para>
/// NOTA: i disegni vengono creati automaticamente quando si associa un file a una piastra
/// (drag &amp; drop in PiastreView). Qui si gestisce solo la modifica dei metadati.
/// </para>
/// </summary>
public class DisegniViewModel : ViewModelBase
{
    private readonly IDisegnoRepository _disegniRepo;

    // Lista completa in memoria — il filtro è applicato su questa.
    private readonly ObservableCollection<Disegno> _tutti = [];

    private string   _filtroRicerca          = string.Empty;
    private string   _filtroStatoSelezionato = "Tutti";
    private Disegno? _disegnoSelezionato;
    private bool     _isCaricamento;

    // Campi del form di modifica (sincronizzati con DisegnoSelezionato)
    private string       _formPercorsoFile = string.Empty;
    private string       _formRevisione    = string.Empty;
    private string       _formFormato      = string.Empty;
    private StatoDisegno _formStato        = StatoDisegno.DaVerificare;
    private string       _formNote         = string.Empty;
    private string?       _erroreFileNonTrovato;
    private BitmapSource? _anteprimaDisegno;
    private bool          _isCaricamentoAnteprima;

    public DisegniViewModel(IDisegnoRepository disegniRepo)
    {
        _disegniRepo = disegniRepo;

        SalvaCommand      = new RelayCommand(async _ => await SalvaAsync(), _ => DisegnoSelezionato is not null);
        AprirFileCommand  = new RelayCommand(_ => AprirFile(),   _ => !string.IsNullOrWhiteSpace(FormPercorsoFile));
        SfogliaFileCommand = new RelayCommand(_ => SfogliaFile(), _ => DisegnoSelezionato is not null);
    }

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

    /// <summary>Valori disponibili nel ComboBox di filtro stato.</summary>
    public IEnumerable<string> StatiFiltro { get; } = ["Tutti", "Attivo", "Da verificare", "Obsoleto"];

    /// <summary>Subset filtrato di _tutti — bound alla DataGrid.</summary>
    public ObservableCollection<Disegno> DisegniFiltrati { get; } = [];

    // ─── Selezione e dettaglio ────────────────────────────────────────────────

    public Disegno? DisegnoSelezionato
    {
        get => _disegnoSelezionato;
        set
        {
            if (SetField(ref _disegnoSelezionato, value))
            {
                OnPropertyChanged(nameof(IsDetailVisible));
                OnPropertyChanged(nameof(IsNessunDisegnoSelezionato));
                OnPropertyChanged(nameof(IsNoAnteprima));
                // Popola il form con i dati del disegno selezionato
                CaricaForm();
                _ = CaricaAnteprimaAsync();
            }
        }
    }

    public bool IsCaricamento
    {
        get => _isCaricamento;
        set => SetField(ref _isCaricamento, value);
    }

    public bool IsDetailVisible            => DisegnoSelezionato is not null;
    public bool IsNessunDisegnoSelezionato => DisegnoSelezionato is null;

    // ─── Campi form modifica ──────────────────────────────────────────────────

    public string FormPercorsoFile
    {
        get => _formPercorsoFile;
        // Quando il percorso viene modificato manualmente, azzera l'eventuale errore precedente.
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

    public BitmapSource? AnteprimaDisegno
    {
        get => _anteprimaDisegno;
        set
        {
            if (SetField(ref _anteprimaDisegno, value))
            {
                OnPropertyChanged(nameof(IsAnteprimaVisible));
                OnPropertyChanged(nameof(IsNoAnteprima));
            }
        }
    }
    public bool IsAnteprimaVisible => _anteprimaDisegno is not null;

    public bool IsCaricamentoAnteprima
    {
        get => _isCaricamentoAnteprima;
        private set { if (SetField(ref _isCaricamentoAnteprima, value)) OnPropertyChanged(nameof(IsNoAnteprima)); }
    }
    public bool IsNoAnteprima => !IsAnteprimaVisible && !IsCaricamentoAnteprima && DisegnoSelezionato is not null;

    /// <summary>Tutti i valori dell'enum StatoDisegno — usati nel ComboBox del form.</summary>
    public IEnumerable<StatoDisegno> StatiDisegno    { get; } = Enum.GetValues<StatoDisegno>();
    public IEnumerable<string>       FormatiDisponibili { get; } = ["DWG", "DXF", "PDF", "STP", "STEP", "IGS"];

    // ─── Comandi ─────────────────────────────────────────────────────────────

    public ICommand SalvaCommand       { get; }
    public ICommand AprirFileCommand   { get; }
    public ICommand SfogliaFileCommand { get; }

    // ─── Inizializzazione navigazione ─────────────────────────────────────────

    public override async Task OnNavigatedAsync()
    {
        IsCaricamento = true;
        try   { await LoadAsync(); }
        finally { IsCaricamento = false; }
    }

    // ─── Caricamento ─────────────────────────────────────────────────────────

    private async Task LoadAsync()
    {
        var disegni = await _disegniRepo.GetAllAsync();
        foreach (var d in disegni) _tutti.Add(d);
        AggiornaFiltro();
    }

    /// <summary>Aggiunge un disegno appena creato alla lista senza ricaricare tutto dal DB.</summary>
    public void AggiungiDisegno(Disegno d)
    {
        _tutti.Add(d);
        AggiornaFiltro();
    }

    // ─── Filtro in memoria ────────────────────────────────────────────────────

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

        // Ordine: i più recenti (DataUltimaModificaFile) in cima, poi alfabetico.
        var filtrati = _tutti
            .Where(d =>
                (statoFiltro is null || d.Stato == statoFiltro)
                && (string.IsNullOrEmpty(f)
                    || (d.CodiceDisegno?.ToLower().Contains(f) ?? false)
                    || (d.Piastra?.CodicePiastra.ToLower().Contains(f) ?? false)))
            .OrderByDescending(d => d.DataUltimaModificaFile)
            .ThenBy(d => d.CodiceDisegno);

        DisegniFiltrati.Clear();
        foreach (var d in filtrati) DisegniFiltrati.Add(d);
    }

    // ─── Form ─────────────────────────────────────────────────────────────────

    private void CaricaForm()
    {
        if (DisegnoSelezionato is null) return;
        FormPercorsoFile      = DisegnoSelezionato.PercorsoFile ?? string.Empty;
        FormRevisione         = DisegnoSelezionato.Revisione    ?? string.Empty;
        FormFormato           = DisegnoSelezionato.Formato      ?? string.Empty;
        FormStato             = DisegnoSelezionato.Stato;
        FormNote              = DisegnoSelezionato.Note         ?? string.Empty;
        ErroreFileNonTrovato  = null;
    }

    // ─── Salvataggio ─────────────────────────────────────────────────────────

    private async Task SalvaAsync()
    {
        if (DisegnoSelezionato is null) return;
        var salvato = DisegnoSelezionato;

        salvato.PercorsoFile = N(FormPercorsoFile);
        salvato.Revisione    = N(FormRevisione);
        salvato.Formato      = N(FormFormato);
        salvato.Stato        = FormStato;
        salvato.Note         = N(FormNote);

        await _disegniRepo.UpdateAsync(salvato);

        // Riordina la lista: il rebuild può causare la perdita di selezione nel DataGrid.
        // Ripristiniamo sempre la selezione sull'elemento salvato; SetField gestisce il no-op
        // se l'oggetto non è cambiato, ma garantisce CaricaForm() se il DataGrid ha perso
        // la selezione (o ne ha selezionata un'altra durante il CollectionChanged.Reset).
        AggiornaFiltro();
        DisegnoSelezionato = salvato;
        // SetField restituisce false se stessa reference → CaricaAnteprimaAsync non è
        // triggerato dal setter; lo chiamiamo esplicitamente per aggiornare il percorso.
        _ = CaricaAnteprimaAsync();
    }

    // ─── Anteprima DWG ───────────────────────────────────────────────────────

    private async Task CaricaAnteprimaAsync()
    {
        AnteprimaDisegno       = null;
        IsCaricamentoAnteprima = true;
        try
        {
            AnteprimaDisegno = await DwgThumbnailReader.EstraiAnteprimaAsync(_disegnoSelezionato?.PercorsoFile);
        }
        finally
        {
            IsCaricamentoAnteprima = false;
        }
    }

    // ─── Apertura file ────────────────────────────────────────────────────────

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
            // UseShellExecute = true apre il file con l'applicazione predefinita del SO
            // (es. AutoCAD per .dwg, Adobe per .pdf).
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
