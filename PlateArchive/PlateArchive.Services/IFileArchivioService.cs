using PlateArchive.Core.Enums;

namespace PlateArchive.Services;

public interface IFileArchivioService
{
    /// <summary>
    /// Copia il file nella sottocartella corretta dell'archivio condiviso e restituisce il percorso di destinazione.
    /// <para>
    /// Standard       → {cartellaBase}\Standard\{codicePiastra}{ext}
    /// SpecialeCliente → {cartellaBase}\Clienti\{codiceCliente}\{codicePiastra}{ext}
    /// </para>
    /// Restituisce null se la cartella condivisa non è configurata o il file non esiste.
    /// </summary>
    Task<string?> ArchiviaDisegnoAsync(
        string      percorsoOrigine,
        string      codicePiastra,
        TipoPiastra tipoPiastra,
        string?     codiceCliente = null);

    bool IsConfigurato { get; }
}
