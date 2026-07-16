namespace PlateArchive.Core.Models;

public class AllegatoCliente
{
    public int      IdAllegato       { get; set; }
    public int      IdCliente        { get; set; }
    public string   NomeFile         { get; set; } = string.Empty;
    public string   PercorsoFile     { get; set; } = string.Empty;
    public string?  Descrizione      { get; set; }
    public long     DimensioneBytes  { get; set; }
    public DateTime DataCaricamento  { get; set; }

    public Cliente? Cliente          { get; set; }
}
