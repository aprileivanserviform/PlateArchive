namespace PlateArchive.Core.Enums;

/// <summary>
/// Fonte da cui è stata determinata la compatibilità tra piastra e macchina.
/// Non ancora usata nell'UI v1, predisposta per audit tecnico futuro.
/// </summary>
public enum FonteDatoCompatibilita
{
    /// <summary>Compatibilità derivata dal disegno tecnico.</summary>
    Disegno,
    /// <summary>Compatibilità inserita manualmente da un operatore.</summary>
    Manuale,
    /// <summary>Compatibilità verificata su una macchina fisica.</summary>
    VerificaTecnica
}
