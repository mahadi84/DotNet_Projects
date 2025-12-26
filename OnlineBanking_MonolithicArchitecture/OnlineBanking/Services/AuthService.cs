using Microsoft.EntityFrameworkCore;
using OnlineBanking.Common;
using OnlineBanking.Contracts;
using OnlineBanking.Data;
using OnlineBanking.Entities;
using OnlineBanking.Security;

namespace OnlineBanking.Services;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly AuditWriter _audit;

    public AuthService(AppDbContext db, AuditWriter audit)
    {
        _db = db;
        _audit = audit;
    }

    // Registers a new customer (unique email + unique 5-digit account number)
    public async Task<Result<string>> RegisterAsync(string name, string email, string password, string city, string ip, string userAgent)
    {
        email = (email ?? "").Trim().ToLowerInvariant();
        name = (name ?? "").Trim();
        city = (city ?? "").Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return Result<string>.Fail("Invalid input.");

        if (await _db.Customers.AnyAsync(x => x.Email == email))
            return Result<string>.Fail("Email already exists.");

        // generate unique 5-digit account number
        string acc;
        int guard = 0;
        do
        {
            acc = Random.Shared.Next(10000, 100000).ToString();
            guard++;
            if (guard > 50) return Result<string>.Fail("Could not generate account number. Try again.");
        }
        while (await _db.Customers.AnyAsync(x => x.AccountNumber == acc));

        var customer = new Customer
        {
            Name = name,
            Email = email,
            City = city,
            AccountNumber = acc,
            PasswordHash = PasswordHasher.Hash(password),
            IsAdmin = false,
            Account = new Account { Balance = 0m }
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        await _audit.WriteAsync("REGISTER", acc, acc, ip, userAgent, $"New customer registered: {customer.Email}");

        return Result<string>.Ok(acc, "Registered successfully.");
    }

    // Validates login credentials + lockout policy + audit logs
    public async Task<Result<(Guid CustomerId, bool IsAdmin)>> ValidateLoginAsync(string accountNumber, string password, string ip, string userAgent)
    {
        accountNumber = (accountNumber ?? "").Trim();

        var customer = await _db.Customers.FirstOrDefaultAsync(x => x.AccountNumber == accountNumber);
        if (customer is null)
        {
            await _audit.WriteAsync("LOGIN_FAIL", "UNKNOWN", accountNumber, ip, userAgent, "Account not found.");
            return Result<(Guid, bool)>.Fail("Invalid account number or password.");
        }

        // If currently locked
        if (customer.LockedUntilUtc.HasValue && customer.LockedUntilUtc.Value > DateTimeOffset.UtcNow)
        {
            await _audit.WriteAsync("LOGIN_BLOCKED", customer.AccountNumber, customer.AccountNumber, ip, userAgent, "Account locked.");
            return Result<(Guid, bool)>.Fail("Account is locked. Try later.");
        }

        // Password check
        if (!PasswordHasher.Verify(password, customer.PasswordHash))
        {
            customer.FailedLoginCount += 1;

            // lock after 5 failed attempts for 15 minutes
            if (customer.FailedLoginCount >= 5)
            {
                customer.LockedUntilUtc = DateTimeOffset.UtcNow.AddMinutes(15);
                customer.FailedLoginCount = 0;
                await _db.SaveChangesAsync();

                await _audit.WriteAsync("LOGIN_LOCKED", customer.AccountNumber, customer.AccountNumber, ip, userAgent, "Locked due to failed attempts.");
                return Result<(Guid, bool)>.Fail("Too many failed attempts. Account locked for 15 minutes.");
            }

            await _db.SaveChangesAsync();
            await _audit.WriteAsync("LOGIN_FAIL", customer.AccountNumber, customer.AccountNumber, ip, userAgent, "Wrong password.");
            return Result<(Guid, bool)>.Fail("Invalid account number or password.");
        }

        // success: reset fail count
        customer.FailedLoginCount = 0;
        customer.LockedUntilUtc = null;
        await _db.SaveChangesAsync();

        await _audit.WriteAsync("LOGIN_SUCCESS", customer.AccountNumber, customer.AccountNumber, ip, userAgent, "Login ok.");

        return Result<(Guid, bool)>.Ok((customer.Id, customer.IsAdmin), "Login ok.");
    }
}
