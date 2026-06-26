using Microsoft.EntityFrameworkCore;
using PlateArchive.Core.Models;

namespace PlateArchive.Data;

public class PlateArchiveDbContext(DbContextOptions<PlateArchiveDbContext> options) : DbContext(options)
{
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

        // Soft delete
        mb.Entity<FormatoMacchina>().HasQueryFilter(f => !f.IsEliminata);
        mb.Entity<ProduttoreMacchina>().HasQueryFilter(p => !p.IsEliminata);
        mb.Entity<Piastra>().HasQueryFilter(p => !p.IsEliminata);

        mb.Entity<Cliente>()
            .HasIndex(c => c.CodiceClienteGestionale).IsUnique();

        mb.Entity<MacchinaStandard>()
            .HasIndex(m => m.CodiceMacchina).IsUnique();

        // FK MacchinaStandard → FormatiMacchine (NO ACTION lato DB — ClientSetNull gestito da EF)
        mb.Entity<MacchinaStandard>()
            .HasOne(m => m.Formato)
            .WithMany(f => f.Macchine)
            .HasForeignKey(m => m.IdFormato)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.ClientSetNull);

        // FK MacchinaStandard → ProduttoriMacchine (SET NULL)
        mb.Entity<MacchinaStandard>()
            .HasOne(m => m.Produttore)
            .WithMany(p => p.Macchine)
            .HasForeignKey(m => m.IdProduttore)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        mb.Entity<Piastra>()
            .HasIndex(p => p.CodicePiastra).IsUnique();

        mb.Entity<Piastra>()
            .HasIndex(p => p.CodiceArticoloGestionale)
            .IsUnique()
            .HasFilter("[CodiceArticoloGestionale] IS NOT NULL");

        // FK Piastra → CategoriePiastre (SET NULL)
        mb.Entity<Piastra>()
            .HasOne(p => p.Categoria)
            .WithMany()
            .HasForeignKey(p => p.IdCategoriaPiastra)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // FK Piastra → FormatiMacchine (NO ACTION lato DB — ClientSetNull gestito da EF)
        mb.Entity<Piastra>()
            .HasOne(p => p.Formato)
            .WithMany(f => f.Piastre)
            .HasForeignKey(p => p.IdFormato)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.ClientSetNull);

        // Relazione 1:1 Piastra → Disegno
        mb.Entity<Disegno>()
            .HasIndex(d => d.IdPiastra).IsUnique();
        mb.Entity<Disegno>()
            .HasOne(d => d.Piastra)
            .WithOne(p => p.Disegno)
            .HasForeignKey<Disegno>(d => d.IdPiastra)
            .OnDelete(DeleteBehavior.Cascade);

        mb.Entity<ClienteMacchina>()
            .HasOne(cm => cm.Cliente)
            .WithMany(c => c.Macchine)
            .HasForeignKey(cm => cm.IdCliente);

        mb.Entity<ClienteMacchina>()
            .HasOne(cm => cm.MacchinaStandard)
            .WithMany(m => m.ClientiAssociati)
            .HasForeignKey(cm => cm.IdMacchinaStandard);

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

        mb.Entity<ClientePiastra>()
            .HasOne(cp => cp.ClienteMacchina)
            .WithMany()
            .HasForeignKey(cp => cp.IdClienteMacchina)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.ClientSetNull);
    }
}
