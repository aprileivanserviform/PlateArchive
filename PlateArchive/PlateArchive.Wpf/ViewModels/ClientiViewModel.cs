namespace PlateArchive.Wpf.ViewModels;

public class ClientiViewModel : ViewModelBase
{
    private string _filtroRicerca = string.Empty;

    public string Titolo => "Clienti";

    public string FiltroRicerca
    {
        get => _filtroRicerca;
        set => SetField(ref _filtroRicerca, value);
    }
}
