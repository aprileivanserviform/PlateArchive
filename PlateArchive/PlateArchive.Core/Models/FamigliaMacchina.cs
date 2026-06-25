namespace PlateArchive.Core.Models;

public class FamigliaMacchina
{
    public int    IdFamiglia   { get; set; }
    public string NomeFamiglia { get; set; } = string.Empty;
    public bool   IsEliminata  { get; set; } = false;
    public string? Note        { get; set; }

    public ICollection<MacchinaStandard> Macchine { get; set; } = [];
}
