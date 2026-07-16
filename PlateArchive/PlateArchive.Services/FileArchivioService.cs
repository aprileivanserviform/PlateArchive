using PlateArchive.Core.Enums;

namespace PlateArchive.Services;

/// <summary>
/// Copia i file nella cartella condivisa aziendale rispettando la struttura:
/// <code>
///   {cartellaBase}\Standard\{codicePiastra}{ext}
///   {cartellaBase}\Clienti\{codiceCliente} - {ragioneSociale}\{codicePiastra}{ext}
///   {cartellaBase}\Clienti\{codiceCliente} - {ragioneSociale}\Allegati\{nomeFile}
/// </code>
/// Le directory vengono create automaticamente al primo salvataggio.
/// </summary>
public class FileArchivioService(string cartellaBase) : IFileArchivioService
{
    public bool IsConfigurato => !string.IsNullOrWhiteSpace(cartellaBase);

    public async Task<string?> ArchiviaDisegnoAsync(
        string      percorsoOrigine,
        string      codicePiastra,
        TipoPiastra tipoPiastra,
        string?     codiceCliente  = null,
        string?     ragioneSociale = null)
    {
        if (!IsConfigurato)             return null;
        if (!File.Exists(percorsoOrigine)) return null;

        var sottocartella = tipoPiastra == TipoPiastra.SpecialeCliente && !string.IsNullOrWhiteSpace(codiceCliente)
            ? GetCartellaCliente(codiceCliente, ragioneSociale)
            : Path.Combine(cartellaBase, "Standard");

        Directory.CreateDirectory(sottocartella);

        var ext                  = Path.GetExtension(percorsoOrigine);
        var percorsoDestinazione = Path.Combine(sottocartella, $"{codicePiastra}{ext}");

        await Task.Run(() => File.Copy(percorsoOrigine, percorsoDestinazione, overwrite: true));

        return percorsoDestinazione;
    }

    public async Task<string?> ArchiviaAllegatoClienteAsync(
        string percorsoOrigine,
        string nomeFile,
        string codiceCliente,
        string ragioneSociale)
    {
        if (!IsConfigurato)             return null;
        if (!File.Exists(percorsoOrigine)) return null;

        var cartellaAllegati = Path.Combine(GetCartellaCliente(codiceCliente, ragioneSociale), "Allegati");
        Directory.CreateDirectory(cartellaAllegati);

        var percorsoDestinazione = Path.Combine(cartellaAllegati, nomeFile);

        // Suffisso numerico se il nome esiste già nella cartella allegati.
        if (File.Exists(percorsoDestinazione))
        {
            var ext      = Path.GetExtension(nomeFile);
            var baseName = Path.GetFileNameWithoutExtension(nomeFile);
            int counter  = 2;
            do { percorsoDestinazione = Path.Combine(cartellaAllegati, $"{baseName}_{counter++}{ext}"); }
            while (File.Exists(percorsoDestinazione));
        }

        await Task.Run(() => File.Copy(percorsoOrigine, percorsoDestinazione));

        return percorsoDestinazione;
    }

    // ─── helpers ──────────────────────────────────────────────────────────────

    private string GetCartellaCliente(string codiceCliente, string? ragioneSociale)
    {
        var nomeCartella = string.IsNullOrWhiteSpace(ragioneSociale)
            ? codiceCliente
            : $"{codiceCliente} - {SanitizzaNome(ragioneSociale)}";
        return Path.Combine(cartellaBase, "Clienti", nomeCartella);
    }

    private static string SanitizzaNome(string nome)
    {
        var invalidi = Path.GetInvalidFileNameChars();
        return string.Concat(nome.Select(c => invalidi.Contains(c) ? '_' : c)).Trim();
    }
}
