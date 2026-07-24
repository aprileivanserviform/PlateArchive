using PlateArchive.Core.Models;

namespace PlateArchive.Data.Repositories.Interfaces;

/// <summary>
/// Repository per la tabella di giunzione <c>PiastreMacchineCompatibili</c>.
/// Gestisce le relazioni N:N tra piastre e modelli di macchina.
/// </summary>
public interface ICompatibilitaRepository : IRepository<PiastraMacchinaCompatibile>
{
    /// <summary>Restituisce tutte le macchine compatibili con una piastra (con navigazioni caricate).</summary>
    Task<IEnumerable<PiastraMacchinaCompatibile>> GetByPiastraAsync(int idPiastra);

    /// <summary>Restituisce tutte le piastre compatibili con una macchina (con navigazioni caricate).</summary>
    Task<IEnumerable<PiastraMacchinaCompatibile>> GetByMacchinaAsync(int idMacchinaStandard);

    /// <summary>Verifica se una coppia (piastra, macchina) è già registrata — usato per bloccare i duplicati.</summary>
    Task<bool> ExistsAsync(int idPiastra, int idMacchinaStandard);

    /// <summary>
    /// True se la macchina ha già una piastra <see cref="Core.Enums.TipoPiastra.Standard"/> associata (attiva).
    /// Una macchina può avere al più una piastra Standard; le SpecialeCliente non sono limitate.
    /// </summary>
    Task<bool> HasPiastraStandardAsync(int idMacchinaStandard);

    /// <summary>Id delle macchine che hanno già una piastra Standard associata (attiva) — per filtrare i selettori.</summary>
    Task<IReadOnlyCollection<int>> GetIdMacchineConPiastraStandardAsync();

    /// <summary>Attiva/disattiva una compatibilità senza eliminarla dal registro storico.</summary>
    Task SetAttivaAsync(int idCompatibilita, bool attiva);
}
