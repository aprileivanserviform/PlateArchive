using PlateArchive.Core.Enums;

namespace PlateArchive.Core.Models;

public class PiastraMacchinaCompatibile
{
    public int IdCompatibilita { get; set; }
    public int IdPiastra { get; set; }
    public int IdMacchinaStandard { get; set; }
    public FonteDatoCompatibilita? FonteDato { get; set; }
    public DateTime? DataVerifica { get; set; }
    public string? UtenteVerifica { get; set; }
    public bool Attiva { get; set; } = true;
    public string? Note { get; set; }

    public Piastra Piastra { get; set; } = null!;
    public MacchinaStandard MacchinaStandard { get; set; } = null!;
}
