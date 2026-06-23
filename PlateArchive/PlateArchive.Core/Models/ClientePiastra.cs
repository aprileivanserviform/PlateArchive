using PlateArchive.Core.Enums;

namespace PlateArchive.Core.Models;

public class ClientePiastra
{
    public int IdClientePiastra { get; set; }
    public int IdCliente { get; set; }
    public int IdPiastra { get; set; }
    public int? IdClienteMacchina { get; set; }
    public DateTime DataAssociazione { get; set; }
    public StatoClientePiastra Stato { get; set; }
    public string? Note { get; set; }

    public Cliente Cliente { get; set; } = null!;
    public Piastra Piastra { get; set; } = null!;
    public ClienteMacchina? ClienteMacchina { get; set; }
}
