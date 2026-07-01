using PlateArchive.Core.Enums;

namespace PlateArchive.Core.Models;

/// <summary>
/// Entità centrale del sistema. Rappresenta una piastra tecnica dell'azienda.
/// <para>
/// <see cref="TipoPiastra"/> distingue le piastre del catalogo condiviso (Standard)
/// dalle varianti personalizzate per un singolo cliente (SpecialeCliente).
/// Le piastre speciali hanno <see cref="IdClienteEsclusivo"/> valorizzato e vengono
/// salvate nella sottocartella Clienti\{CodiceCliente}\ dell'archivio condiviso.
/// </para>
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

    /// <summary>Standard = catalogo condiviso; SpecialeCliente = variante esclusiva di un cliente.</summary>
    public TipoPiastra TipoPiastra { get; set; } = TipoPiastra.Standard;

    /// <summary>
    /// FK verso <see cref="Cliente"/>: valorizzato solo quando <see cref="TipoPiastra"/> = SpecialeCliente.
    /// Determina la sottocartella di archiviazione del file disegno.
    /// </summary>
    public int? IdClienteEsclusivo { get; set; }

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

    public CategoriaPiastra? Categoria        { get; set; }
    public FormatoMacchina?  Formato          { get; set; }
    public Cliente?          ClienteEsclusivo { get; set; }

    /// <summary>Disegno tecnico associato (relazione 1:1 — una piastra ha al più un disegno).</summary>
    public Disegno? Disegno { get; set; }

    /// <summary>Modelli di macchina con cui questa piastra è compatibile.</summary>
    public ICollection<PiastraMacchinaCompatibile> MacchineCompatibili { get; set; } = [];

    /// <summary>Clienti che possiedono questa piastra (catalogo condiviso).</summary>
    public ICollection<ClientePiastra> ClientiAssociati { get; set; } = [];
}
