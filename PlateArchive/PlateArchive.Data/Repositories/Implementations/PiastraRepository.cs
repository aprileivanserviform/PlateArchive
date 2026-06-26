using Microsoft.EntityFrameworkCore;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;

namespace PlateArchive.Data.Repositories.Implementations;

public class PiastraRepository(PlateArchiveDbContext db) : IPiastraRepository
{
    // Le query usano il HasQueryFilter (IsEliminata == false) in automatico.

    public async Task<Piastra?> GetByIdAsync(int id) =>
        await db.Piastre
            .Include(p => p.Categoria)
            .Include(p => p.Formato)
            .Include(p => p.Disegno)
            .FirstOrDefaultAsync(p => p.IdPiastra == id);

    public async Task<IEnumerable<Piastra>> GetAllAsync() =>
        await db.Piastre
            .Include(p => p.Categoria)
            .Include(p => p.Formato)
            .Include(p => p.Disegno)
            .OrderBy(p => p.CodicePiastra)
            .ToListAsync();

    public async Task<Piastra?> GetByCodicePiastraAsync(string codice) =>
        await db.Piastre
            .Include(p => p.Categoria)
            .Include(p => p.Formato)
            .Include(p => p.Disegno)
            .FirstOrDefaultAsync(p => p.CodicePiastra == codice);

    public async Task<IEnumerable<Piastra>> SearchAsync(string query)
    {
        var q = query.ToLower();
        return await db.Piastre
            .Include(p => p.Categoria)
            .Include(p => p.Formato)
            .Include(p => p.Disegno)
            .Where(p => p.CodicePiastra.ToLower().Contains(q)
                     || (p.CodiceArticoloGestionale != null && p.CodiceArticoloGestionale.ToLower().Contains(q))
                     || (p.Descrizione != null && p.Descrizione.ToLower().Contains(q)))
            .OrderBy(p => p.CodicePiastra)
            .ToListAsync();
    }

    public async Task<IEnumerable<Piastra>> GetUltimeInseriteAsync(int count = 10) =>
        await db.Piastre
            .OrderByDescending(p => p.DataCreazione)
            .Take(count)
            .ToListAsync();

    public async Task<bool> HasClientiAssociatiAsync(int idPiastra) =>
        await db.ClientiPiastre.AnyAsync(cp => cp.IdPiastra == idPiastra);

    public async Task EliminaLogicamenteAsync(int idPiastra)
    {
        var piastra = await db.Piastre.FirstOrDefaultAsync(p => p.IdPiastra == idPiastra);
        if (piastra is null) return;
        piastra.IsEliminata        = true;
        piastra.DataUltimaModifica = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task AddAsync(Piastra entity)
    {
        entity.DataCreazione      = DateTime.UtcNow;
        entity.DataUltimaModifica = DateTime.UtcNow;
        db.Piastre.Add(entity);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Piastra entity)
    {
        entity.DataUltimaModifica = DateTime.UtcNow;
        db.Piastre.Update(entity);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await db.Piastre.IgnoreQueryFilters()
                                     .FirstOrDefaultAsync(p => p.IdPiastra == id);
        if (entity is not null)
        {
            db.Piastre.Remove(entity);
            await db.SaveChangesAsync();
        }
    }
}
