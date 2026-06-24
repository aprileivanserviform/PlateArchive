namespace PlateArchive.Services;

public interface IFileArchivioService
{
    /// <summary>
    /// Copia il file di origine nella cartella condivisa e restituisce il percorso di destinazione.
    /// Restituisce null se la cartella condivisa non è configurata o il file non esiste.
    /// </summary>
    Task<string?> ArchiviaDisegnoAsync(string percorsoOrigine, string codicePiastra);

    bool IsConfigurato { get; }
}
