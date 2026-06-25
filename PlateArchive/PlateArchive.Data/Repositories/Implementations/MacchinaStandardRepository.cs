using Microsoft.EntityFrameworkCore;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;

namespace PlateArchive.Data.Repositories.Implementations;

public class MacchinaStandardRepository(PlateArchiveDbContext db) : IMacchinaStandardRepository
{
    public async Task<MacchinaStandard?> GetByIdAsync(int id) =>
        await db.MacchineStandard
            .Include(m => m.Famiglia)
            .Include(m => m.Produttore)
            .FirstOrDefaultAsync(m => m.IdMacchinaStandard == id);

    public async Task<IEnumerable<MacchinaStandard>> GetAllAsync() =>
        await db.MacchineStandard
            .Include(m => m.Famiglia)
            .Include(m => m.Produttore)
            .OrderBy(m => m.NomeMacchina)
            .ToListAsync();

    public async Task<MacchinaStandard?> GetByCodiceMacchinaAsync(string codice) =>
        await db.MacchineStandard
            .Include(m => m.Famiglia)
            .Include(m => m.Produttore)
            .FirstOrDefaultAsync(m => m.CodiceMacchina == codice);

    public async Task<IEnumerable<MacchinaStandard>> SearchAsync(string query)
    {
        var q = query.ToLower();
        return await db.MacchineStandard
            .Include(m => m.Famiglia)
            .Include(m => m.Produttore)
            .Where(m => m.NomeMacchina.ToLower().Contains(q)
                     || m.CodiceMacchina.ToLower().Contains(q)
                     || (m.Famiglia != null && m.Famiglia.NomeFamiglia.ToLower().Contains(q)))
            .OrderBy(m => m.NomeMacchina)
            .ToListAsync();
    }

    public async Task<IEnumerable<MacchinaStandard>> GetAttiveAsync() =>
        await db.MacchineStandard
            .Include(m => m.Famiglia)
            .Include(m => m.Produttore)
            .Where(m => m.Attiva)
            .OrderBy(m => m.NomeMacchina)
            .ToListAsync();

    public async Task AddAsync(MacchinaStandard entity)
    {
        db.MacchineStandard.Add(entity);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(MacchinaStandard entity)
    {
        db.MacchineStandard.Update(entity);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await db.MacchineStandard.FindAsync(id);
        if (entity is not null)
        {
            db.MacchineStandard.Remove(entity);
            await db.SaveChangesAsync();
        }
    }
}
