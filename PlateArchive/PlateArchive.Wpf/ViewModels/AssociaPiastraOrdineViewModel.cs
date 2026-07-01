using System.Collections.ObjectModel;
using System.Windows.Input;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;
using PlateArchive.Wpf.Commands;

namespace PlateArchive.Wpf.ViewModels;

/// <summary>
/// ViewModel del dialog "Associa piastra" — collega l'articolo di una riga ordine (che non ha
/// ancora corrispondenza) a una piastra esistente, impostandone il <c>CodiceArticoloGestionale</c>.
/// Aperto da <c>OrdiniVenditaView</c> per le righe con <c>PiastraNonTrovata</c>.
/// </summary>
public class AssociaPiastraOrdineViewModel : ViewModelBase
{
    private readonly IPiastraRepository _piastreRepo;

    private List<Piastra> _tuttePiastre = [];

    private string   _codiceArticolo = string.Empty;
    private string   _filtroPiastra  = string.Empty;
    private Piastra? _piastraSelezionata;
    private string?  _errore;
    private bool     _confermato;

    public AssociaPiastraOrdineViewModel(IPiastraRepository piastreRepo)
    {
        _piastreRepo = piastreRepo;

        ConfermaCommand = new RelayCommand(async _ => await ConfermaAsync(), _ => PiastraSelezionata is not null);
        AnnullaCommand  = new RelayCommand(_ => Annulla());
        SelezionaPiastraCommand         = new RelayCommand(p => PiastraSelezionata = (Piastra)p!);
        RimuoviPiastraSelezionataCommand = new RelayCommand(_ => PiastraSelezionata = null);
    }

    public string CodiceArticolo
    {
        get => _codiceArticolo;
        private set => SetField(ref _codiceArticolo, value);
    }

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

    public async Task InitAsync(string codiceArticolo)
    {
        CodiceArticolo = codiceArticolo;
        _tuttePiastre  = (await _piastreRepo.GetAllAsync()).ToList();
    }

    private void AggiornaSuggerimenti()
    {
        PiastreSuggerite.Clear();
        var f = _filtroPiastra.Trim().ToLower();
        if (!string.IsNullOrEmpty(f))
        {
            foreach (var p in _tuttePiastre
                .Where(p => p.CodicePiastra.ToLower().Contains(f)
                         || (p.Descrizione?.ToLower().Contains(f) ?? false))
                .Take(8))
                PiastreSuggerite.Add(p);
        }
        OnPropertyChanged(nameof(IsSuggerimentiVisible));
    }

    private async Task ConfermaAsync()
    {
        if (PiastraSelezionata is null) return;
        Errore = null;

        try
        {
            PiastraSelezionata.CodiceArticoloGestionale = CodiceArticolo;
            await _piastreRepo.UpdateAsync(PiastraSelezionata);
        }
        catch (Exception ex)
        {
            Errore = $"Impossibile associare la piastra: {ex.Message}";
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
}
