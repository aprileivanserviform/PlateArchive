using Microsoft.EntityFrameworkCore;
using PlateArchive.Core.Enums;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;

namespace PlateArchive.Data.Repositories.Implementations;

/// <summary>
/// Repository per la tabella ClientiPiastre (associazione commerciale cliente ↔ piastra).
/// Le query includono sempre Piastra.Disegno (ThenInclude) perché la UI mostra l'icona disegno
/// e permette di aprire il file direttamente dalla lista piastre del cliente.
/// </summary>
public class ClientePiastraRepository(PlateArchiveDbContext db) : IClientePiastraRepository
{
    public async Task<ClientePiastra?> GetByIdAsync(int id) =>
        await db.ClientiPiastre
            .Include(cp => cp.Piastra).ThenInclude(p => p.Disegno)
            .Include(cp => cp.ClienteMacchina).ThenInclude(cm => cm!.MacchinaStandard)
            .FirstOrDefaultAsync(cp => cp.IdClientePiastra == id);

    public async Task<IEnumerable<ClientePiastra>> GetAllAsync() =>
        await db.ClientiPiastre
            .Include(cp => cp.Piastra)
            .Include(cp => cp.ClienteMacchina)
            .ToListAsync();

    public async Task<IEnumerable<ClientePiastra>> GetByClienteAsync(int idCliente) =>
        await db.ClientiPiastre
            .Include(cp => cp.Piastra).ThenInclude(p => p.Disegno)
            .Include(cp => cp.ClienteMacchina).ThenInclude(cm => cm!.MacchinaStandard)
            .Where(cp => cp.IdCliente == idCliente)
            .OrderBy(cp => cp.Piastra.CodicePiastra)
            .ToListAsync();

    public async Task<IEnumerable<ClientePiastra>> GetByPiastraAsync(int idPiastra) =>
        await db.ClientiPiastre
            .Include(cp => cp.Cliente)
            .Include(cp => cp.ClienteMacchina).ThenInclude(cm => cm!.MacchinaStandard)
            .Where(cp => cp.IdPiastra == idPiastra)
            .OrderBy(cp => cp.Cliente.RagioneSociale)
            .ToListAsync();

    public async Task<bool> ExistsAsync(int idCliente, int idPiastra) =>
        await db.ClientiPiastre.AnyAsync(cp => cp.IdCliente == idCliente && cp.IdPiastra == idPiastra);

    public async Task AddAsync(ClientePiastra entity)
    {
        entity.DataAssociazione = DateTime.UtcNow;
        db.ClientiPiastre.Add(entity);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(ClientePiastra entity)
    {
        db.ClientiPiastre.Update(entity);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity is not null)
        {
            db.ClientiPiastre.Remove(entity);
            await db.SaveChangesAsync();
        }
    }

    public async Task SetStatoAsync(int idClientePiastra, StatoClientePiastra stato)
    {
        var entity = await db.ClientiPiastre.FindAsync(idClientePiastra);
        if (entity is null) return;
        entity.Stato = stato;
        await db.SaveChangesAsync();
    }
}
