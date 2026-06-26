using Microsoft.EntityFrameworkCore;
using PlateArchive.Core.Models;
using PlateArchive.Data.Repositories.Interfaces;

namespace PlateArchive.Data.Repositories.Implementations;

/// <summary>
/// Repository sola lettura per la tabella CategoriePiastre.
/// Le categorie sono dati di configurazione: non esiste CRUD nella UI v1.
/// Vengono ordinate per campo Ordine (valore numerico che determina la sequenza nel ComboBox).
/// </summary>
public class CategoriaPiastraRepository(PlateArchiveDbContext db) : ICategoriaPiastraRepository
{
    public async Task<IEnumerable<CategoriaPiastra>> GetAllAsync() =>
        await db.CategoriePiastre.OrderBy(c => c.Ordine).ToListAsync();
}
