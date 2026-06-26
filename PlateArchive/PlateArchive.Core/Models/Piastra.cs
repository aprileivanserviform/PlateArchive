using PlateArchive.Core.Enums;

namespace PlateArchive.Core.Models;

/// <summary>
/// Entità centrale del sistema. Rappresenta una piastra tecnica dell'azienda.
/// Ogni piastra ha un codice interno univoco (<see cref="CodicePiastra"/>) e,
/// facoltativamente, un codice articolo gestionale (<see cref="CodiceArticoloGestionale"/>)
/// che la collega al gestionale commerciale.
/// Una piastra può essere compatibile con più modelli di macchina e associata a più clienti.
/// </summary>
public class Piastra
{
    public int     IdPiastra                { get; set; }

    /// <summary>Codice univoco interno (es. PLT-000001). Generato dall'app.</summary>
    public string  CodicePiastra            { get; set; } = string.Empty;

    /// <summary>Codice articolo nel gestionale commerciale (nullable: non tutte le piastre sono ancora censite).</summary>
    public string? CodiceArticoloGestionale { get; set; }
    public string? Descrizione              { get; set; }
    public StatoPiastra Stato               { get; set; }

    /// <summary>FK verso <see cref="CategoriaPiastra"/>: tipo/famiglia della piastra (es. Flessografica, Offset).</summary>
    public int?    IdCategoriaPiastra       { get; set; }

    /// <summary>FK verso <see cref="FormatoMacchina"/>: formato macchina a cui è destinata questa piastra.</summary>
    public int?    IdFormato                { get; set; }

    /// <summary>True = eliminata logicamente. Filtrata automaticamente da EF Core (HasQueryFilter).</summary>
    public bool    IsEliminata              { get; set; } = false;

    // ─── Misure fisiche ───────────────────────────────────────────────────────
    public decimal? LarghezzaMm { get; set; }
    public decimal? AltezzaMm   { get; set; }
    public decimal? SpessoreMm  { get; set; }
    public decimal? Durezza     { get; set; }
    public decimal? Peso        { get; set; }

    public string?  Note            { get; set; }
    public DateTime DataCreazione   { get; set; }
    public DateTime DataUltimaModifica { get; set; }

    // ─── Navigazioni ─────────────────────────────────────────────────────────

    public CategoriaPiastra? Categoria { get; set; }
    public FormatoMacchina?  Formato   { get; set; }

    /// <summary>Disegno tecnico associato (relazione 1:1 — ogni piastra ha al massimo un disegno corrente).</summary>
    public Disegno? Disegno { get; set; }

    /// <summary>Modelli di macchina con cui questa piastra è compatibile.</summary>
    public ICollection<PiastraMacchinaCompatibile> MacchineCompatibili { get; set; } = [];

    /// <summary>Clienti che possiedono questa piastra.</summary>
    public ICollection<ClientePiastra> ClientiAssociati { get; set; } = [];
}
