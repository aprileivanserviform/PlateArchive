using PlateArchive.Core.Enums;

namespace PlateArchive.Services;

public interface IFileArchivioService
{
    /// <summary>
    /// Copia il disegno nella sottocartella corretta dell'archivio condiviso.
    /// <para>
    /// Standard        → {cartellaBase}\Piastre Standard\{codicePiastra}{ext}
    /// SpecialeCliente → {cartellaBase}\Piastre Clienti\{codiceCliente} - {ragioneSociale}\{codicePiastra}{ext}
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
    /// Percorso risultante: {cartellaBase}\Piastre Clienti\{codiceCliente} - {ragioneSociale}\Allegati\{nomeFile}
    /// Se il nome esiste già viene aggiunto un suffisso numerico (_2, _3 …).
    /// Restituisce null se la cartella non è configurata o il file sorgente non esiste.
    /// </summary>
    Task<string?> ArchiviaAllegatoClienteAsync(
        string percorsoOrigine,
        string nomeFile,
        string codiceCliente,
        string ragioneSociale);

    /// <summary>
    /// Restituisce il percorso di destinazione che verrebbe usato da <see cref="ArchiviaDisegnoAsync"/>
    /// senza copiare il file. Utile per controllare se esiste già un conflitto di nome.
    /// Restituisce null se la cartella non è configurata.
    /// </summary>
    string? GetPercorsoDestinazioneDisegno(
        string      percorsoOrigine,
        TipoPiastra tipoPiastra,
        string?     codiceCliente  = null,
        string?     ragioneSociale = null);

    bool IsConfigurato { get; }
}
