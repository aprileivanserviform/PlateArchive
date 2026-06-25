using Microsoft.EntityFrameworkCore;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;

namespace PlateArchive.Data.Repositories.Implementations;

public class ProduttoreMacchinaRepository(PlateArchiveDbContext db) : IProduttoreMacchinaRepository
{
    public async Task<IEnumerable<ProduttoreMacchina>> GetAllAsync() =>
        await db.ProduttoriMacchine.OrderBy(p => p.NomeProduttore).ToListAsync();

    public async Task<bool> HasMacchineAssociateAsync(int idProduttore) =>
        await db.MacchineStandard.AnyAsync(m => m.IdProduttore == idProduttore);

    public async Task EliminaLogicamenteAsync(int idProduttore)
    {
        var entity = await db.ProduttoriMacchine.FindAsync(idProduttore);
        if (entity is null) return;
        entity.IsEliminata = true;
        await db.SaveChangesAsync();
    }

    public async Task AddAsync(ProduttoreMacchina entity)
    {
        db.ProduttoriMacchine.Add(entity);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(ProduttoreMacchina entity)
    {
        db.ProduttoriMacchine.Update(entity);
        await db.SaveChangesAsync();
    }
}
