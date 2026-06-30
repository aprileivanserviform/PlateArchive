namespace PlateArchive.Core.Enums;

public enum TipoPiastra
{
    /// <summary>Piastra del catalogo condiviso — salvata in Piastre\Standard\.</summary>
    Standard,

    /// <summary>Variante modificata per un singolo cliente — salvata in Piastre\Clienti\{CodiceCliente}\.</summary>
    SpecialeCliente
}
