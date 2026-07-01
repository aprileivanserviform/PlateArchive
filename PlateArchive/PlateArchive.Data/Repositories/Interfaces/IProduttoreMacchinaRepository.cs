using PlateArchive.Core.Models;

namespace PlateArchive.Data.Repositories.Interfaces;

public interface IProduttoreMacchinaRepository
{
    Task<IEnumerable<ProduttoreMacchina>> GetAllAsync();
    Task<bool>  HasMacchineAssociateAsync(int idProduttore);
    Task        EliminaLogicamenteAsync(int idProduttore);
    Task        AddAsync(ProduttoreMacchina entity);
    Task        UpdateAsync(ProduttoreMacchina entity);
}
