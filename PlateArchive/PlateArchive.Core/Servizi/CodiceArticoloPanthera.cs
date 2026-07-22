using System.Globalization;

namespace PlateArchive.Core.Servizi;

/// <summary>
/// Parsing della nuova codifica articoli del gestionale (Panthera) per le piastre.
/// <para>
/// Il codice è composto da 16 cifre + suffisso (es. <c>3010351020000000-G</c>):
/// famiglia (pos 1-2), spessore (pos 3-4), durezza (pos 5-6), formato (pos 7-10).
/// Il formato è espresso in decimi: <c>1020</c> = formato 102, <c>0760</c> = 76;
/// <c>000</c> = lastra grezza (nessun formato, il disegno non serve).
/// I codici della vecchia codifica contengono lettere (es. <c>30C1K2K310SP160M-G</c>)
/// e non partecipano ad alcun match: verranno sospesi lato gestionale.
/// </para>
/// </summary>
public static class CodiceArticoloPanthera
{
    private const int LunghezzaParteNumerica = 16;
    private const int InizioFormato          = 6; // pos 7, 0-based
    private const int LunghezzaFormato       = 4; // pos 7-10

    /// <summary>True se il codice segue la nuova codifica (primi 16 caratteri tutti cifre).</summary>
    public static bool IsNuovaCodifica(string? codice) =>
        codice is { Length: >= LunghezzaParteNumerica }
        && codice.Take(LunghezzaParteNumerica).All(char.IsAsciiDigit);

    /// <summary>
    /// Estrae il formato dalle posizioni 7-10 del codice (decimi: <c>1020</c> → 102).
    /// False per i codici della vecchia codifica; per le lastre grezze restituisce true
    /// con <paramref name="formato"/> = 0.
    /// </summary>
    public static bool TryEstraiFormato(string? codice, out decimal formato)
    {
        formato = 0;
        if (!IsNuovaCodifica(codice)) return false;

        formato = int.Parse(codice!.Substring(InizioFormato, LunghezzaFormato)) / 10m;
        return true;
    }

    /// <summary>True se il codice è di una lastra grezza (nuova codifica con formato 000).</summary>
    public static bool IsGrezza(string? codice) =>
        TryEstraiFormato(codice, out var formato) && formato == 0;

    /// <summary>
    /// Confronta il formato estratto dal codice articolo con il nome di un
    /// <c>FormatoMacchina</c> (es. "102"), tollerando separatori decimali diversi.
    /// </summary>
    public static bool FormatoCompatibile(decimal formatoCodice, string? nomeFormato)
    {
        if (string.IsNullOrWhiteSpace(nomeFormato)) return false;

        return decimal.TryParse(nomeFormato.Trim().Replace(',', '.'),
                                NumberStyles.Number, CultureInfo.InvariantCulture, out var valore)
            && valore == formatoCodice;
    }
}
