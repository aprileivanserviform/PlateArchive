namespace PlateArchive.Core.Models;

public class MacchinaStandard
{
    public int    IdMacchinaStandard { get; set; }
    public string CodiceMacchina     { get; set; } = string.Empty;
    public string NomeMacchina       { get; set; } = string.Empty;
    public int?   IdFormato          { get; set; }
    public int?   IdProduttore       { get; set; }
    public decimal? LarghezzaMm      { get; set; }
    public decimal? AltezzaMm        { get; set; }
    public string?  Versione         { get; set; }
    public bool     Attiva           { get; set; } = true;
    public string?  Note             { get; set; }

    public FormatoMacchina?    Formato    { get; set; }
    public ProduttoreMacchina? Produttore { get; set; }
    public ICollection<PiastraMacchinaCompatibile> PiastreCompatibili { get; set; } = [];
    public ICollection<ClienteMacchina>            ClientiAssociati   { get; set; } = [];
}
