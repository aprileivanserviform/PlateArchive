using System.Collections.ObjectModel;

namespace PlateArchive.Wpf.ViewModels;

/// <summary>
/// ViewModel del dialog "Scegli piastra" — mostrato da <c>OrdiniVenditaView</c> quando una
/// riga ordine ha più piastre compatibili (stesso cliente + stesso formato, TASK-18):
/// l'utente sceglie da quale aprire il disegno o il dettaglio. Le associazioni Obsolete
/// restano visibili ma marcate. Nessuna dipendenza da repository: riceve le
/// <see cref="PiastraCompatibile"/> già risolte dalla riga, quindi viene istanziato
/// direttamente (niente registrazione DI).
/// </summary>
public class SceltaPiastraOrdineViewModel(
    string codiceArticolo,
    string descrizioneArticolo,
    IReadOnlyList<PiastraCompatibile> piastreCompatibili) : ViewModelBase
{
    public string CodiceArticolo      { get; } = codiceArticolo;
    public string DescrizioneArticolo { get; } = descrizioneArticolo;

    public bool IsDescrizioneArticoloVisible => !string.IsNullOrEmpty(DescrizioneArticolo);

    public ObservableCollection<PiastraCompatibile> Piastre { get; } = [.. piastreCompatibili];
}
