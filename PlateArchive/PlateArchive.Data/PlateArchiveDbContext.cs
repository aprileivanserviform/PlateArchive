using Microsoft.EntityFrameworkCore;
using PlateArchive.Core.Models;

namespace PlateArchive.Data;

/// <summary>
/// DbContext principale dell'applicazione.
/// Configurato in App.xaml.cs con SQL Server (prod) o SQLite (dev/test).
/// <para>
/// Ogni schermata WPF ottiene la propria istanza (Scoped) tramite NavigationService:
/// quando si naviga altrove il scope viene distrutto → DbContext rilasciato.
/// </para>
/// </summary>
public class PlateArchiveDbContext(DbContextOptions<PlateArchiveDbContext> options) : DbContext(options)
{
    // ─── DbSet (uno per tabella) ──────────────────────────────────────────────

    public DbSet<Cliente>                    Clienti                    => Set<Cliente>();
    public DbSet<MacchinaStandard>           MacchineStandard           => Set<MacchinaStandard>();
    public DbSet<FormatoMacchina>            FormatiMacchine            => Set<FormatoMacchina>();
    public DbSet<ProduttoreMacchina>         ProduttoriMacchine         => Set<ProduttoreMacchina>();
    public DbSet<CategoriaPiastra>           CategoriePiastre           => Set<CategoriaPiastra>();
    public DbSet<Piastra>                    Piastre                    => Set<Piastra>();
    public DbSet<Disegno>                    Disegni                    => Set<Disegno>();
    public DbSet<PiastraMacchinaCompatibile> PiastreMacchineCompatibili => Set<PiastraMacchinaCompatibile>();
    public DbSet<ClienteMacchina>            ClientiMacchine            => Set<ClienteMacchina>();
    public DbSet<ClientePiastra>             ClientiPiastre             => Set<ClientePiastra>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // ─── Chiavi primarie ──────────────────────────────────────────────────

        mb.Entity<Cliente>().HasKey(c => c.IdCliente);
        mb.Entity<MacchinaStandard>().HasKey(m => m.IdMacchinaStandard);
        mb.Entity<FormatoMacchina>().HasKey(f => f.IdFormato);
        mb.Entity<ProduttoreMacchina>().HasKey(p => p.IdProduttore);
        mb.Entity<CategoriaPiastra>().HasKey(c => c.IdCategoriaPiastra);
        mb.Entity<CategoriaPiastra>().HasIndex(c => c.Codice).IsUnique();
        mb.Entity<Piastra>().HasKey(p => p.IdPiastra);
        mb.Entity<Disegno>().HasKey(d => d.IdDisegno);
        mb.Entity<PiastraMacchinaCompatibile>().HasKey(x => x.IdCompatibilita);
        mb.Entity<ClienteMacchina>().HasKey(cm => cm.IdClienteMacchina);
        mb.Entity<ClientePiastra>().HasKey(cp => cp.IdClientePiastra);

        // ─── Query filter soft-delete ─────────────────────────────────────────
        // Le entità con IsEliminata = true vengono automaticamente escluse da tutte le query.
        // Per bypassare il filtro (es. in repository specializzati) si usa .IgnoreQueryFilters().

        mb.Entity<FormatoMacchina>().HasQueryFilter(f => !f.IsEliminata);
        mb.Entity<ProduttoreMacchina>().HasQueryFilter(p => !p.IsEliminata);
        mb.Entity<Piastra>().HasQueryFilter(p => !p.IsEliminata);

        // ─── Indici univoci ───────────────────────────────────────────────────

        mb.Entity<Cliente>()
            .HasIndex(c => c.CodiceClienteGestionale).IsUnique();

        mb.Entity<MacchinaStandard>()
            .HasIndex(m => m.CodiceMacchina).IsUnique();

        // ─── FK MacchinaStandard → FormatiMacchine ────────────────────────────
        // ClientSetNull genera ON DELETE NO ACTION nel DB (evita Msg 1785 — multiple cascade paths).
        // Quando il formato viene eliminato logicamente, EF Core imposta IdFormato = null in memoria
        // prima di chiamare SaveChanges, senza propagazione a cascata lato DB.

        mb.Entity<MacchinaStandard>()
            .HasOne(m => m.Formato)
            .WithMany(f => f.Macchine)
            .HasForeignKey(m => m.IdFormato)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.ClientSetNull);

        // FK MacchinaStandard → ProduttoriMacchine (SET NULL — percorso unico, nessun Msg 1785)
        mb.Entity<MacchinaStandard>()
            .HasOne(m => m.Produttore)
            .WithMany(p => p.Macchine)
            .HasForeignKey(m => m.IdProduttore)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // ─── Piastra ──────────────────────────────────────────────────────────

        mb.Entity<Piastra>()
            .HasIndex(p => p.CodicePiastra).IsUnique();

        // CodiceArticoloGestionale univoco solo quando valorizzato (NULL non viola l'unique).
        mb.Entity<Piastra>()
            .HasIndex(p => p.CodiceArticoloGestionale)
            .IsUnique()
            .HasFilter("[CodiceArticoloGestionale] IS NOT NULL");

        // FK Piastra → CategoriePiastre (SET NULL — percorso unico)
        mb.Entity<Piastra>()
            .HasOne(p => p.Categoria)
            .WithMany()
            .HasForeignKey(p => p.IdCategoriaPiastra)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // FK Piastra → FormatiMacchine (ClientSetNull — stesso motivo di MacchinaStandard)
        mb.Entity<Piastra>()
            .HasOne(p => p.Formato)
            .WithMany(f => f.Piastre)
            .HasForeignKey(p => p.IdFormato)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.ClientSetNull);

        // ─── Disegno → Piastra (1:1) ─────────────────────────────────────────
        // Ogni disegno appartiene a esattamente una piastra.
        // Il vincolo UNIQUE su IdPiastra garantisce che una piastra abbia al più un disegno.

        mb.Entity<Disegno>()
            .HasIndex(d => d.IdPiastra).IsUnique();

        mb.Entity<Disegno>()
            .HasOne(d => d.Piastra)
            .WithOne(p => p.Disegno)
            .HasForeignKey<Disegno>(d => d.IdPiastra)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // ─── Piastra.IdClienteEsclusivo → Cliente ────────────────────────────
        // Solo le piastre SpecialeCliente hanno questa FK valorizzata.
        // ClientSetNull — evita cascade multipli (ClientePiastra già punta a Cliente).

        mb.Entity<Piastra>()
            .HasOne(p => p.ClienteEsclusivo)
            .WithMany()
            .HasForeignKey(p => p.IdClienteEsclusivo)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.ClientSetNull);

        // ─── ClienteMacchina (associazione cliente ↔ macchina standard) ──────

        mb.Entity<ClienteMacchina>()
            .HasOne(cm => cm.Cliente)
            .WithMany(c => c.Macchine)
            .HasForeignKey(cm => cm.IdCliente);

        mb.Entity<ClienteMacchina>()
            .HasOne(cm => cm.MacchinaStandard)
            .WithMany(m => m.ClientiAssociati)
            .HasForeignKey(cm => cm.IdMacchinaStandard);

        // ─── PiastraMacchinaCompatibile (N:N piastre ↔ macchine) ─────────────

        // Coppia (IdPiastra, IdMacchinaStandard) univoca — evita duplicati.
        mb.Entity<PiastraMacchinaCompatibile>()
            .HasIndex(x => new { x.IdPiastra, x.IdMacchinaStandard }).IsUnique();

        mb.Entity<PiastraMacchinaCompatibile>()
            .HasOne(x => x.Piastra)
            .WithMany(p => p.MacchineCompatibili)
            .HasForeignKey(x => x.IdPiastra);

        mb.Entity<PiastraMacchinaCompatibile>()
            .HasOne(x => x.MacchinaStandard)
            .WithMany(m => m.PiastreCompatibili)
            .HasForeignKey(x => x.IdMacchinaStandard);

        // ─── ClientePiastra (associazione cliente ↔ piastra) ─────────────────

        // Coppia (IdCliente, IdPiastra) univoca — un cliente non può avere la stessa piastra due volte.
        mb.Entity<ClientePiastra>()
            .HasIndex(x => new { x.IdCliente, x.IdPiastra }).IsUnique();

        mb.Entity<ClientePiastra>()
            .HasOne(cp => cp.Cliente)
            .WithMany(c => c.Piastre)
            .HasForeignKey(cp => cp.IdCliente);

        mb.Entity<ClientePiastra>()
            .HasOne(cp => cp.Piastra)
            .WithMany(p => p.ClientiAssociati)
            .HasForeignKey(cp => cp.IdPiastra);

        // FK ClientePiastra → ClienteMacchina opzionale (ClientSetNull — evita cascade multipli).
        // Eliminare una ClienteMacchina non elimina le ClientePiastre collegate: IdClienteMacchina → null.
        mb.Entity<ClientePiastra>()
            .HasOne(cp => cp.ClienteMacchina)
            .WithMany()
            .HasForeignKey(cp => cp.IdClienteMacchina)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
