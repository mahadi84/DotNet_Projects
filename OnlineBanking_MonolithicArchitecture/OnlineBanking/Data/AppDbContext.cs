using Microsoft.EntityFrameworkCore;
using OnlineBanking.Entities;

namespace OnlineBanking.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Customer>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200).IsRequired();
            e.Property(x => x.City).HasMaxLength(100).IsRequired();

            e.Property(x => x.AccountNumber).HasMaxLength(5).IsRequired();
            e.Property(x => x.PasswordHash).IsRequired();

            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.AccountNumber).IsUnique();

            e.HasOne(x => x.Account)
                .WithOne(a => a.Customer)
                .HasForeignKey<Account>(a => a.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Account>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Balance).HasPrecision(18, 2);
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        b.Entity<Transaction>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.BalanceAfter).HasPrecision(18, 2);
            e.Property(x => x.Reference).HasMaxLength(200);
            e.HasIndex(x => x.CreatedAtUtc);
        });

        b.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(80).IsRequired();
            e.Property(x => x.ActorAccountNumber).HasMaxLength(20).IsRequired();
            e.Property(x => x.TargetAccountNumber).HasMaxLength(20);
            e.Property(x => x.Ip).HasMaxLength(64);
            e.Property(x => x.UserAgent).HasMaxLength(300);
            e.Property(x => x.Message).HasMaxLength(500).IsRequired();
            e.HasIndex(x => x.CreatedAtUtc);
            e.HasIndex(x => x.Action);
        });
    }
}
