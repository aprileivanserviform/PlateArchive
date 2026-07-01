using PlateArchive.Core.Enums;

namespace PlateArchive.Core.Models;

/// <summary>
/// Disegno tecnico presente nell'archivio.
/// Il file fisico (DWG/DXF/PDF) NON è salvato nel database:
/// si memorizza solo il percorso sul server condiviso (<see cref="PercorsoFile"/>)
/// o l'identificatore Autodesk Vault (<see cref="VaultId"/>).
/// <para>
/// Relazione 1:1 con <see cref="Piastra"/>: ogni disegno appartiene a esattamente una piastra
/// e ogni piastra ha al più un disegno (vincolo UNIQUE su <see cref="IdPiastra"/>).
/// </para>
/// </summary>
public class Disegno
{
    public int IdDisegno { get; set; }

    /// <summary>FK verso <see cref="Piastra"/> — vincolo UNIQUE garantisce la relazione 1:1.</summary>
    public int? IdPiastra { get; set; }

    /// <summary>Codice del disegno nel sistema CAD (es. numero tavola).</summary>
    public string? CodiceDisegno { get; set; }
    public string? NomeFile      { get; set; }

    /// <summary>Percorso UNC sul server condiviso (es. \\server\disegni\PLT-000001.dwg).</summary>
    public string? PercorsoFile  { get; set; }

    /// <summary>Identificatore in Autodesk Vault (per l'integrazione futura — §23.4).</summary>
    public string? VaultId       { get; set; }
    public string? Revisione     { get; set; }

    /// <summary>Formato del file: DWG, DXF, PDF, STP, ecc.</summary>
    public string? Formato                  { get; set; }
    public StatoDisegno Stato               { get; set; }
    public DateTime? DataUltimaModificaFile { get; set; }
    public string? Note                     { get; set; }

    // ─── Navigazione ─────────────────────────────────────────────────────────
    public Piastra? Piastra { get; set; }
}
