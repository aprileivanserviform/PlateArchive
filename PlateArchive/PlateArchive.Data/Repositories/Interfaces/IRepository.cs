namespace PlateArchive.Data.Repositories.Interfaces;

/// <summary>
/// Interfaccia generica CRUD per tutti i repository del progetto.
/// Ogni entità ha un repository concreto che implementa questa interfaccia
/// e può aggiungere metodi di ricerca specifici (es. GetByCodicePiastraAsync).
/// Tutti i metodi sono asincroni per non bloccare il thread UI (WPF).
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);

    /// <summary>Eliminazione fisica del record dal database.</summary>
    Task DeleteAsync(int id);
}
