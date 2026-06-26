namespace PlateArchive.Services;

/// <summary>
/// Servizio che copia i file disegno (DWG/DXF/PDF) nella cartella condivisa aziendale.
/// Il percorso della cartella è configurato in appsettings.json (CartellaCondivisaDisegni).
/// Il file viene rinominato con il codice piastra per garantire univocità.
/// </summary>
public class FileArchivioService(string cartellaBase) : IFileArchivioService
{
    /// <summary>False se CartellaCondivisaDisegni non è configurato — il bottone "Archivia" rimane disabilitato.</summary>
    public bool IsConfigurato => !string.IsNullOrWhiteSpace(cartellaBase);

    public async Task<string?> ArchiviaDisegnoAsync(string percorsoOrigine, string codicePiastra)
    {
        if (!IsConfigurato)    return null;
        if (!File.Exists(percorsoOrigine)) return null;

        // Il file di destinazione si chiama CodicePiastra + estensione originale
        // (es. PLT-000001.dwg). Overwrite: se aggiorno il disegno, sovrascrive il vecchio.
        var ext                  = Path.GetExtension(percorsoOrigine);
        var nomeFile             = $"{codicePiastra}{ext}";
        var percorsoDestinazione = Path.Combine(cartellaBase, nomeFile);

        Directory.CreateDirectory(cartellaBase);
        await Task.Run(() => File.Copy(percorsoOrigine, percorsoDestinazione, overwrite: true));

        return percorsoDestinazione;
    }
}
