using PlateArchive.Core.Models;

namespace PlateArchive.Data.Repositories.Interfaces;

public interface INotaTecnicaClienteRepository
{
    Task<IReadOnlyList<NotaTecnicaCliente>> GetByClienteAsync(int idCliente);
    Task AddAsync(NotaTecnicaCliente nota);
    Task UpdateAsync(NotaTecnicaCliente nota);
    Task DeleteAsync(int idNota);
}
