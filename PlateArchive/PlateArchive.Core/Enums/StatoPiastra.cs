namespace PlateArchive.Core.Enums;

/// <summary>
/// Stato del ciclo di vita di una piastra nel catalogo.
/// Usato per filtrare la lista piastre (la dashboard mostra le "Da verificare").
/// </summary>
public enum StatoPiastra
{
    /// <summary>Piastra in uso corrente.</summary>
    Attiva,
    /// <summary>Piastra fuori produzione — mantenuta per storico clienti.</summary>
    Obsoleta,
    /// <summary>Piastra appena inserita, in attesa di validazione tecnica.</summary>
    DaVerificare
}
