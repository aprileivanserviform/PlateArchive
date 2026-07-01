namespace PlateArchive.Services;

/// <summary>
/// Riga di ordine di vendita non evasa, letta dal gestionale (DB2/Panthera).
/// Sola lettura: non ha una controparte locale nel database di PlateArchive.
/// </summary>
public record RigaOrdineVendita(
    int    AnnoOrdine,
    int    NumeroOrdine,
    int    RigaOrdine,
    string CodiceArticolo,
    string RagioneSocialeCliente);
