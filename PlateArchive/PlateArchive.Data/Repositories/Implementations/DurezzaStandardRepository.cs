using Microsoft.EntityFrameworkCore;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;

namespace PlateArchive.Data.Repositories.Implementations;

public class DurezzaStandardRepository(PlateArchiveDbContext db) : IDurezzaStandardRepository
{
    public async Task<IEnumerable<DurezzaStandard>> GetAllAsync() =>
        await db.DurezzePiastre.OrderBy(d => d.Valore).ToListAsync();

    public async Task<bool> HasPiastreAssociateAsync(int idDurezza) =>
        await db.Piastre.AnyAsync(p => p.IdDurezza == idDurezza);

    public async Task EliminaLogicamenteAsync(int idDurezza)
    {
        var entity = await db.DurezzePiastre.FindAsync(idDurezza);
        if (entity is null) return;
        entity.IsEliminata = true;
        await db.SaveChangesAsync();
    }

    public async Task AddAsync(DurezzaStandard entity)
    {
        db.DurezzePiastre.Add(entity);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(DurezzaStandard entity)
    {
        db.DurezzePiastre.Update(entity);
        await db.SaveChangesAsync();
    }
}
