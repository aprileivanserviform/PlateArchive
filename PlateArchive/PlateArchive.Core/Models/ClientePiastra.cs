using PlateArchive.Core.Enums;

namespace PlateArchive.Core.Models;

/// <summary>
/// Associazione tra un <see cref="Cliente"/> e una <see cref="Piastra"/>.
/// Rappresenta il fatto che un cliente utilizza o ha ricevuto quella piastra.
/// <para>
/// <see cref="IdClienteMacchina"/> è NULLABLE per design:
/// una piastra può essere associata a un cliente senza specificare
/// su quale macchina viene montata (la macchina è un dettaglio opzionale).
/// </para>
/// </summary>
public class ClientePiastra
{
    public int IdClientePiastra { get; set; }
    public int IdCliente        { get; set; }
    public int IdPiastra        { get; set; }

    /// <summary>
    /// Macchina del cliente su cui la piastra è montata — opzionale.
    /// Nullable per vincolo di dominio: il cliente può non aver specificato la macchina.
    /// </summary>
    public int? IdClienteMacchina { get; set; }
    public DateTime DataAssociazione { get; set; }
    public StatoClientePiastra Stato { get; set; }
    public string? Note              { get; set; }

    // ─── Navigazioni ─────────────────────────────────────────────────────────
    public Cliente          Cliente         { get; set; } = null!;
    public Piastra          Piastra         { get; set; } = null!;
    public ClienteMacchina? ClienteMacchina { get; set; }
}
