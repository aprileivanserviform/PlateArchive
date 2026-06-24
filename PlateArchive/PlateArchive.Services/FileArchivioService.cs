namespace PlateArchive.Services;

public class FileArchivioService(string cartellaBase) : IFileArchivioService
{
    public bool IsConfigurato => !string.IsNullOrWhiteSpace(cartellaBase);

    public async Task<string?> ArchiviaDisegnoAsync(string percorsoOrigine, string codicePiastra)
    {
        if (!IsConfigurato) return null;
        if (!File.Exists(percorsoOrigine)) return null;

        var ext                = Path.GetExtension(percorsoOrigine);
        var nomeFile           = $"{codicePiastra}{ext}";
        var percorsoDestinazione = Path.Combine(cartellaBase, nomeFile);

        Directory.CreateDirectory(cartellaBase);
        await Task.Run(() => File.Copy(percorsoOrigine, percorsoDestinazione, overwrite: true));

        return percorsoDestinazione;
    }
}
