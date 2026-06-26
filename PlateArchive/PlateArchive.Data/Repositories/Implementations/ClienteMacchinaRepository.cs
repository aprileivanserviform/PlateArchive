using Microsoft.EntityFrameworkCore;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;

namespace PlateArchive.Data.Repositories.Implementations;

/// <summary>
/// Repository per la tabella ClientiMacchine (associazione cliente ↔ unità fisica di macchina).
/// Include sempre la navigation MacchinaStandard perché la UI mostra sempre il nome modello.
/// </summary>
public class ClienteMacchinaRepository(PlateArchiveDbContext db) : IClienteMacchinaRepository
{
    public async Task<ClienteMacchina?> GetByIdAsync(int id) =>
        await db.ClientiMacchine.Include(cm => cm.MacchinaStandard).FirstOrDefaultAsync(cm => cm.IdClienteMacchina == id);

    public async Task<IEnumerable<ClienteMacchina>> GetAllAsync() =>
        await db.ClientiMacchine.Include(cm => cm.MacchinaStandard).ToListAsync();

    public async Task<IEnumerable<ClienteMacchina>> GetByClienteAsync(int idCliente) =>
        await db.ClientiMacchine
            .Include(cm => cm.MacchinaStandard)
            .Where(cm => cm.IdCliente == idCliente)
            .OrderBy(cm => cm.MacchinaStandard.NomeMacchina)
            .ToListAsync();

    public async Task<IEnumerable<ClienteMacchina>> GetByMacchinaAsync(int idMacchinaStandard) =>
        await db.ClientiMacchine
            .Include(cm => cm.Cliente)
            .Where(cm => cm.IdMacchinaStandard == idMacchinaStandard)
            .ToListAsync();

    public async Task AddAsync(ClienteMacchina entity)
    {
        // Timestamp di quando il cliente ha acquistato/registrato questa macchina.
        entity.DataAssociazione = DateTime.UtcNow;
        db.ClientiMacchine.Add(entity);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(ClienteMacchina entity)
    {
        db.ClientiMacchine.Update(entity);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity is not null)
        {
            db.ClientiMacchine.Remove(entity);
            await db.SaveChangesAsync();
        }
    }
}
