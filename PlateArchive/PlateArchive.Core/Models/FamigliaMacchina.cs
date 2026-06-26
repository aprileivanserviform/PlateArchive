namespace PlateArchive.Core.Models;

/// <summary>
/// Formato macchina (es. 106, 145, 88): tabella madre condivisa tra piastre e macchine.
/// Solo le macchine con lo stesso formato della piastra possono essere associate.
/// </summary>
public class FormatoMacchina
{
    public int    IdFormato   { get; set; }
    public string NomeFormato { get; set; } = string.Empty;
    public bool   IsEliminata { get; set; } = false;
    public string? Note       { get; set; }

    public ICollection<MacchinaStandard> Macchine { get; set; } = [];
    public ICollection<Piastra>          Piastre  { get; set; } = [];
}
