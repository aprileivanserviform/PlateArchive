using PlateArchive.Core.Enums;
using PlateArchive.Core.Models;

namespace PlateArchive.Data.Repositories.Interfaces;

public interface IDisegnoRepository : IRepository<Disegno>
{
    Task<IEnumerable<Disegno>> GetByStatoAsync(StatoDisegno stato);

    /// <summary>Disegno associato a una piastra (al più uno, per la relazione 1:1).</summary>
    Task<Disegno?> GetByPiastraAsync(int idPiastra);

    /// <summary>Cerca un disegno per nome file (confronto esatto, case-insensitive).</summary>
    Task<Disegno?> GetByNomeFileAsync(string nomeFile);
}
