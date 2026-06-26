using Microsoft.EntityFrameworkCore;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;

namespace PlateArchive.Data.Repositories.Implementations;

public class FormatoMacchinaRepository(PlateArchiveDbContext db) : IFormatoMacchinaRepository
{
    public async Task<IEnumerable<FormatoMacchina>> GetAllAsync() =>
        await db.FormatiMacchine.OrderBy(f => f.NomeFormato).ToListAsync();

    public async Task<bool> HasMacchineAssociateAsync(int idFormato) =>
        await db.MacchineStandard.AnyAsync(m => m.IdFormato == idFormato)
        || await db.Piastre.AnyAsync(p => p.IdFormato == idFormato);

    public async Task EliminaLogicamenteAsync(int idFormato)
    {
        var entity = await db.FormatiMacchine.FindAsync(idFormato);
        if (entity is null) return;
        entity.IsEliminata = true;
        await db.SaveChangesAsync();
    }

    public async Task AddAsync(FormatoMacchina entity)
    {
        db.FormatiMacchine.Add(entity);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(FormatoMacchina entity)
    {
        db.FormatiMacchine.Update(entity);
        await db.SaveChangesAsync();
    }
}
