using PlateArchive.Core.Enums;

namespace PlateArchive.Services;

/// <summary>
/// Copia i file disegno (DWG/DXF/PDF) nella cartella condivisa aziendale rispettando
/// la struttura:
/// <code>
///   {cartellaBase}\Standard\{codicePiastra}{ext}          ← piastre standard
///   {cartellaBase}\Clienti\{codiceCliente}\{codicePiastra}{ext}  ← piastre speciali
/// </code>
/// La directory viene creata automaticamente al primo salvataggio.
/// </summary>
public class FileArchivioService(string cartellaBase) : IFileArchivioService
{
    public bool IsConfigurato => !string.IsNullOrWhiteSpace(cartellaBase);

    public async Task<string?> ArchiviaDisegnoAsync(
        string      percorsoOrigine,
        string      codicePiastra,
        TipoPiastra tipoPiastra,
        string?     codiceCliente = null)
    {
        if (!IsConfigurato)            return null;
        if (!File.Exists(percorsoOrigine)) return null;

        var sottocartella = tipoPiastra == TipoPiastra.SpecialeCliente && !string.IsNullOrWhiteSpace(codiceCliente)
            ? Path.Combine(cartellaBase, "Clienti", codiceCliente)
            : Path.Combine(cartellaBase, "Standard");

        Directory.CreateDirectory(sottocartella);

        var ext                  = Path.GetExtension(percorsoOrigine);
        var nomeFile             = $"{codicePiastra}{ext}";
        var percorsoDestinazione = Path.Combine(sottocartella, nomeFile);

        await Task.Run(() => File.Copy(percorsoOrigine, percorsoDestinazione, overwrite: true));

        return percorsoDestinazione;
    }
}
