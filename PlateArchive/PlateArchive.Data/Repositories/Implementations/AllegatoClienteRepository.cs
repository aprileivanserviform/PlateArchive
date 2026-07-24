using Microsoft.EntityFrameworkCore;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;

namespace PlateArchive.Data.Repositories.Implementations;

public class AllegatoClienteRepository(PlateArchiveDbContext db) : IAllegatoClienteRepository
{
    public async Task<IReadOnlyList<AllegatoCliente>> GetByClienteAsync(int idCliente) =>
        await db.AllegatiClienti
            .Where(a => a.IdCliente == idCliente)
            .OrderByDescending(a => a.DataCaricamento)
            .ToListAsync();

    public async Task<AllegatoCliente> AddAsync(AllegatoCliente allegato)
    {
        allegato.DataCaricamento = DateTime.UtcNow;
        db.AllegatiClienti.Add(allegato);
        await db.SaveChangesAsync();
        return allegato;
    }

    public async Task DeleteAsync(int idAllegato)
    {
        var entity = await db.AllegatiClienti.FindAsync(idAllegato);
        if (entity is not null)
        {
            db.AllegatiClienti.Remove(entity);
            await db.SaveChangesAsync();
        }
    }
}
