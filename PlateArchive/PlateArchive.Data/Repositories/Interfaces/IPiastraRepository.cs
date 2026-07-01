using PlateArchive.Core.Models;

namespace PlateArchive.Data.Repositories.Interfaces;

/// <summary>
/// Repository per le piastre. Estende il CRUD generico con ricerche specifiche
/// e il supporto al soft-delete (le piastre non vengono mai cancellate fisicamente).
/// </summary>
public interface IPiastraRepository : IRepository<Piastra>
{
    /// <summary>Ricerca per codice piastra esatto (case-insensitive).</summary>
    Task<Piastra?> GetByCodicePiastraAsync(string codice);

    /// <summary>Ricerca full-text su codice, descrizione e codice articolo gestionale.</summary>
    Task<IEnumerable<Piastra>> SearchAsync(string query);

    /// <summary>Restituisce le ultime <paramref name="count"/> piastre inserite — usato dalla Dashboard.</summary>
    Task<IEnumerable<Piastra>> GetUltimeInseriteAsync(int count = 10);

    /// <summary>True se la piastra è associata ad almeno un cliente — usato per bloccare la cancellazione.</summary>
    Task<bool> HasClientiAssociatiAsync(int idPiastra);

    /// <summary>Imposta <c>IsEliminata = true</c> senza eliminare il record fisicamente.</summary>
    Task EliminaLogicamenteAsync(int idPiastra);
}
