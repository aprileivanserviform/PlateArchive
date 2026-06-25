using Microsoft.EntityFrameworkCore;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;

namespace PlateArchive.Data.Repositories.Implementations;

public class FamigliaMacchinaRepository(PlateArchiveDbContext db) : IFamigliaMacchinaRepository
{
    public async Task<IEnumerable<FamigliaMacchina>> GetAllAsync() =>
        await db.FamiglieMacchine.OrderBy(f => f.NomeFamiglia).ToListAsync();

    public async Task<bool> HasMacchineAssociateAsync(int idFamiglia) =>
        await db.MacchineStandard.AnyAsync(m => m.IdFamiglia == idFamiglia);

    public async Task EliminaLogicamenteAsync(int idFamiglia)
    {
        var entity = await db.FamiglieMacchine.FindAsync(idFamiglia);
        if (entity is null) return;
        entity.IsEliminata = true;
        await db.SaveChangesAsync();
    }

    public async Task AddAsync(FamigliaMacchina entity)
    {
        db.FamiglieMacchine.Add(entity);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(FamigliaMacchina entity)
    {
        db.FamiglieMacchine.Update(entity);
        await db.SaveChangesAsync();
    }
}
