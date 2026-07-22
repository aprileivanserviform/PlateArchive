namespace PlateArchive.Core.Models;

/// <summary>
/// Tabella lookup dei valori di durezza per le piastre (es. "60 ShA", "65 ShD").
/// Gestita nella sezione Impostazioni → Durezze piastre.
/// Soft-delete via <see cref="IsEliminata"/> per preservare l'integrità referenziale.
/// </summary>
public class DurezzaStandard
{
    public int    IdDurezza  { get; set; }

    /// <summary>Valore visualizzato nel dropdown (es. "60 ShA", "38 ShD", "90 ShA").</summary>
    public string  Valore    { get; set; } = string.Empty;
    public string? Note      { get; set; }
    public bool    IsEliminata { get; set; } = false;

    public ICollection<Piastra> Piastre { get; set; } = [];
}
