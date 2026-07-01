using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Services;
using PlateArchive.Wpf.Commands;

namespace PlateArchive.Wpf.ViewModels;

/// <summary>
/// Riga ordine di vendita (letta dal gestionale) abbinata alla piastra corrispondente
/// (ricerca locale per <see cref="Piastra.CodiceArticoloGestionale"/>), se trovata.
/// </summary>
public class RigaOrdineVenditaRow(RigaOrdineVendita riga, Piastra? piastra)
{
    public RigaOrdineVendita Riga     { get; } = riga;
    public Piastra?          Piastra  { get; } = piastra;

    public bool PiastraTrovata    => Piastra is not null;
    public bool PiastraNonTrovata => Piastra is null;
    public bool HaDisegno         => Piastra?.Disegno is not null;
}

/// <summary>
/// ViewModel della schermata "Ordini vendita": elenca le righe ordine non evase lette dal
/// gestionale (DB2/Panthera, interrogazione live — nessuna cache locale, vedi TASK-16/17 in
/// docs/TASKS.md) e permette di aprire direttamente il disegno tecnico della piastra
/// corrispondente all'articolo di riga, senza doverla cercare manualmente in Piastre.
/// </summary>
public class OrdiniVenditaViewModel : ViewModelBase
{
    private readonly IRigheOrdineVenditaService _righeOrdineService;
    private readonly IPiastraRepository         _piastreRepo;

    private readonly List<RigaOrdineVenditaRow> _tutte = [];

    private string  _filtroRicerca = string.Empty;
    private bool    _isCaricamento;
    private string? _errore;

    public OrdiniVenditaViewModel(IRigheOrdineVenditaService righeOrdineService, IPiastraRepository piastreRepo)
    {
        _righeOrdineService = righeOrdineService;
        _piastreRepo        = piastreRepo;

        AggiornaCommand     = new RelayCommand(async _ => await CaricaAsync());
        AprirDisegnoCommand = new RelayCommand(
            p => AprirDisegno((RigaOrdineVenditaRow)p!),
            p => p is RigaOrdineVenditaRow { HaDisegno: true });
    }

    public ObservableCollection<RigaOrdineVenditaRow> RigheFiltrate { get; } = [];

    public string FiltroRicerca
    {
        get => _filtroRicerca;
        set { if (SetField(ref _filtroRicerca, value)) AggiornaFiltro(); }
    }

    public bool IsCaricamento
    {
        get => _isCaricamento;
        set => SetField(ref _isCaricamento, value);
    }

    public string? Errore
    {
        get => _errore;
        set { if (SetField(ref _errore, value)) OnPropertyChanged(nameof(IsErroreVisible)); }
    }

    public bool IsErroreVisible => !string.IsNullOrEmpty(_errore);

    public ICommand AggiornaCommand     { get; }
    public ICommand AprirDisegnoCommand { get; }

    public override Task OnNavigatedAsync() => CaricaAsync();

    private async Task CaricaAsync()
    {
        Errore = null;

        if (!_righeOrdineService.IsDisponibile)
        {
            Errore = "Connessione al gestionale (DB2) non configurata.";
            return;
        }

        IsCaricamento = true;
        try
        {
            var righe = await _righeOrdineService.LeggiRigheInevaseAsync();

            _tutte.Clear();
            foreach (var r in righe)
            {
                var piastra = await _piastreRepo.GetByCodiceArticoloGestionaleAsync(r.CodiceArticolo);
                _tutte.Add(new RigaOrdineVenditaRow(r, piastra));
            }

            AggiornaFiltro();
        }
        catch (Exception ex)
        {
            Errore = $"Errore durante la lettura degli ordini dal gestionale: {ex.Message}";
        }
        finally
        {
            IsCaricamento = false;
        }
    }

    private void AggiornaFiltro()
    {
        var f = FiltroRicerca.Trim().ToLower();

        RigheFiltrate.Clear();
        foreach (var r in _tutte.Where(r =>
            string.IsNullOrEmpty(f)
            || r.Riga.CodiceArticolo.ToLower().Contains(f)
            || r.Riga.RagioneSocialeCliente.ToLower().Contains(f)
            || r.Riga.NumeroOrdine.ToString().Contains(f)))
        {
            RigheFiltrate.Add(r);
        }
    }

    private void AprirDisegno(RigaOrdineVenditaRow row)
    {
        var percorso = row.Piastra?.Disegno?.PercorsoFile;
        if (string.IsNullOrEmpty(percorso)) return;

        if (!File.Exists(percorso))
        {
            Errore = $"File non trovato: {percorso}";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(percorso) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Errore = $"Impossibile aprire il file: {ex.Message}";
        }
    }
}
