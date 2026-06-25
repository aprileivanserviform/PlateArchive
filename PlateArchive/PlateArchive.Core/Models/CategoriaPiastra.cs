namespace PlateArchive.Core.Models;

public class CategoriaPiastra
{
    public int    IdCategoriaPiastra { get; set; }
    public string Codice             { get; set; } = string.Empty;
    public string Descrizione        { get; set; } = string.Empty;
    public int    Ordine             { get; set; }
}
