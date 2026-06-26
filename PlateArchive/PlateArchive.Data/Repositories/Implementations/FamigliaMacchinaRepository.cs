using Microsoft.EntityFrameworkCore;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;

namespace PlateArchive.Data.Repositories.Implementations;

/// <summary>
/// Repository per la tabella FormatiMacchine (lookup con soft-delete).
/// HasMacchineAssociateAsync controlla PRIMA di eliminare logicamente:
/// se un formato è ancora usato da macchine o piastre, l'eliminazione viene bloccata dalla UI.
/// Il HasQueryFilter esclude automaticamente i formati con IsEliminata = true.
/// </summary>
public class FormatoMacchinaRepository(PlateArchiveDbContext db) : IFormatoMacchinaRepository
{
    public async Task<IEnumerable<FormatoMacchina>> GetAllAsync() =>
        await db.FormatiMacchine.OrderBy(f => f.NomeFormato).ToListAsync();

    // Controlla sia MacchineStandard che Piastre — il formato è condiviso tra entrambe.
    public async Task<bool> HasMacchineAssociateAsync(int idFormato) =>
        await db.MacchineStandard.AnyAsync(m => m.IdFormato == idFormato)
        || await db.Piastre.AnyAsync(p => p.IdFormato == idFormato);

    public async Task EliminaLogicamenteAsync(int idFormato)
    {
        var entity = await db.FormatiMacchine.FindAsync(idFormato);
        if (entity is null) return;
        entity.IsEliminata = true;
        await db.SaveChangesAsync();
    }

    public async Task AddAsync(FormatoMacchina entity)
    {
        db.FormatiMacchine.Add(entity);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(FormatoMacchina entity)
    {
        db.FormatiMacchine.Update(entity);
        await db.SaveChangesAsync();
    }
}
