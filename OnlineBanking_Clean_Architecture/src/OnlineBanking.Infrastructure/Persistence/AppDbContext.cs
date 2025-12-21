using Microsoft.EntityFrameworkCore;
using OnlineBanking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineBanking.Infrastructure.Persistence
{
    public sealed class AppDbContext : DbContext
    {
        // DbContext এর constructor
        // options এর মধ্যে connection string/provider/config থাকে
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Customers টেবিল/collection এর EF Core mapping
        public DbSet<Customer> Customers => Set<Customer>();

        // Accounts টেবিল/collection এর EF Core mapping
        public DbSet<Account> Accounts => Set<Account>();

        // Transactions টেবিল/collection এর EF Core mapping
        public DbSet<Transaction> Transactions => Set<Transaction>();

        // AuditLogs টেবিল/collection এর EF Core mapping
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

        // Model configuration (Fluent API)
        // DB schema, constraints, relationships এখানে define করা হয়
        protected override void OnModelCreating(ModelBuilder b)
        {
            // ---------------------------
            // Customer entity mapping
            // ---------------------------
            b.Entity<Customer>(e =>
            {
                // Primary key
                e.HasKey(x => x.Id);

                // Name field max length + required (NOT NULL)
                e.Property(x => x.Name).HasMaxLength(100).IsRequired();

                // Email field max length + required
                e.Property(x => x.Email).HasMaxLength(200).IsRequired();

                // City field max length + required
                e.Property(x => x.City).HasMaxLength(100).IsRequired();

                // AccountNumber 5 char + required
                // (তুমি 5-digit ধরে নিয়েছ—DB লেভেলে max length 5 enforce হচ্ছে)
                e.Property(x => x.AccountNumber).HasMaxLength(5).IsRequired();

                // PasswordHash required (plain password কখনো রাখা হবে না)
                e.Property(x => x.PasswordHash).IsRequired();

                // Email unique index -> duplicate email ঢুকবে না
                e.HasIndex(x => x.Email).IsUnique();

                // AccountNumber unique index -> duplicate account number ঢুকবে না
                e.HasIndex(x => x.AccountNumber).IsUnique();

                // Customer ↔ Account (One-to-One)
                // Customer has one Account, Account has one Customer
                // ForeignKey: Account.CustomerId
                // Cascade delete: Customer delete হলে Account delete হবে
                e.HasOne(x => x.Account)
                    .WithOne(a => a.Customer)
                    .HasForeignKey<Account>(a => a.CustomerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------------------------
            // Account entity mapping
            // ---------------------------
            b.Entity<Account>(e =>
            {
                // Primary key
                e.HasKey(x => x.Id);

                // Balance decimal precision set (18,2) -> 999... with 2 decimal
                e.Property(x => x.Balance).HasPrecision(18, 2);

                // RowVersion কে concurrency token বানায়
                // একই সময়ে update হলে EF Core conflict detect করবে
                e.Property(x => x.RowVersion).IsRowVersion();
            });

            // ---------------------------
            // Transaction entity mapping
            // ---------------------------
            b.Entity<Transaction>(e =>
            {
                // Primary key
                e.HasKey(x => x.Id);

                // Amount precision (18,2)
                e.Property(x => x.Amount).HasPrecision(18, 2);

                // BalanceAfter precision (18,2)
                e.Property(x => x.BalanceAfter).HasPrecision(18, 2);

                // Reference optional text length limit
                e.Property(x => x.Reference).HasMaxLength(200);

                // CreatedAtUtc এর উপর index -> statement / history query দ্রুত হবে
                e.HasIndex(x => x.CreatedAtUtc);
            });

            // ---------------------------
            // AuditLog entity mapping
            // ---------------------------
            b.Entity<AuditLog>(e =>
            {
                // Primary key
                e.HasKey(x => x.Id);

                // Action required + length limit (ex: LOGIN_SUCCESS)
                e.Property(x => x.Action).HasMaxLength(80).IsRequired();

                // ActorAccountNumber required + length limit
                // কে কাজ করেছে (accountNumber / SYSTEM)
                e.Property(x => x.ActorAccountNumber).HasMaxLength(20).IsRequired();

                // TargetAccountNumber optional + length limit
                // যাকে নিয়ে কাজ (transfer receiver, etc.)
                e.Property(x => x.TargetAccountNumber).HasMaxLength(20);

                // IP optional + length limit
                e.Property(x => x.Ip).HasMaxLength(64);

                // UserAgent optional + length limit
                e.Property(x => x.UserAgent).HasMaxLength(300);

                // Message required + length limit
                e.Property(x => x.Message).HasMaxLength(500).IsRequired();

                // Index on CreatedAtUtc -> latest logs দ্রুত query
                e.HasIndex(x => x.CreatedAtUtc);

                // Index on Action -> action filter (LOGIN_FAIL) দ্রুত
                e.HasIndex(x => x.Action);
            });
        }
    }
}
