using PlateArchive.Core.Models;

namespace PlateArchive.Data.Repositories.Interfaces;

public interface IPiastraRepository : IRepository<Piastra>
{
    Task<Piastra?> GetByCodicePiastraAsync(string codice);
    Task<IEnumerable<Piastra>> SearchAsync(string query);
    Task<IEnumerable<Piastra>> GetUltimeInseriteAsync(int count = 10);
}
