using PlateArchive.Core.Models;

namespace PlateArchive.Data.Repositories.Interfaces;

public interface ICategoriaPiastraRepository
{
    Task<IEnumerable<CategoriaPiastra>> GetAllAsync();
    Task<CategoriaPiastra?> GetByIdAsync(int id);
    Task AddAsync(CategoriaPiastra entity);
    Task UpdateAsync(CategoriaPiastra entity);
    Task DeleteAsync(int id);

    /// <summary>True se la categoria è usata da almeno una piastra — blocca la cancellazione.</summary>
    Task<bool> HasPiastreAssociateAsync(int id);
}
