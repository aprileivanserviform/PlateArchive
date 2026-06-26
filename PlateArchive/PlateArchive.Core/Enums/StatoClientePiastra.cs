namespace PlateArchive.Core.Enums;

/// <summary>
/// Stato dell'associazione tra un cliente e una piastra.
/// Toggle Attiva ↔ Obsoleta disponibile dalla schermata dettaglio cliente
/// (pulsante per archiviare una piastra senza eliminarla dal database).
/// </summary>
public enum StatoClientePiastra
{
    /// <summary>Piastra attualmente in uso dal cliente.</summary>
    Attiva,
    /// <summary>Piastra non più in uso — mantenuta per storico.</summary>
    Obsoleta,
    /// <summary>Piastra proposta al cliente (offerta commerciale in corso).</summary>
    Proposta,
    /// <summary>Piastra da verificare tecnicamente prima di confermare l'associazione.</summary>
    DaVerificare
}
