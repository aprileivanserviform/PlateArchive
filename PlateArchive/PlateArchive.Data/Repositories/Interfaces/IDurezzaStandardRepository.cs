using PlateArchive.Core.Models;

namespace PlateArchive.Data.Repositories.Interfaces;

public interface IDurezzaStandardRepository
{
    Task<IEnumerable<DurezzaStandard>> GetAllAsync();
    Task<bool>  HasPiastreAssociateAsync(int idDurezza);
    Task        EliminaLogicamenteAsync(int idDurezza);
    Task        AddAsync(DurezzaStandard entity);
    Task        UpdateAsync(DurezzaStandard entity);
}
