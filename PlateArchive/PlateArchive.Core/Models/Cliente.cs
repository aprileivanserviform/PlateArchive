namespace PlateArchive.Core.Models;

public class Cliente
{
    public int IdCliente { get; set; }
    public string CodiceClienteGestionale { get; set; } = string.Empty;
    public string RagioneSociale { get; set; } = string.Empty;
    public string? Note { get; set; }

    public ICollection<ClienteMacchina> Macchine { get; set; } = [];
    public ICollection<ClientePiastra> Piastre { get; set; } = [];
}
