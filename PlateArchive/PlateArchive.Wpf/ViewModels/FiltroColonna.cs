using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace PlateArchive.Wpf.ViewModels;

public enum FiltroColonnaTipo { Testo, Numerico, Data, Enum }

public enum FiltroColonnaOp
{
    Nessuno = 0,
    UgualeA,
    DiversoDa,
    Contiene,
    NonContiene,
    MaggioreDi,
    MaggioreUgualeDi,
    MinoreDi,
    MinoreUgualeDi,
    Tra,
    Vuoto,
    NonVuoto
}

/// <summary>
/// Stato del filtro per una singola colonna del DataGrid Piastre.
/// Ogni istanza è associata a una colonna; il ViewModel registra su Cambiato per
/// rieseguire AggiornaFiltro() ogni volta che l'utente cambia operatore o valore.
/// </summary>
public class FiltroColonna : INotifyPropertyChanged
{
    private FiltroColonnaOp _op         = FiltroColonnaOp.Nessuno;
    private string          _v1         = string.Empty;
    private string          _v2         = string.Empty;
    private bool            _popupOpen;

    private static readonly CultureInfo  Inv = CultureInfo.InvariantCulture;
    private static readonly NumberStyles Ns  = NumberStyles.Any;

    public FiltroColonna(string titolo, FiltroColonnaTipo tipo)
    {
        Titolo = titolo;
        Tipo   = tipo;
    }

    public string            Titolo { get; }
    public FiltroColonnaTipo Tipo   { get; }

    // Valori disponibili per colonne Enum (popolati dopo LoadAsync)
    public List<string> ValoriEnum { get; } = [];

    // ─── Operatore ────────────────────────────────────────────────────────────

    public FiltroColonnaOp Operatore
    {
        get => _op;
        set
        {
            if (_op == value) return;
            // Reset valori quando si rimuove il filtro
            if (value == FiltroColonnaOp.Nessuno) { _v1 = _v2 = string.Empty; IsPopupOpen = false; }
            _op = value;
            NotifyAll();
            Cambiato?.Invoke();
        }
    }

    public string Valore1
    {
        get => _v1;
        set { if (_v1 == value) return; _v1 = value; Notify(); Cambiato?.Invoke(); }
    }

    public string Valore2
    {
        get => _v2;
        set { if (_v2 == value) return; _v2 = value; Notify(); Cambiato?.Invoke(); }
    }

    // Legato al ToggleButton nell'header e a Popup.IsOpen in modo TwoWay
    public bool IsPopupOpen
    {
        get => _popupOpen;
        set { if (_popupOpen == value) return; _popupOpen = value; Notify(); }
    }

    // ─── Proprietà derivate (usate come binding in XAML) ─────────────────────

    public bool IsAttivo        => _op != FiltroColonnaOp.Nessuno;
    public bool IsTra           => _op == FiltroColonnaOp.Tra;

    // Input valore visibile: true quando l'operatore richiede un valore
    public bool MostraInput      => _op is not (FiltroColonnaOp.Nessuno or FiltroColonnaOp.Vuoto or FiltroColonnaOp.NonVuoto);
    public bool MostraTextInput  => MostraInput && Tipo != FiltroColonnaTipo.Enum;
    public bool MostraEnumInput  => MostraInput && Tipo == FiltroColonnaTipo.Enum;

    // Visibilità dei gruppi di operatori nel popup
    public bool MostraContiene    => Tipo == FiltroColonnaTipo.Testo;
    public bool MostraComparazione => Tipo is FiltroColonnaTipo.Numerico or FiltroColonnaTipo.Data;

    public event Action? Cambiato;
    public event PropertyChangedEventHandler? PropertyChanged;

    private void Notify([CallerMemberName] string? n = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

    private void NotifyAll()
    {
        Notify(nameof(Operatore));
        Notify(nameof(Valore1));
        Notify(nameof(Valore2));
        Notify(nameof(IsAttivo));
        Notify(nameof(IsTra));
        Notify(nameof(MostraInput));
        Notify(nameof(MostraTextInput));
        Notify(nameof(MostraEnumInput));
    }

    // ─── Logica di filtro ─────────────────────────────────────────────────────

    public bool ApplicaA(object? valore)
    {
        if (_op == FiltroColonnaOp.Nessuno) return true;

        var s = valore?.ToString() ?? string.Empty;

        if (_op == FiltroColonnaOp.Vuoto)   return string.IsNullOrWhiteSpace(s);
        if (_op == FiltroColonnaOp.NonVuoto) return !string.IsNullOrWhiteSpace(s);

        return Tipo switch
        {
            FiltroColonnaTipo.Testo or FiltroColonnaTipo.Enum => ApplicaTesto(s),
            FiltroColonnaTipo.Numerico                        => ApplicaNumerico(s),
            FiltroColonnaTipo.Data                            => ApplicaData(s),
            _                                                 => true
        };
    }

    private bool ApplicaTesto(string s)
    {
        // Valore1 vuoto → nessun filtro attivo (l'utente non ha ancora digitato)
        if (string.IsNullOrEmpty(_v1)) return true;
        return _op switch
        {
            FiltroColonnaOp.UgualeA     => s.Equals(_v1, StringComparison.OrdinalIgnoreCase),
            FiltroColonnaOp.DiversoDa   => !s.Equals(_v1, StringComparison.OrdinalIgnoreCase),
            FiltroColonnaOp.Contiene    => s.Contains(_v1, StringComparison.OrdinalIgnoreCase),
            FiltroColonnaOp.NonContiene => !s.Contains(_v1, StringComparison.OrdinalIgnoreCase),
            _                           => true
        };
    }

    private bool ApplicaNumerico(string s)
    {
        if (!decimal.TryParse(s.Replace(',', '.'), Ns, Inv, out var n)) return false;
        if (!decimal.TryParse(_v1.Replace(',', '.'), Ns, Inv, out var v1)) return true;
        return _op switch
        {
            FiltroColonnaOp.UgualeA          => n == v1,
            FiltroColonnaOp.DiversoDa        => n != v1,
            FiltroColonnaOp.MaggioreDi       => n > v1,
            FiltroColonnaOp.MaggioreUgualeDi => n >= v1,
            FiltroColonnaOp.MinoreDi         => n < v1,
            FiltroColonnaOp.MinoreUgualeDi   => n <= v1,
            FiltroColonnaOp.Tra              => decimal.TryParse(_v2.Replace(',', '.'), Ns, Inv, out var v2) && n >= v1 && n <= v2,
            _                                => true
        };
    }

    private bool ApplicaData(string s)
    {
        if (!DateTime.TryParse(s, out var d)) return false;
        if (!DateTime.TryParse(_v1, out var d1)) return true;
        return _op switch
        {
            FiltroColonnaOp.UgualeA          => d.Date == d1.Date,
            FiltroColonnaOp.DiversoDa        => d.Date != d1.Date,
            FiltroColonnaOp.MaggioreDi       => d.Date > d1.Date,
            FiltroColonnaOp.MaggioreUgualeDi => d.Date >= d1.Date,
            FiltroColonnaOp.MinoreDi         => d.Date < d1.Date,
            FiltroColonnaOp.MinoreUgualeDi   => d.Date <= d1.Date,
            FiltroColonnaOp.Tra              => DateTime.TryParse(_v2, out var d2) && d.Date >= d1.Date && d.Date <= d2.Date,
            _                                => true
        };
    }
}
