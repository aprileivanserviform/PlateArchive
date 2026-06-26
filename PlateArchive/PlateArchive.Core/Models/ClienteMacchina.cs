namespace PlateArchive.Core.Models;

/// <summary>
/// Associazione commerciale tra un <see cref="Cliente"/> e un modello di <see cref="MacchinaStandard"/>.
/// Rappresenta il fatto che un cliente possiede o utilizza una macchina di quel modello.
/// Non è la stessa cosa della compatibilità tecnica piastra-macchina
/// (quella è in <see cref="PiastraMacchinaCompatibile"/>).
/// </summary>
public class ClienteMacchina
{
    public int    IdClienteMacchina  { get; set; }
    public int    IdCliente          { get; set; }
    public int    IdMacchinaStandard { get; set; }

    /// <summary>Numero di serie / matricola dell'unità fisica del cliente (facoltativo).</summary>
    public string? Matricola              { get; set; }

    /// <summary>Eventuale codice interno usato dal cliente per identificare la propria macchina.</summary>
    public string? CodiceInternoCliente   { get; set; }
    public DateTime DataAssociazione      { get; set; }
    public bool     Attiva                { get; set; } = true;
    public string?  Note                  { get; set; }

    // ─── Navigazioni ─────────────────────────────────────────────────────────
    public Cliente          Cliente          { get; set; } = null!;
    public MacchinaStandard MacchinaStandard { get; set; } = null!;
}
