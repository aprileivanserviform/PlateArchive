using System.Collections.ObjectModel;
using System.Windows.Input;
using PlateArchive.Core.Enums;
using PlateArchive.Core.Models;
using PlateArchive.Core.Servizi;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Wpf.Commands;

namespace PlateArchive.Wpf.ViewModels;

/// <summary>
/// ViewModel del dialog "Associa piastra" — associa una piastra esistente al cliente della
/// riga ordine (crea la <c>ClientePiastra</c>, stato Attiva): il collegamento è commerciale
/// (cliente ↔ piastra) e il match negli ordini avviene per cliente + formato (TASK-18/20).
/// Aperto da <c>OrdiniVenditaView</c> per le righe con <c>PiastraNonTrovata</c>.
/// La piastra scelta comparirà nel match automatico solo se il suo formato coincide con
/// quello del codice articolo: in caso contrario il dialog mostra un avviso non bloccante.
/// </summary>
public class AssociaPiastraOrdineViewModel : ViewModelBase
{
    private readonly IPiastraRepository        _piastreRepo;
    private readonly IClienteRepository        _clientiRepo;
    private readonly IClientePiastraRepository _clientiPiastreRepo;

    private List<Piastra> _tuttePiastre = [];
    private Cliente?      _cliente;
    private decimal?      _formatoCodice;

    private string   _codiceArticolo      = string.Empty;
    private string   _descrizioneArticolo = string.Empty;
    private string   _filtroPiastra       = string.Empty;
    private Piastra? _piastraSelezionata;
    private string?  _errore;
    private string?  _avviso;
    private bool     _confermato;

    public AssociaPiastraOrdineViewModel(
        IPiastraRepository        piastreRepo,
        IClienteRepository        clientiRepo,
        IClientePiastraRepository clientiPiastreRepo)
    {
        _piastreRepo        = piastreRepo;
        _clientiRepo        = clientiRepo;
        _clientiPiastreRepo = clientiPiastreRepo;

        ConfermaCommand = new RelayCommand(
            async _ => await ConfermaAsync(),
            _ => PiastraSelezionata is not null && _cliente is not null);
        AnnullaCommand  = new RelayCommand(_ => Annulla());
        SelezionaPiastraCommand          = new RelayCommand(p => PiastraSelezionata = (Piastra)p!);
        RimuoviPiastraSelezionataCommand = new RelayCommand(_ => PiastraSelezionata = null);
    }

    public string CodiceArticolo
    {
        get => _codiceArticolo;
        private set => SetField(ref _codiceArticolo, value);
    }

    /// <summary>Descrizione estesa dell'articolo (DESCR_ESTESA), letta dalla riga ordine.</summary>
    public string DescrizioneArticolo
    {
        get => _descrizioneArticolo;
        private set
        {
            if (SetField(ref _descrizioneArticolo, value))
                OnPropertyChanged(nameof(IsDescrizioneArticoloVisible));
        }
    }

    public bool IsDescrizioneArticoloVisible => !string.IsNullOrEmpty(_descrizioneArticolo);

    /// <summary>Ragione sociale del cliente della riga ordine (a cui verrà associata la piastra).</summary>
    public string RagioneSocialeCliente => _cliente?.RagioneSociale ?? string.Empty;

    public ObservableCollection<Piastra> PiastreSuggerite { get; } = [];

    public string FiltroPiastra
    {
        get => _filtroPiastra;
        set { if (SetField(ref _filtroPiastra, value)) AggiornaSuggerimenti(); }
    }

    public Piastra? PiastraSelezionata
    {
        get => _piastraSelezionata;
        set
        {
            if (SetField(ref _piastraSelezionata, value))
            {
                if (value is not null)
                {
                    _filtroPiastra = string.Empty;
                    OnPropertyChanged(nameof(FiltroPiastra));
                    PiastreSuggerite.Clear();
                    OnPropertyChanged(nameof(IsSuggerimentiVisible));
                }
                AggiornaAvvisoFormato();
                OnPropertyChanged(nameof(IsPiastraSelezionataVisible));
                OnPropertyChanged(nameof(IsPiastraSearchVisible));
            }
        }
    }

    public bool IsPiastraSelezionataVisible => PiastraSelezionata is not null;
    public bool IsPiastraSearchVisible      => PiastraSelezionata is null;
    public bool IsSuggerimentiVisible       => PiastreSuggerite.Count > 0;

    public string? Errore
    {
        get => _errore;
        set { if (SetField(ref _errore, value)) OnPropertyChanged(nameof(IsErroreVisible)); }
    }

    public bool IsErroreVisible => !string.IsNullOrEmpty(_errore);

    /// <summary>Avviso non bloccante: formato piastra assente o diverso da quello del codice.</summary>
    public string? Avviso
    {
        get => _avviso;
        private set { if (SetField(ref _avviso, value)) OnPropertyChanged(nameof(IsAvvisoVisible)); }
    }

    public bool IsAvvisoVisible => !string.IsNullOrEmpty(_avviso);

    public bool Confermato
    {
        get => _confermato;
        private set => SetField(ref _confermato, value);
    }

    public event EventHandler? RichiestaChiusura;

    public ICommand ConfermaCommand                  { get; }
    public ICommand AnnullaCommand                   { get; }
    public ICommand SelezionaPiastraCommand          { get; }
    public ICommand RimuoviPiastraSelezionataCommand { get; }

    public async Task InitAsync(string codiceArticolo, string codiceClienteGestionale, string descrizioneArticolo = "")
    {
        CodiceArticolo      = codiceArticolo;
        DescrizioneArticolo = descrizioneArticolo;

        _formatoCodice = CodiceArticoloPanthera.TryEstraiFormato(codiceArticolo, out var formato)
            ? formato : null;

        _tuttePiastre = (await _piastreRepo.GetAllAsync()).ToList();

        _cliente = string.IsNullOrWhiteSpace(codiceClienteGestionale)
            ? null
            : await _clientiRepo.GetByCodiceGestionaleAsync(codiceClienteGestionale);
        if (_cliente is null)
            Errore = "Cliente della riga ordine non trovato in PlateArchive: " +
                     "sincronizzare i clienti dal gestionale prima di associare la piastra.";
        OnPropertyChanged(nameof(RagioneSocialeCliente));

        AggiornaSuggerimenti();
    }

    private void AggiornaSuggerimenti()
    {
        PiastreSuggerite.Clear();
        var f = _filtroPiastra.Trim().ToLower();
        var candidate = string.IsNullOrEmpty(f)
            // A filtro vuoto proponiamo le piastre col formato richiesto dal codice articolo:
            // sono le uniche che parteciperanno al match automatico.
            ? _tuttePiastre.Where(p => _formatoCodice is not null
                && CodiceArticoloPanthera.FormatoCompatibile(_formatoCodice.Value, p.Formato?.NomeFormato))
            : _tuttePiastre.Where(p => p.CodicePiastra.ToLower().Contains(f)
                || (p.Descrizione?.ToLower().Contains(f) ?? false));

        foreach (var p in candidate.Take(8))
            PiastreSuggerite.Add(p);

        OnPropertyChanged(nameof(IsSuggerimentiVisible));
    }

    private void AggiornaAvvisoFormato()
    {
        var piastra = PiastraSelezionata;
        if (piastra is null || _formatoCodice is null)
        {
            Avviso = null;
            return;
        }

        if (piastra.Formato is null)
            Avviso = $"La piastra non ha un formato impostato: non comparirà nel match " +
                     $"automatico dell'articolo (formato {FormatoTesto(_formatoCodice.Value)}).";
        else if (!CodiceArticoloPanthera.FormatoCompatibile(_formatoCodice.Value, piastra.Formato.NomeFormato))
            Avviso = $"La piastra ha formato {piastra.Formato.NomeFormato}, l'articolo richiede " +
                     $"{FormatoTesto(_formatoCodice.Value)}: non comparirà nel match automatico.";
        else
            Avviso = null;
    }

    private static string FormatoTesto(decimal formato) => formato.ToString("0.#");

    private async Task ConfermaAsync()
    {
        if (PiastraSelezionata is null || _cliente is null) return;
        Errore = null;

        try
        {
            var esiste = await _clientiPiastreRepo.ExistsAsync(_cliente.IdCliente, PiastraSelezionata.IdPiastra);
            if (!esiste)
            {
                await _clientiPiastreRepo.AddAsync(new ClientePiastra
                {
                    IdCliente        = _cliente.IdCliente,
                    IdPiastra        = PiastraSelezionata.IdPiastra,
                    DataAssociazione = DateTime.UtcNow,
                    Stato            = StatoClientePiastra.Attiva,
                    Cliente          = _cliente,
                    Piastra          = PiastraSelezionata
                });
            }
        }
        catch (Exception ex)
        {
            // L'errore è mostrato in linea nella finestra, non in un MessageBox separato.
            Errore = $"Impossibile associare la piastra: {App.CausaErrore(ex) ?? ex.Message}";
            return;
        }

        Confermato = true;
        RichiestaChiusura?.Invoke(this, EventArgs.Empty);
    }

    private void Annulla()
    {
        Confermato = false;
        RichiestaChiusura?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Chiamato dal code-behind quando una nuova piastra è stata creata tramite drag&amp;drop
    /// del disegno (flusso ImportaDisegnoWindow): il dialog si chiude come confermato,
    /// così la riga ordine viene ricaricata e trova la piastra appena creata.
    /// </summary>
    public void SegnalaPiastraCreata()
    {
        Confermato = true;
        RichiestaChiusura?.Invoke(this, EventArgs.Empty);
    }
}
