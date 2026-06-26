namespace PlateArchive.Core.Models;

/// <summary>
/// Rappresenta un cliente dell'azienda.
/// L'anagrafica proviene dal gestionale DB2 (sincronizzazione automatica):
/// <see cref="CodiceClienteGestionale"/> è la chiave primaria condivisa con il gestionale.
/// Un cliente può possedere più macchine (<see cref="Macchine"/>) e più piastre (<see cref="Piastre"/>).
/// </summary>
public class Cliente
{
    public int    IdCliente                 { get; set; }

    /// <summary>Chiave univoca nel gestionale DB2 — usata per la sincronizzazione.</summary>
    public string CodiceClienteGestionale  { get; set; } = string.Empty;
    public string RagioneSociale           { get; set; } = string.Empty;
    public string? Note                    { get; set; }

    // ─── Navigazioni ─────────────────────────────────────────────────────────

    /// <summary>Macchine standard acquistate/utilizzate da questo cliente.</summary>
    public ICollection<ClienteMacchina> Macchine { get; set; } = [];

    /// <summary>Piastre associate a questo cliente.</summary>
    public ICollection<ClientePiastra>  Piastre  { get; set; } = [];
}
