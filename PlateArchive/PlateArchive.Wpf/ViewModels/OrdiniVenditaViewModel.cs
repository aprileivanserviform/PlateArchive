using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using PlateArchive.Core.Enums;
using PlateArchive.Core.Models;
using PlateArchive.Core.Servizi;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Services;
using PlateArchive.Wpf.Commands;

namespace PlateArchive.Wpf.ViewModels;

/// <summary>
/// Piastra candidata per una riga ordine, trovata dal match cliente+formato.
/// <see cref="StatoAssociazione"/> è null per le piastre SpecialeCliente del cliente
/// che non hanno (ancora) una riga ClientePiastra.
/// </summary>
public record PiastraCompatibile(Piastra Piastra, StatoClientePiastra? StatoAssociazione)
{
    public bool IsObsoleta => StatoAssociazione == StatoClientePiastra.Obsoleta;
    public bool HaDisegno  => Piastra.Disegno is not null;
}

/// <summary>
/// Riga ordine di vendita (letta dal gestionale) con le piastre compatibili trovate dal
/// match cliente+formato (vedi <see cref="OrdiniVenditaViewModel"/>): nessuna (riga da
/// associare), una (comportamento diretto disegno/dettaglio) o più di una (dialog di scelta).
/// </summary>
public class RigaOrdineVenditaRow(RigaOrdineVendita riga, IReadOnlyList<PiastraCompatibile> piastreCompatibili)
{
    public RigaOrdineVendita                 Riga               { get; } = riga;
    public IReadOnlyList<PiastraCompatibile> PiastreCompatibili { get; } = piastreCompatibili;

    public bool PiastraTrovata      => PiastreCompatibili.Count > 0;
    public bool PiastraNonTrovata   => PiastreCompatibili.Count == 0;
    public bool PiastraSingola      => PiastreCompatibili.Count == 1;
    public bool MultipleCompatibili => PiastreCompatibili.Count > 1;
    public int  NumeroCompatibili   => PiastreCompatibili.Count;

    /// <summary>Piastra univoca della riga — solo quando il match è singolo.</summary>
    public Piastra? Piastra   => PiastraSingola ? PiastreCompatibili[0].Piastra : null;
    public bool     HaDisegno => PiastraSingola && PiastreCompatibili[0].HaDisegno;

    /// <summary>Codice articolo della vecchia codifica (contiene lettere): nessun match
    /// possibile, in attesa che il gestionale lo sospenda.</summary>
    public bool CodiceNonConforme => !CodiceArticoloPanthera.IsNuovaCodifica(Riga.CodiceArticolo);

    /// <summary>"Associa piastra" ha senso solo per i codici nuovi senza compatibili.</summary>
    public bool AssociaVisibile => PiastraNonTrovata && !CodiceNonConforme;
}

/// <summary>
/// ViewModel della schermata "Ordini vendita": elenca le righe ordine non evase lette dal
/// gestionale (DB2/Panthera, interrogazione live — nessuna cache locale, vedi TASK-16/17 in
/// docs/TASKS.md) e collega ogni riga alle piastre del cliente tramite la nuova codifica
/// articoli (TASK-18): cliente della riga (R_CLIENTE) + formato estratto dalle posizioni
/// 7-10 del codice articolo, confrontato con il FormatoMacchina delle piastre associate al
/// cliente (ClientePiastra, qualsiasi stato, + piastre SpecialeCliente). Le righe di lastre
/// grezze (formato 000) non vengono mostrate: non hanno disegno.
/// </summary>
public class OrdiniVenditaViewModel : ViewModelBase
{
    private readonly IRigheOrdineVenditaService _righeOrdineService;
    private readonly IPiastraRepository         _piastreRepo;
    private readonly IClienteRepository         _clientiRepo;
    private readonly IClientePiastraRepository  _clientiPiastreRepo;

    private readonly List<RigaOrdineVenditaRow> _tutte = [];

    private string                 _filtroRicerca = string.Empty;
    private bool                   _isCaricamento;
    private string?                _errore;
    private IReadOnlyList<string>  _colonne = [];

    public OrdiniVenditaViewModel(
        IRigheOrdineVenditaService righeOrdineService,
        IPiastraRepository         piastreRepo,
        IClienteRepository         clientiRepo,
        IClientePiastraRepository  clientiPiastreRepo)
    {
        _righeOrdineService = righeOrdineService;
        _piastreRepo        = piastreRepo;
        _clientiRepo        = clientiRepo;
        _clientiPiastreRepo = clientiPiastreRepo;

        AggiornaCommand     = new RelayCommand(async _ => await CaricaAsync());
        AprirDisegnoCommand = new RelayCommand(
            p => AprirDisegno((RigaOrdineVenditaRow)p!),
            p => p is RigaOrdineVenditaRow { HaDisegno: true });
    }

    public ObservableCollection<RigaOrdineVenditaRow> RigheFiltrate { get; } = [];

    /// <summary>
    /// Nomi colonna restituiti dalla query configurata in appsettings.json: la View li usa
    /// per generare le colonne della griglia (è la SELECT a comandare la tabella).
    /// </summary>
    public IReadOnlyList<string> Colonne
    {
        get => _colonne;
        private set => SetField(ref _colonne, value);
    }

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
            var result = await _righeOrdineService.LeggiRigheInevaseAsync();
            Colonne    = result.Colonne;

            // Lookup caricati una volta sola: il match per riga avviene tutto in memoria
            // (le query per riga renderebbero il caricamento proporzionale agli ordini).
            var clienti = (await _clientiRepo.GetAllAsync())
                .GroupBy(c => c.CodiceClienteGestionale, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            var piastre    = (await _piastreRepo.GetAllAsync()).ToList();
            var piastreById       = piastre.ToDictionary(p => p.IdPiastra);
            var specialiByCliente = piastre
                .Where(p => p.IdClienteEsclusivo is not null)
                .ToLookup(p => p.IdClienteEsclusivo!.Value);
            var associazioniByCliente = (await _clientiPiastreRepo.GetAllAsync())
                .ToLookup(cp => cp.IdCliente);

            _tutte.Clear();
            foreach (var r in result.Righe)
            {
                // Lastre grezze (formato 000): nessun disegno da gestire, riga non mostrata.
                if (CodiceArticoloPanthera.IsGrezza(r.CodiceArticolo)) continue;

                var compatibili = TrovaPiastreCompatibili(
                    r, clienti, associazioniByCliente, piastreById, specialiByCliente);
                _tutte.Add(new RigaOrdineVenditaRow(r, compatibili));
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

    private static IReadOnlyList<PiastraCompatibile> TrovaPiastreCompatibili(
        RigaOrdineVendita                riga,
        Dictionary<string, Cliente>      clientiByCodice,
        ILookup<int, ClientePiastra>     associazioniByCliente,
        Dictionary<int, Piastra>         piastreById,
        ILookup<int, Piastra>            specialiByCliente)
    {
        if (!CodiceArticoloPanthera.TryEstraiFormato(riga.CodiceArticolo, out var formato) || formato == 0)
            return [];
        if (!clientiByCodice.TryGetValue(riga.CodiceClienteGestionale, out var cliente))
            return [];

        var compatibili = new List<PiastraCompatibile>();

        foreach (var cp in associazioniByCliente[cliente.IdCliente])
        {
            // Le piastre soft-deleted non sono nel dizionario (HasQueryFilter): saltate.
            if (!piastreById.TryGetValue(cp.IdPiastra, out var piastra)) continue;
            if (CodiceArticoloPanthera.FormatoCompatibile(formato, piastra.Formato?.NomeFormato))
                compatibili.Add(new PiastraCompatibile(piastra, cp.Stato));
        }

        foreach (var piastra in specialiByCliente[cliente.IdCliente])
        {
            if (compatibili.Any(c => c.Piastra.IdPiastra == piastra.IdPiastra)) continue;
            if (CodiceArticoloPanthera.FormatoCompatibile(formato, piastra.Formato?.NomeFormato))
                compatibili.Add(new PiastraCompatibile(piastra, null));
        }

        return Ordina(compatibili);
    }

    // Prima le associazioni correnti, in fondo le Obsolete (comunque visibili).
    private static IReadOnlyList<PiastraCompatibile> Ordina(List<PiastraCompatibile> compatibili) =>
        [.. compatibili
            .OrderBy(c => c.IsObsoleta)
            .ThenBy(c => c.Piastra.CodicePiastra, StringComparer.OrdinalIgnoreCase)];

    private void AggiornaFiltro()
    {
        var f = FiltroRicerca.Trim().ToLower();

        RigheFiltrate.Clear();
        // La ricerca copre tutte le colonne della query, qualunque esse siano.
        foreach (var r in _tutte.Where(r =>
            string.IsNullOrEmpty(f)
            || r.Riga.Valori.Any(v => v.ToLower().Contains(f))))
        {
            RigheFiltrate.Add(r);
        }
    }

    /// <summary>Ri-esegue il match cliente+formato per la riga indicata (senza ripetere la
    /// query DB2) — usata dopo che l'utente ha associato una piastra al cliente tramite
    /// <c>AssociaPiastraOrdineWindow</c>.</summary>
    public async Task RicaricaRigaAsync(RigaOrdineVenditaRow vecchia)
    {
        var nuova = new RigaOrdineVenditaRow(
            vecchia.Riga, await TrovaPiastreCompatibiliAsync(vecchia.Riga));

        var idxTutte = _tutte.IndexOf(vecchia);
        if (idxTutte >= 0) _tutte[idxTutte] = nuova;

        var idxFiltrate = RigheFiltrate.IndexOf(vecchia);
        if (idxFiltrate >= 0) RigheFiltrate[idxFiltrate] = nuova;
    }

    // Variante a query mirate del match batch: usata solo per il refresh di una singola riga.
    private async Task<IReadOnlyList<PiastraCompatibile>> TrovaPiastreCompatibiliAsync(RigaOrdineVendita riga)
    {
        if (!CodiceArticoloPanthera.TryEstraiFormato(riga.CodiceArticolo, out var formato) || formato == 0)
            return [];

        var cliente = await _clientiRepo.GetByCodiceGestionaleAsync(riga.CodiceClienteGestionale);
        if (cliente is null) return [];

        var compatibili = new List<PiastraCompatibile>();

        foreach (var cp in await _clientiPiastreRepo.GetByClienteAsync(cliente.IdCliente))
        {
            // Piastra null se soft-deleted (HasQueryFilter la esclude dall'Include).
            if (cp.Piastra is null) continue;
            if (CodiceArticoloPanthera.FormatoCompatibile(formato, cp.Piastra.Formato?.NomeFormato))
                compatibili.Add(new PiastraCompatibile(cp.Piastra, cp.Stato));
        }

        foreach (var piastra in await _piastreRepo.GetByClienteEsclusivoAsync(cliente.IdCliente))
        {
            if (compatibili.Any(c => c.Piastra.IdPiastra == piastra.IdPiastra)) continue;
            if (CodiceArticoloPanthera.FormatoCompatibile(formato, piastra.Formato?.NomeFormato))
                compatibili.Add(new PiastraCompatibile(piastra, null));
        }

        return Ordina(compatibili);
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
