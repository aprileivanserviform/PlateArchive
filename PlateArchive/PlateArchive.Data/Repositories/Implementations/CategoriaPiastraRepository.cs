using Microsoft.EntityFrameworkCore;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;

namespace PlateArchive.Data.Repositories.Implementations;

public class CategoriaPiastraRepository(PlateArchiveDbContext db) : ICategoriaPiastraRepository
{
    public async Task<IEnumerable<CategoriaPiastra>> GetAllAsync() =>
        await db.CategoriePiastre.OrderBy(c => c.Ordine).ToListAsync();
}
