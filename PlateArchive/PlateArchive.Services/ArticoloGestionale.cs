namespace PlateArchive.Services;

/// <summary>
/// Articolo dell'anagrafica del gestionale (DB2/Panthera, THIP.ARTICOLI).
/// Sola lettura: usato dal selettore "Codice articolo gestionale" delle piastre
/// Speciale Cliente (TASK-19) per scegliere un codice reale invece di testo libero.
/// </summary>
public record ArticoloGestionale(string Codice, string Descrizione);
