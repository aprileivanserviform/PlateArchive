namespace PlateArchive.Core.Models;

/// <summary>
/// Formato macchina (es. 106, 145, 88): tabella lookup condivisa tra piastre e macchine.
/// Questo è il criterio di compatibilità primario:
/// una piastra è compatibile solo con macchine dello stesso formato.
/// <para>
/// NOTA: la classe si chiama <c>FormatoMacchina</c> ma il file fisico è ancora
/// <c>FamigliaMacchina.cs</c> per ragioni storiche (rinomina parziale).
/// Il nome della tabella nel database è <c>FormatiMacchine</c>.
/// </para>
/// Supporta soft-delete (<see cref="IsEliminata"/>) per mantenere l'integrità
/// referenziale con macchine e piastre esistenti.
/// </summary>
public class FormatoMacchina
{
    public int    IdFormato   { get; set; }
    public string NomeFormato { get; set; } = string.Empty;

    /// <summary>True = eliminato logicamente. Filtrato automaticamente da EF Core (HasQueryFilter).</summary>
    public bool   IsEliminata { get; set; } = false;
    public string? Note       { get; set; }

    // ─── Navigazioni ─────────────────────────────────────────────────────────

    /// <summary>Macchine standard di questo formato.</summary>
    public ICollection<MacchinaStandard> Macchine { get; set; } = [];

    /// <summary>Piastre destinate a questo formato di macchina.</summary>
    public ICollection<Piastra>          Piastre  { get; set; } = [];
}
