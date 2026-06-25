using PlateArchive.Core.Models;

namespace PlateArchive.Data.Repositories.Interfaces;

public interface ICategoriaPiastraRepository
{
    Task<IEnumerable<CategoriaPiastra>> GetAllAsync();
}
