using PlateArchive.Core.Models;

namespace PlateArchive.Data.Repositories.Interfaces;

/// <summary>
/// Repository per i formati macchina (<see cref="FormatoMacchina"/>).
/// Non estende IRepository generico perché la tabella usa soft-delete
/// e non espone l'eliminazione fisica.
/// NOTA: il file si chiama IFamigliaMacchinaRepository.cs per ragioni storiche
/// (rinomina parziale da "Famiglie" a "Formati").
/// </summary>
public interface IFormatoMacchinaRepository
{
    /// <summary>Restituisce tutti i formati non eliminati, ordinati per nome.</summary>
    Task<IEnumerable<FormatoMacchina>> GetAllAsync();

    /// <summary>
    /// True se il formato è usato da almeno una macchina o una piastra.
    /// Usato per bloccare l'eliminazione logica e mostrare un messaggio all'utente.
    /// </summary>
    Task<bool> HasMacchineAssociateAsync(int idFormato);

    /// <summary>Imposta <c>IsEliminata = true</c> senza eliminare il record fisicamente.</summary>
    Task EliminaLogicamenteAsync(int idFormato);
    Task AddAsync(FormatoMacchina entity);
    Task UpdateAsync(FormatoMacchina entity);
}
