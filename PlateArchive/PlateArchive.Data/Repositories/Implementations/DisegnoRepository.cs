using Microsoft.EntityFrameworkCore;
using PlateArchive.Core.Enums;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;

namespace PlateArchive.Data.Repositories.Implementations;

/// <summary>
/// Repository per la tabella Disegni (metadati — il file fisico è gestito da FileArchivioService).
/// Relazione 1:1 con Piastre tramite FK diretta Disegno.IdPiastra.
/// </summary>
public class DisegnoRepository(PlateArchiveDbContext db) : IDisegnoRepository
{
    public async Task<Disegno?> GetByIdAsync(int id) =>
        await db.Disegni
            .Include(d => d.Piastra)
            .FirstOrDefaultAsync(d => d.IdDisegno == id);

    public async Task<IEnumerable<Disegno>> GetAllAsync() =>
        await db.Disegni
            .Include(d => d.Piastra).ThenInclude(p => p!.Categoria)
            .Include(d => d.Piastra).ThenInclude(p => p!.Formato)
            .OrderBy(d => d.CodiceDisegno)
            .ToListAsync();

    public async Task<IEnumerable<Disegno>> GetByStatoAsync(StatoDisegno stato) =>
        await db.Disegni
            .Include(d => d.Piastra).ThenInclude(p => p!.Categoria)
            .Include(d => d.Piastra).ThenInclude(p => p!.Formato)
            .Where(d => d.Stato == stato)
            .ToListAsync();

    public async Task<Disegno?> GetByPiastraAsync(int idPiastra) =>
        await db.Disegni
            .FirstOrDefaultAsync(d => d.IdPiastra == idPiastra);

    public async Task<Disegno?> GetByNomeFileAsync(string nomeFile) =>
        await db.Disegni
            .Include(d => d.Piastra)
            .FirstOrDefaultAsync(d => d.NomeFile != null
                && d.NomeFile.ToLower() == nomeFile.ToLower());

    public async Task AddAsync(Disegno entity)
    {
        db.Disegni.Add(entity);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Disegno entity)
    {
        db.Disegni.Update(entity);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity is not null)
        {
            db.Disegni.Remove(entity);
            await db.SaveChangesAsync();
        }
    }
}
