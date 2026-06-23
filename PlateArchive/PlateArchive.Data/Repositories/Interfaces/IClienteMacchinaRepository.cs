using PlateArchive.Core.Models;

namespace PlateArchive.Data.Repositories.Interfaces;

public interface IClienteMacchinaRepository : IRepository<ClienteMacchina>
{
    Task<IEnumerable<ClienteMacchina>> GetByClienteAsync(int idCliente);
    Task<IEnumerable<ClienteMacchina>> GetByMacchinaAsync(int idMacchinaStandard);
}
