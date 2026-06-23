using PlateArchive.Core.Enums;

namespace PlateArchive.Core.Models;

public class Disegno
{
    public int IdDisegno { get; set; }
    public int IdPiastra { get; set; }
    public string? CodiceDisegno { get; set; }
    public string? NomeFile { get; set; }
    public string? PercorsoFile { get; set; }
    public string? VaultId { get; set; }
    public string? Revisione { get; set; }
    public string? Formato { get; set; }
    public StatoDisegno Stato { get; set; }
    public DateTime? DataUltimaModificaFile { get; set; }
    public string? Note { get; set; }

    public Piastra Piastra { get; set; } = null!;
}
