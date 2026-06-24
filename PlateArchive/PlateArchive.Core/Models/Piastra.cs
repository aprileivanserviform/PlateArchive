using PlateArchive.Core.Enums;

namespace PlateArchive.Core.Models;

public class Piastra
{
    public int IdPiastra { get; set; }
    public string CodicePiastra { get; set; } = string.Empty;
    public string? CodiceArticoloGestionale { get; set; }
    public string? Descrizione { get; set; }
    public StatoPiastra Stato { get; set; }
    public CategoriaPiastra? Categoria { get; set; }
    public string? FormatoMacchina { get; set; }
    public decimal? LarghezzaMm { get; set; }
    public decimal? LunghezzaMm { get; set; }
    public decimal? SpessoreMm { get; set; }
    public string? Note { get; set; }
    public DateTime DataCreazione { get; set; }
    public DateTime DataUltimaModifica { get; set; }

    public Disegno? Disegno { get; set; }
    public ICollection<PiastraMacchinaCompatibile> MacchineCompatibili { get; set; } = [];
    public ICollection<ClientePiastra> ClientiAssociati { get; set; } = [];
}
