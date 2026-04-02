using DigitalWallet.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace DigitalWallet.Data;

public class WalletDbContext : DbContext
{
    public WalletDbContext(DbContextOptions<WalletDbContext> options) : base(options) { }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Transaction>(entity =>
        {
            // Unique constraint on Reference to prevent duplicate transactions
            entity.HasIndex(t => t.Reference).IsUnique();
            entity.Property(t => t.Amount).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<Client>(entity =>
        {
            entity.HasMany(c => c.Transactions)
                  .WithOne(t => t.Client)
                  .HasForeignKey(t => t.ClientId);
        });
    }
}
