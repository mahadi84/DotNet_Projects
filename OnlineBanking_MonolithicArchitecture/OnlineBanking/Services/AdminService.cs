using Microsoft.EntityFrameworkCore;
using OnlineBanking.Common;
using OnlineBanking.Contracts;
using OnlineBanking.Data;
using OnlineBanking.Entities;
using OnlineBanking.Security;

namespace OnlineBanking.Services;

public sealed class AdminService : IAdminService
{
    private readonly AppDbContext _db;
    private readonly AuditWriter _audit;

    public AdminService(AppDbContext db, AuditWriter audit)
    {
        _db = db;
        _audit = audit;
    }

    // Returns all customers for admin listing
    public async Task<IReadOnlyList<Customer>> GetCustomersAsync()
    {
        return await _db.Customers.AsNoTracking()
            .OrderBy(c => c.AccountNumber)
            .ToListAsync();
    }

    public async Task<Customer?> GetCustomerByAccountAsync(string accountNumber)
    {
        accountNumber = (accountNumber ?? "").Trim();
        return await _db.Customers.FirstOrDefaultAsync(c => c.AccountNumber == accountNumber);
    }

    public async Task<Result> SetAdminAsync(string accountNumber, bool isAdmin, string actorAcc, string ip, string ua)
    {
        var c = await GetCustomerByAccountAsync(accountNumber);
        if (c is null) return Result.Fail("Customer not found.");

        c.IsAdmin = isAdmin;
        await _db.SaveChangesAsync();

        await _audit.WriteAsync("ADMIN_SET_ROLE", actorAcc, c.AccountNumber, ip, ua, $"Set IsAdmin={isAdmin}");
        return Result.Ok("Role updated.");
    }

    public async Task<Result> LockAsync(string accountNumber, int minutes, string actorAcc, string ip, string ua)
    {
        var c = await GetCustomerByAccountAsync(accountNumber);
        if (c is null) return Result.Fail("Customer not found.");

        c.LockedUntilUtc = DateTimeOffset.UtcNow.AddMinutes(minutes);
        c.FailedLoginCount = 0;
        await _db.SaveChangesAsync();

        await _audit.WriteAsync("ADMIN_LOCK", actorAcc, c.AccountNumber, ip, ua, $"Locked for {minutes} minutes");
        return Result.Ok("Locked.");
    }

    public async Task<Result> UnlockAsync(string accountNumber, string actorAcc, string ip, string ua)
    {
        var c = await GetCustomerByAccountAsync(accountNumber);
        if (c is null) return Result.Fail("Customer not found.");

        c.LockedUntilUtc = null;
        c.FailedLoginCount = 0;
        await _db.SaveChangesAsync();

        await _audit.WriteAsync("ADMIN_UNLOCK", actorAcc, c.AccountNumber, ip, ua, "Unlocked");
        return Result.Ok("Unlocked.");
    }

    public async Task<Result> ResetPasswordAsync(string accountNumber, string newPassword, string actorAcc, string ip, string ua)
    {
        var c = await GetCustomerByAccountAsync(accountNumber);
        if (c is null) return Result.Fail("Customer not found.");

        c.PasswordHash = PasswordHasher.Hash(newPassword);
        c.FailedLoginCount = 0;
        c.LockedUntilUtc = null;

        await _db.SaveChangesAsync();
        await _audit.WriteAsync("ADMIN_RESET_PASSWORD", actorAcc, c.AccountNumber, ip, ua, "Password reset");
        return Result.Ok("Password reset done.");
    }

    public async Task<IReadOnlyList<AuditLog>> GetLatestAuditAsync(int take = 100)
    {
        return await _db.AuditLogs.AsNoTracking()
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(take)
            .ToListAsync();
    }
}
