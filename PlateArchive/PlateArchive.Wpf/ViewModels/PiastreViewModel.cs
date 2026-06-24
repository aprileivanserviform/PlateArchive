namespace PlateArchive.Wpf.ViewModels;

public class PiastreViewModel : ViewModelBase
{
    private string _filtroRicerca = string.Empty;

    public string Titolo => "Piastre";

    public string FiltroRicerca
    {
        get => _filtroRicerca;
        set => SetField(ref _filtroRicerca, value);
    }
}
