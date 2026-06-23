using PlateArchive.Core.Enums;
using PlateArchive.Core.Models;

namespace PlateArchive.Data.Repositories.Interfaces;

public interface IDisegnoRepository : IRepository<Disegno>
{
    Task<Disegno?> GetByIdPiastraAsync(int idPiastra);
    Task<IEnumerable<Disegno>> GetByStatoAsync(StatoDisegno stato);
}
