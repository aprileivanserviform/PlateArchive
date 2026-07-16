using PlateArchive.Core.Enums;

namespace PlateArchive.Services;

public interface IFileArchivioService
{
    /// <summary>
    /// Copia il disegno nella sottocartella corretta dell'archivio condiviso.
    /// <para>
    /// Standard        → {cartellaBase}\Standard\{codicePiastra}{ext}
    /// SpecialeCliente → {cartellaBase}\Clienti\{codiceCliente} - {ragioneSociale}\{codicePiastra}{ext}
    /// </para>
    /// Restituisce null se la cartella condivisa non è configurata o il file non esiste.
    /// </summary>
    Task<string?> ArchiviaDisegnoAsync(
        string      percorsoOrigine,
        string      codicePiastra,
        TipoPiastra tipoPiastra,
        string?     codiceCliente   = null,
        string?     ragioneSociale  = null);

    /// <summary>
    /// Copia un allegato generico nella sottocartella Allegati del cliente.
    /// Percorso risultante: {cartellaBase}\Clienti\{codiceCliente} - {ragioneSociale}\Allegati\{nomeFile}
    /// Se il nome esiste già viene aggiunto un suffisso numerico (_2, _3 …).
    /// Restituisce null se la cartella non è configurata o il file sorgente non esiste.
    /// </summary>
    Task<string?> ArchiviaAllegatoClienteAsync(
        string percorsoOrigine,
        string nomeFile,
        string codiceCliente,
        string ragioneSociale);

    bool IsConfigurato { get; }
}
