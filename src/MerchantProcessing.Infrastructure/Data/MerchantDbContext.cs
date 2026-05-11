using Microsoft.EntityFrameworkCore;
using MerchantProcessing.Domain.Entities;

namespace MerchantProcessing.Infrastructure.Data;

public class MerchantDbContext : DbContext
{
    public MerchantDbContext(DbContextOptions<MerchantDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<ConsolidatedDaily> ConsolidatedDaily => Set<ConsolidatedDaily>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Account configuration
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Balance).HasPrecision(18, 2);
            entity.Property(e => e.Version).IsConcurrencyToken();
            entity.HasIndex(e => e.LastUpdated);
        });

        // Transaction configuration
        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.BalanceSnapshot).HasPrecision(18, 2);
            entity.HasIndex(e => new { e.AccountId, e.Timestamp });
            entity.HasIndex(e => e.IdempotencyKey).IsUnique();
            
            entity.HasOne(e => e.Account)
                .WithMany(a => a.Transactions)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ConsolidatedDaily configuration
        modelBuilder.Entity<ConsolidatedDaily>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.AccountId, e.Date }).IsUnique();
            entity.Property(e => e.OpeningBalance).HasPrecision(18, 2);
            entity.Property(e => e.TotalCredits).HasPrecision(18, 2);
            entity.Property(e => e.TotalDebits).HasPrecision(18, 2);
            entity.Property(e => e.ClosingBalance).HasPrecision(18, 2);
            
            entity.HasOne(e => e.Account)
                .WithMany()
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // IdempotencyKey configuration
        modelBuilder.Entity<IdempotencyKey>(entity =>
        {
            entity.HasKey(e => e.Key);
            entity.HasIndex(e => e.ExpiresAt);
        });
    }
}
