using PlateArchive.Core.Models;

namespace PlateArchive.Data.Repositories.Interfaces;

public interface IAllegatoClienteRepository
{
    Task<IReadOnlyList<AllegatoCliente>> GetByClienteAsync(int idCliente);
    Task<AllegatoCliente> AddAsync(AllegatoCliente allegato);
    Task DeleteAsync(int idAllegato);
}
