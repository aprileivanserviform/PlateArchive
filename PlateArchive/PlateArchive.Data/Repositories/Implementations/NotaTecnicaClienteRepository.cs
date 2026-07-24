using Microsoft.EntityFrameworkCore;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;

namespace PlateArchive.Data.Repositories.Implementations;

public class NotaTecnicaClienteRepository(PlateArchiveDbContext db) : INotaTecnicaClienteRepository
{
    public async Task<IReadOnlyList<NotaTecnicaCliente>> GetByClienteAsync(int idCliente) =>
        await db.NoteTecnicheClienti
            .Where(n => n.IdCliente == idCliente)
            .OrderByDescending(n => n.DataModifica)
            .ToListAsync();

    public async Task AddAsync(NotaTecnicaCliente nota)
    {
        nota.DataCreazione = DateTime.UtcNow;
        nota.DataModifica  = DateTime.UtcNow;
        db.NoteTecnicheClienti.Add(nota);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(NotaTecnicaCliente nota)
    {
        nota.DataModifica = DateTime.UtcNow;
        db.NoteTecnicheClienti.Update(nota);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int idNota)
    {
        var entity = await db.NoteTecnicheClienti.FindAsync(idNota);
        if (entity is not null)
        {
            db.NoteTecnicheClienti.Remove(entity);
            await db.SaveChangesAsync();
        }
    }
}
