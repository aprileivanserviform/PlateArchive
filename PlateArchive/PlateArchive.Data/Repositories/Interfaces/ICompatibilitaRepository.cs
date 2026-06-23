using PlateArchive.Core.Models;

namespace PlateArchive.Data.Repositories.Interfaces;

public interface ICompatibilitaRepository : IRepository<PiastraMacchinaCompatibile>
{
    Task<IEnumerable<PiastraMacchinaCompatibile>> GetByPiastraAsync(int idPiastra);
    Task<IEnumerable<PiastraMacchinaCompatibile>> GetByMacchinaAsync(int idMacchinaStandard);
    Task<bool> ExistsAsync(int idPiastra, int idMacchinaStandard);
    Task SetAttivaAsync(int idCompatibilita, bool attiva);
}
