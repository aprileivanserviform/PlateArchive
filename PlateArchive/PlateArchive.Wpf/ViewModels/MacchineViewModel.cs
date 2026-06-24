namespace PlateArchive.Wpf.ViewModels;

public class MacchineViewModel : ViewModelBase
{
    private string _filtroRicerca = string.Empty;

    public string Titolo => "Macchine";

    public string FiltroRicerca
    {
        get => _filtroRicerca;
        set => SetField(ref _filtroRicerca, value);
    }
}
