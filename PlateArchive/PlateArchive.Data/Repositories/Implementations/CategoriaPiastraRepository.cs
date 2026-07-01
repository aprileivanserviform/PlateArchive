using Microsoft.EntityFrameworkCore;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;

namespace PlateArchive.Data.Repositories.Implementations;

/// <summary>
/// Repository CRUD per la tabella CategoriePiastre.
/// La cancellazione è fisica (hard delete), ma bloccata se esistono piastre associate.
/// </summary>
public class CategoriaPiastraRepository(PlateArchiveDbContext db) : ICategoriaPiastraRepository
{
    public async Task<IEnumerable<CategoriaPiastra>> GetAllAsync() =>
        await db.CategoriePiastre.OrderBy(c => c.Ordine).ToListAsync();

    public async Task<CategoriaPiastra?> GetByIdAsync(int id) =>
        await db.CategoriePiastre.FindAsync(id);

    public async Task AddAsync(CategoriaPiastra entity)
    {
        db.CategoriePiastre.Add(entity);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(CategoriaPiastra entity)
    {
        db.CategoriePiastre.Update(entity);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await db.CategoriePiastre.FindAsync(id);
        if (entity is null) return;
        db.CategoriePiastre.Remove(entity);
        await db.SaveChangesAsync();
    }

    public async Task<bool> HasPiastreAssociateAsync(int id) =>
        await db.Piastre.AnyAsync(p => p.IdCategoriaPiastra == id);
}
