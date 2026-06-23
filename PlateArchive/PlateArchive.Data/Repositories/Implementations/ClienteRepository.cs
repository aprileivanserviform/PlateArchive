using Microsoft.EntityFrameworkCore;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;

namespace PlateArchive.Data.Repositories.Implementations;

public class ClienteRepository(PlateArchiveDbContext db) : IClienteRepository
{
    public async Task<Cliente?> GetByIdAsync(int id) =>
        await db.Clienti.FindAsync(id);

    public async Task<IEnumerable<Cliente>> GetAllAsync() =>
        await db.Clienti.OrderBy(c => c.RagioneSociale).ToListAsync();

    public async Task<Cliente?> GetByCodiceGestionaleAsync(string codice) =>
        await db.Clienti.FirstOrDefaultAsync(c => c.CodiceClienteGestionale == codice);

    public async Task<IEnumerable<Cliente>> SearchAsync(string query)
    {
        var q = query.ToLower();
        return await db.Clienti
            .Where(c => c.RagioneSociale.ToLower().Contains(q)
                     || c.CodiceClienteGestionale.ToLower().Contains(q)
                     || (c.PartitaIVA != null && c.PartitaIVA.Contains(q)))
            .OrderBy(c => c.RagioneSociale)
            .ToListAsync();
    }

    public async Task AddAsync(Cliente entity)
    {
        db.Clienti.Add(entity);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Cliente entity)
    {
        db.Clienti.Update(entity);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity is not null)
        {
            db.Clienti.Remove(entity);
            await db.SaveChangesAsync();
        }
    }
}
