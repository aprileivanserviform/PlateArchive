namespace PlateArchive.Core.Models;

public class ProduttoreMacchina
{
    public int    IdProduttore   { get; set; }
    public string NomeProduttore { get; set; } = string.Empty;
    public bool   IsEliminata    { get; set; } = false;
    public string? Note          { get; set; }

    public ICollection<MacchinaStandard> Macchine { get; set; } = [];
}
