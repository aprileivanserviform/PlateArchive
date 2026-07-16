namespace PlateArchive.Core.Models;

public class NotaTecnicaCliente
{
    public int      IdNota         { get; set; }
    public int      IdCliente      { get; set; }
    public string   Titolo         { get; set; } = string.Empty;
    public string?  Testo          { get; set; }
    public DateTime DataCreazione  { get; set; }
    public DateTime DataModifica   { get; set; }

    public Cliente? Cliente        { get; set; }
}
