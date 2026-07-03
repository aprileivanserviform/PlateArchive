namespace PlateArchive.Core.Models;

/// <summary>
/// Modello standard di macchina (catalogo prodotti).
/// Non è la macchina di un singolo cliente, ma il modello "astratto" che descrive
/// le caratteristiche tecniche condivise da tutte le unità di quel modello.
/// L'associazione cliente-macchina è in <see cref="ClientiAssociati"/>.
/// </summary>
public class MacchinaStandard
{
    public int    IdMacchinaStandard { get; set; }

    /// <summary>Codice univoco del modello macchina — normalizzato per evitare duplicati.</summary>
    public string CodiceMacchina    { get; set; } = string.Empty;
    public string NomeMacchina      { get; set; } = string.Empty;

    /// <summary>FK verso <see cref="FormatoMacchina"/>: determina quali piastre sono compatibili.</summary>
    public int?   IdFormato         { get; set; }
    public int?   IdProduttore      { get; set; }

    /// <summary>
    /// Dimensioni del foglio lavorabile dalla macchina di fustellatura, in millimetri.
    /// Il formato che la macchina può fustellare è compreso tra il minimo e il massimo
    /// (larghezza × altezza).
    /// </summary>
    public decimal? LarghezzaMinimaFoglioMm  { get; set; }
    public decimal? AltezzaMinimaFoglioMm    { get; set; }
    public decimal? LarghezzaMassimaFoglioMm { get; set; }
    public decimal? AltezzaMassimaFoglioMm   { get; set; }

    public string?  Versione        { get; set; }

    /// <summary>False = macchina fuori produzione/disabilitata (soft-disable, non è soft-delete).</summary>
    public bool     Attiva          { get; set; } = true;
    public string?  Note            { get; set; }

    // ─── Navigazioni ─────────────────────────────────────────────────────────

    public FormatoMacchina?    Formato    { get; set; }
    public ProduttoreMacchina? Produttore { get; set; }

    /// <summary>Piastre tecnicamente compatibili con questo modello di macchina.</summary>
    public ICollection<PiastraMacchinaCompatibile> PiastreCompatibili { get; set; } = [];

    /// <summary>Clienti che possiedono una o più unità di questo modello.</summary>
    public ICollection<ClienteMacchina>            ClientiAssociati   { get; set; } = [];
}
