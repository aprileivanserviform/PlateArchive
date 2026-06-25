using PlateArchive.Core.Models;

namespace PlateArchive.Data.Repositories.Interfaces;

public interface IFamigliaMacchinaRepository
{
    Task<IEnumerable<FamigliaMacchina>> GetAllAsync();
    Task<bool>  HasMacchineAssociateAsync(int idFamiglia);
    Task        EliminaLogicamenteAsync(int idFamiglia);
    Task        AddAsync(FamigliaMacchina entity);
    Task        UpdateAsync(FamigliaMacchina entity);
}
