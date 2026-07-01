using PlateArchive.Core.Enums;

namespace PlateArchive.Core.Models;

/// <summary>
/// Tabella di giunzione N:N tra <see cref="Piastra"/> e <see cref="MacchinaStandard"/>.
/// Registra la compatibilità tecnica tra una piastra e un modello di macchina.
/// La coppia (IdPiastra, IdMacchinaStandard) è univoca nel database.
/// </summary>
public class PiastraMacchinaCompatibile
{
    public int IdCompatibilita    { get; set; }
    public int IdPiastra          { get; set; }
    public int IdMacchinaStandard { get; set; }

    /// <summary>Come è stata verificata la compatibilità (es. test fisico, scheda tecnica, ereditata da formato).</summary>
    public FonteDatoCompatibilita? FonteDato     { get; set; }
    public DateTime?               DataVerifica  { get; set; }
    public string?                 UtenteVerifica { get; set; }

    /// <summary>False = compatibilità sospesa/non più valida senza eliminarla dal registro storico.</summary>
    public bool    Attiva { get; set; } = true;
    public string? Note   { get; set; }

    // ─── Navigazioni ─────────────────────────────────────────────────────────
    public Piastra          Piastra          { get; set; } = null!;
    public MacchinaStandard MacchinaStandard { get; set; } = null!;
}
