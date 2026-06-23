using PlateArchive.Core.Models;

namespace PlateArchive.Data.Repositories.Interfaces;

public interface IClienteRepository : IRepository<Cliente>
{
    Task<Cliente?> GetByCodiceGestionaleAsync(string codice);
    Task<IEnumerable<Cliente>> SearchAsync(string query);
}
