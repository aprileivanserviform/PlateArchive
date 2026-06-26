namespace PlateArchive.Core.Enums;

/// <summary>
/// Stato del ciclo di vita di un disegno tecnico.
/// La dashboard mostra i disegni "Da verificare" come to-do per il responsabile tecnico.
/// </summary>
public enum StatoDisegno
{
    /// <summary>Disegno verificato e approvato.</summary>
    Attivo,
    /// <summary>Disegno superato da una revisione successiva.</summary>
    Obsoleto,
    /// <summary>Disegno appena associato — in attesa di revisione tecnica.</summary>
    DaVerificare
}
