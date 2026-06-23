using PlateArchive.Core.Models;

namespace PlateArchive.Data.Repositories.Interfaces;

public interface IMacchinaStandardRepository : IRepository<MacchinaStandard>
{
    Task<MacchinaStandard?> GetByCodiceMacchinaAsync(string codice);
    Task<IEnumerable<MacchinaStandard>> SearchAsync(string query);
    Task<IEnumerable<MacchinaStandard>> GetAttiveAsync();
}
