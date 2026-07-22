using System.Collections.ObjectModel;
using System.Windows.Input;
using PlateArchive.Services;
using PlateArchive.Wpf.Commands;

namespace PlateArchive.Wpf.ViewModels;

/// <summary>
/// Sotto-ViewModel riusabile del selettore "Codice articolo gestionale" (TASK-19): autocomplete
/// sugli articoli reali del gestionale (cache di sessione via <see cref="IArticoliGestionaleService"/>),
/// pattern chip + suggerimenti come il selettore cliente esclusivo. Usato dai form piastra
/// (PiastreView, NuovaPiastraDialog, ImportaDisegnoWindow) quando la categoria è Speciale Cliente.
/// Se il gestionale non è raggiungibile (VPN), degrada a campo di testo libero
/// (<see cref="CodiceLibero"/>) senza bloccare il form.
/// </summary>
public class SelettoreArticoloGestionaleViewModel : ViewModelBase
{
    private readonly IArticoliGestionaleService _articoliService;

    private List<ArticoloGestionale> _tutti = [];
    private bool                _caricato;
    private string              _filtro       = string.Empty;
    private string              _codiceLibero = string.Empty;
    private ArticoloGestionale? _selezionato;
    private string?             _errore;
    private string?             _avviso;
    private bool                _isCaricamento;

    public SelettoreArticoloGestionaleViewModel(IArticoliGestionaleService articoliService)
    {
        _articoliService = articoliService;

        SelezionaArticoloCommand = new RelayCommand(a =>
        {
            ArticoloSelezionato = (ArticoloGestionale)a!;
            ArticoloScelto?.Invoke(ArticoloSelezionato!);
        });
        RimuoviArticoloCommand = new RelayCommand(_ => { ArticoloSelezionato = null; Avviso = null; });
        RicaricaCommand        = new RelayCommand(async _ => await CaricaAsync(forzaRicarica: true));
    }

    /// <summary>Scatta solo alla scelta esplicita dell'utente (non al ripristino in modifica):
    /// l'host la usa per proporre il formato derivato dal codice.</summary>
    public event Action<ArticoloGestionale>? ArticoloScelto;

    public ICommand SelezionaArticoloCommand { get; }
    public ICommand RimuoviArticoloCommand   { get; }
    public ICommand RicaricaCommand          { get; }

    public ObservableCollection<ArticoloGestionale> Suggerimenti { get; } = [];

    /// <summary>True se il selettore funziona con la lista del gestionale;
    /// false = fallback a testo libero (connessione non configurata o non raggiungibile).</summary>
    public bool IsSelettoreAttivo => _articoliService.IsDisponibile && _errore is null;
    public bool IsFallbackLibero  => !IsSelettoreAttivo;

    public string Filtro
    {
        get => _filtro;
        set { if (SetField(ref _filtro, value)) AggiornaSuggerimenti(); }
    }

    /// <summary>Testo libero usato solo quando il gestionale non è disponibile.</summary>
    public string CodiceLibero
    {
        get => _codiceLibero;
        set => SetField(ref _codiceLibero, value);
    }

    public ArticoloGestionale? ArticoloSelezionato
    {
        get => _selezionato;
        private set
        {
            if (SetField(ref _selezionato, value))
            {
                if (value is not null)
                {
                    _filtro = string.Empty;
                    OnPropertyChanged(nameof(Filtro));
                    Suggerimenti.Clear();
                    OnPropertyChanged(nameof(IsSuggerimentiVisible));
                }
                OnPropertyChanged(nameof(IsArticoloSelezionatoVisible));
                OnPropertyChanged(nameof(IsRicercaVisible));
            }
        }
    }

    public bool IsArticoloSelezionatoVisible => ArticoloSelezionato is not null;
    public bool IsRicercaVisible             => ArticoloSelezionato is null;
    public bool IsSuggerimentiVisible        => Suggerimenti.Count > 0;

    public string? Errore
    {
        get => _errore;
        private set
        {
            if (SetField(ref _errore, value))
            {
                OnPropertyChanged(nameof(IsErroreVisible));
                OnPropertyChanged(nameof(IsSelettoreAttivo));
                OnPropertyChanged(nameof(IsFallbackLibero));
            }
        }
    }

    public bool IsErroreVisible => !string.IsNullOrEmpty(_errore);

    /// <summary>Avviso non bloccante impostato dall'host (es. formato del codice scelto
    /// non presente tra i FormatiMacchine locali).</summary>
    public string? Avviso
    {
        get => _avviso;
        set { if (SetField(ref _avviso, value)) OnPropertyChanged(nameof(IsAvvisoVisible)); }
    }

    public bool IsAvvisoVisible => !string.IsNullOrEmpty(_avviso);

    public bool IsCaricamento
    {
        get => _isCaricamento;
        private set => SetField(ref _isCaricamento, value);
    }

    /// <summary>Codice da salvare sulla piastra: articolo scelto, o testo libero in fallback.</summary>
    public string? CodiceCorrente
    {
        get
        {
            var codice = IsFallbackLibero ? _codiceLibero.Trim() : ArticoloSelezionato?.Codice;
            return string.IsNullOrWhiteSpace(codice) ? null : codice;
        }
    }

    /// <summary>
    /// Prepara il selettore per un nuovo form: carica (o riusa) la lista articoli e ripristina
    /// l'eventuale codice già salvato sulla piastra (chip, senza scatenare
    /// <see cref="ArticoloScelto"/>). In fallback il codice finisce nel testo libero.
    /// </summary>
    public async Task InitAsync(string? codiceEsistente = null)
    {
        Filtro       = string.Empty;
        CodiceLibero = codiceEsistente ?? string.Empty;
        Avviso       = null;
        ArticoloSelezionato = null;
        Suggerimenti.Clear();
        OnPropertyChanged(nameof(IsSuggerimentiVisible));

        await CaricaAsync(forzaRicarica: false);

        if (IsSelettoreAttivo && !string.IsNullOrWhiteSpace(codiceEsistente))
        {
            // Codice salvato in precedenza: chip con la descrizione presa dall'anagrafica,
            // o solo il codice se non più presente a gestionale.
            ArticoloSelezionato =
                _tutti.FirstOrDefault(a => a.Codice.Equals(codiceEsistente.Trim(), StringComparison.OrdinalIgnoreCase))
                ?? new ArticoloGestionale(codiceEsistente.Trim(), string.Empty);
        }
    }

    private async Task CaricaAsync(bool forzaRicarica)
    {
        if (!_articoliService.IsDisponibile)
        {
            Errore = "Connessione al gestionale (DB2) non configurata: inserire il codice manualmente.";
            return;
        }
        if (_caricato && !forzaRicarica) { Errore = null; return; }

        IsCaricamento = true;
        try
        {
            _tutti    = (await _articoliService.GetArticoliAsync(forzaRicarica)).ToList();
            _caricato = true;
            Errore    = null;
        }
        catch (Exception ex)
        {
            Errore = $"Articoli non leggibili dal gestionale ({ex.Message}): inserire il codice manualmente.";
        }
        finally
        {
            IsCaricamento = false;
            AggiornaSuggerimenti();
        }
    }

    private void AggiornaSuggerimenti()
    {
        Suggerimenti.Clear();
        var f = _filtro.Trim().ToLower();
        if (!string.IsNullOrEmpty(f))
        {
            foreach (var a in _tutti
                .Where(a => a.Codice.ToLower().Contains(f)
                         || a.Descrizione.ToLower().Contains(f))
                .Take(8))
                Suggerimenti.Add(a);
        }
        OnPropertyChanged(nameof(IsSuggerimentiVisible));
    }
}
