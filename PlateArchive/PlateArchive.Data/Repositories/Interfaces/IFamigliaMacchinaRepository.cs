using PlateArchive.Core.Models;

namespace PlateArchive.Data.Repositories.Interfaces;

public interface IFormatoMacchinaRepository
{
    Task<IEnumerable<FormatoMacchina>> GetAllAsync();
    Task<bool>  HasMacchineAssociateAsync(int idFormato);
    Task        EliminaLogicamenteAsync(int idFormato);
    Task        AddAsync(FormatoMacchina entity);
    Task        UpdateAsync(FormatoMacchina entity);
}
