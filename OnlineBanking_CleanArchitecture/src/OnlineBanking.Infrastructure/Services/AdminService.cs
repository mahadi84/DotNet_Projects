using Microsoft.EntityFrameworkCore;
using OnlineBanking.Application.Common;
using OnlineBanking.Application.Contracts;
using OnlineBanking.Domain.Entities;
using OnlineBanking.Infrastructure.Persistence;
using OnlineBanking.Infrastructure.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineBanking.Infrastructure.Services
{
    public sealed class AdminService : IAdminService
    {
        // DB access
        private readonly AppDbContext _db;

        // audit writer
        private readonly AuditWriter _audit;

        public AdminService(AppDbContext db, AuditWriter audit)
        {
            _db = db;
            _audit = audit;
        }

        public async Task<IReadOnlyList<Customer>> GetCustomersAsync()
        {
            // read-only list, tracking off
            // order by accountNumber -> admin list clean
            return await _db.Customers.AsNoTracking()
                .OrderBy(c => c.AccountNumber)
                .ToListAsync();
        }

        public async Task<Customer?> GetCustomerByAccountAsync(string accountNumber)
        {
            // normalize
            accountNumber = accountNumber.Trim();

            // customer find by account number
            return await _db.Customers.FirstOrDefaultAsync(c => c.AccountNumber == accountNumber);
        }

        public async Task<Result> SetAdminAsync(string accountNumber, bool isAdmin, string actorAcc, string ip, string ua)
        {
            // target customer find
            var c = await GetCustomerByAccountAsync(accountNumber);
            if (c is null) return Result.Fail("Customer not found.");

            // role update
            c.IsAdmin = isAdmin;

            await _db.SaveChangesAsync();

            // audit write
            await _audit.WriteAsync("ADMIN_SET_ROLE", actorAcc, c.AccountNumber, ip, ua, $"Set IsAdmin={isAdmin}");

            return Result.Ok("Role updated.");
        }

        public async Task<Result> LockAsync(string accountNumber, int minutes, string actorAcc, string ip, string ua)
        {
            var c = await GetCustomerByAccountAsync(accountNumber);
            if (c is null) return Result.Fail("Customer not found.");

            // lock until now+minutes
            c.LockedUntilUtc = DateTimeOffset.UtcNow.AddMinutes(minutes);

            // reset fail count
            c.FailedLoginCount = 0;

            await _db.SaveChangesAsync();

            // audit write
            await _audit.WriteAsync("ADMIN_LOCK", actorAcc, c.AccountNumber, ip, ua, $"Locked for {minutes} minutes");

            return Result.Ok("Locked.");
        }

        public async Task<Result> UnlockAsync(string accountNumber, string actorAcc, string ip, string ua)
        {
            var c = await GetCustomerByAccountAsync(accountNumber);
            if (c is null) return Result.Fail("Customer not found.");

            // unlock
            c.LockedUntilUtc = null;

            // reset fail count
            c.FailedLoginCount = 0;

            await _db.SaveChangesAsync();

            // audit write
            await _audit.WriteAsync("ADMIN_UNLOCK", actorAcc, c.AccountNumber, ip, ua, "Unlocked");

            return Result.Ok("Unlocked.");
        }

        public async Task<Result> ResetPasswordAsync(string accountNumber, string newPassword, string actorAcc, string ip, string ua)
        {
            var c = await GetCustomerByAccountAsync(accountNumber);
            if (c is null) return Result.Fail("Customer not found.");

            // new password hash set
            c.PasswordHash = PasswordHasher.Hash(newPassword);

            // reset security lock state
            c.FailedLoginCount = 0;
            c.LockedUntilUtc = null;

            await _db.SaveChangesAsync();

            // audit write
            await _audit.WriteAsync("ADMIN_RESET_PASSWORD", actorAcc, c.AccountNumber, ip, ua, "Password reset");

            return Result.Ok("Password reset done.");
        }

        public async Task<IReadOnlyList<AuditLog>> GetLatestAuditAsync(int take = 100)
        {
            // latest audit logs return
            return await _db.AuditLogs.AsNoTracking()
                .OrderByDescending(a => a.CreatedAtUtc)
                .Take(take)
                .ToListAsync();
        }
    }
}
