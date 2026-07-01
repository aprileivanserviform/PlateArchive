namespace PlateArchive.Core.Models;

/// <summary>
/// Tabella di lookup: categoria/tipo di piastra (es. Flessografica, Offset, Serigrafica).
/// Usata per classificare le piastre e per filtrare in PiastreView.
/// I valori sono predefiniti e gestiti solo dall'amministratore (nessun CRUD nell'UI v1).
/// </summary>
public class CategoriaPiastra
{
    public int    IdCategoriaPiastra { get; set; }

    /// <summary>Codice breve usato internamente (es. "FLES", "OFF").</summary>
    public string Codice             { get; set; } = string.Empty;
    public string Descrizione        { get; set; } = string.Empty;

    /// <summary>Ordine di visualizzazione nella UI (ComboBox, filtri).</summary>
    public int    Ordine             { get; set; }
}
