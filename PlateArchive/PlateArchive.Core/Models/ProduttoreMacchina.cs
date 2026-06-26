namespace PlateArchive.Core.Models;

/// <summary>
/// Tabella lookup dei produttori di macchine (es. Flexo Label, Mark Andy, Nilpeter).
/// Usata per filtrare e raggruppare i modelli in MacchineView.
/// Supporta soft-delete (<see cref="IsEliminata"/>) per mantenere l'integrità
/// referenziale con le macchine già censite.
/// </summary>
public class ProduttoreMacchina
{
    public int    IdProduttore   { get; set; }
    public string NomeProduttore { get; set; } = string.Empty;

    /// <summary>True = eliminato logicamente. Filtrato automaticamente da EF Core (HasQueryFilter).</summary>
    public bool   IsEliminata   { get; set; } = false;
    public string? Note         { get; set; }

    // ─── Navigazione ─────────────────────────────────────────────────────────
    public ICollection<MacchinaStandard> Macchine { get; set; } = [];
}
