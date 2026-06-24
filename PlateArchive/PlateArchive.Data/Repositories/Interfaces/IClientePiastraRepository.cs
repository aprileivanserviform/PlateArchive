using PlateArchive.Core.Models;

namespace PlateArchive.Data.Repositories.Interfaces;

public interface IClientePiastraRepository : IRepository<ClientePiastra>
{
    Task<IEnumerable<ClientePiastra>> GetByClienteAsync(int idCliente);
    Task<IEnumerable<ClientePiastra>> GetByPiastraAsync(int idPiastra);
    Task<bool> ExistsAsync(int idCliente, int idPiastra);
}
