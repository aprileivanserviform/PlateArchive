namespace PlateArchive.Core.Models;

public class MacchinaStandard
{
    public int IdMacchinaStandard { get; set; }
    public string CodiceMacchina { get; set; } = string.Empty;
    public string NomeMacchina { get; set; } = string.Empty;
    public string? Famiglia { get; set; }
    public string? Formato { get; set; }
    public string? Versione { get; set; }
    public string? Produttore { get; set; }
    public bool Attiva { get; set; } = true;
    public string? Note { get; set; }

    public ICollection<PiastraMacchinaCompatibile> PiastreCompatibili { get; set; } = [];
    public ICollection<ClienteMacchina> ClientiAssociati { get; set; } = [];
}
