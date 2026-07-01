namespace PlateArchive.Services;

/// <summary>
/// Riga di ordine di vendita non evasa, letta dal gestionale (DB2/Panthera).
/// Sola lettura: non ha una controparte locale nel database di PlateArchive.
/// <para>
/// AnnoOrdine/NumeroOrdine/RigaOrdine sono stringhe e non numeri: alcune di queste colonne
/// DB2 sono <c>CHARACTER(10)</c> e possono contenere causale e numero concatenati
/// (es. <c>"VS  003071"</c>), non un valore numerico puro.
/// </para>
/// </summary>
public record RigaOrdineVendita(
    string AnnoOrdine,
    string NumeroOrdine,
    string RigaOrdine,
    string CodiceArticolo,
    string DescrizioneEstesa,
    string RagioneSocialeCliente,
    string CodiceClienteGestionale);
