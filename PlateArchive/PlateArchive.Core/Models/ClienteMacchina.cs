namespace PlateArchive.Core.Models;

public class ClienteMacchina
{
    public int IdClienteMacchina { get; set; }
    public int IdCliente { get; set; }
    public int IdMacchinaStandard { get; set; }
    public string? Matricola { get; set; }
    public string? CodiceInternoCliente { get; set; }
    public DateTime DataAssociazione { get; set; }
    public bool Attiva { get; set; } = true;
    public string? Note { get; set; }

    public Cliente Cliente { get; set; } = null!;
    public MacchinaStandard MacchinaStandard { get; set; } = null!;
}
