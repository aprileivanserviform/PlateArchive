using Microsoft.EntityFrameworkCore;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;

namespace PlateArchive.Data.Repositories.Implementations;

public class CompatibilitaRepository(PlateArchiveDbContext db) : ICompatibilitaRepository
{
    public async Task<PiastraMacchinaCompatibile?> GetByIdAsync(int id) =>
        await db.PiastreMacchineCompatibili.FindAsync(id);

    public async Task<IEnumerable<PiastraMacchinaCompatibile>> GetAllAsync() =>
        await db.PiastreMacchineCompatibili
            .Include(x => x.Piastra)
            .Include(x => x.MacchinaStandard)
            .ToListAsync();

    public async Task<IEnumerable<PiastraMacchinaCompatibile>> GetByPiastraAsync(int idPiastra) =>
        await db.PiastreMacchineCompatibili
            .Include(x => x.MacchinaStandard)
            .Where(x => x.IdPiastra == idPiastra && x.Attiva)
            .ToListAsync();

    public async Task<IEnumerable<PiastraMacchinaCompatibile>> GetByMacchinaAsync(int idMacchinaStandard) =>
        await db.PiastreMacchineCompatibili
            .Include(x => x.Piastra)
            .Where(x => x.IdMacchinaStandard == idMacchinaStandard && x.Attiva)
            .ToListAsync();

    public async Task<bool> ExistsAsync(int idPiastra, int idMacchinaStandard) =>
        await db.PiastreMacchineCompatibili
            .AnyAsync(x => x.IdPiastra == idPiastra && x.IdMacchinaStandard == idMacchinaStandard);

    public async Task SetAttivaAsync(int idCompatibilita, bool attiva)
    {
        var entity = await GetByIdAsync(idCompatibilita);
        if (entity is not null)
        {
            entity.Attiva = attiva;
            await db.SaveChangesAsync();
        }
    }

    public async Task AddAsync(PiastraMacchinaCompatibile entity)
    {
        db.PiastreMacchineCompatibili.Add(entity);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(PiastraMacchinaCompatibile entity)
    {
        db.PiastreMacchineCompatibili.Update(entity);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity is not null)
        {
            db.PiastreMacchineCompatibili.Remove(entity);
            await db.SaveChangesAsync();
        }
    }
}
