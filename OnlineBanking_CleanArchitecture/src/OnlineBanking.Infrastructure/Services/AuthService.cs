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
    public sealed class AuthService : IAuthService
    {
        // DB access
        private readonly AppDbContext _db;

        // audit helper (log writing)
        private readonly AuditWriter _audit;

        // constructor injection
        public AuthService(AppDbContext db, AuditWriter audit)
        {
            _db = db;
            _audit = audit;
        }

        public async Task<Result<string>> RegisterAsync(string name, string email, string password, string city, string ip, string userAgent)
        {
            // email normalize: spaces remove + lower করা
            email = email.Trim().ToLowerInvariant();

            // একই email আগে থেকেই আছে কিনা check
            if (await _db.Customers.AnyAsync(x => x.Email == email))
                return Result<string>.Fail("Email already exists.");

            // generate unique 5-digit account number
            // guard রাখা হয়েছে যেন infinite loop না হয়
            string acc;
            int guard = 0;
            do
            {
                // 10000..99999 range -> 5 digit
                acc = Random.Shared.Next(10000, 100000).ToString();
                guard++;

                // 50 বার চেষ্টা করেও unique না পেলে fail
                if (guard > 50) return Result<string>.Fail("Could not generate account number. Try again.");
            }
            while (await _db.Customers.AnyAsync(x => x.AccountNumber == acc));

            // নতুন customer object তৈরি
            var customer = new Customer
            {
                // name trim করে রাখা
                Name = name.Trim(),

                // normalized email
                Email = email,

                // city trim করে রাখা
                City = city.Trim(),

                // generated unique account number
                AccountNumber = acc,

                // password hash করে save
                PasswordHash = PasswordHasher.Hash(password),

                // default admin false
                IsAdmin = false,

                // নতুন account create, balance=0
                Account = new Account { Balance = 0m }
            };

            // DB তে add
            _db.Customers.Add(customer);

            // persist
            await _db.SaveChangesAsync();

            // audit log write (REGISTER)
            await _audit.WriteAsync("REGISTER", acc, acc, ip, userAgent, $"New customer registered: {customer.Email}");

            // success response: account number ফেরত দিচ্ছে
            return Result<string>.Ok(acc, "Registered successfully.");
        }

        public async Task<Result<(Guid CustomerId, bool IsAdmin)>> ValidateLoginAsync(string accountNumber, string password, string ip, string userAgent)
        {
            // input normalize
            accountNumber = accountNumber.Trim();

            // accountNumber দিয়ে customer খোঁজা
            var customer = await _db.Customers.FirstOrDefaultAsync(x => x.AccountNumber == accountNumber);

            // account না পাওয়া গেলে audit + fail
            if (customer is null)
            {
                await _audit.WriteAsync("LOGIN_FAIL", "UNKNOWN", accountNumber, ip, userAgent, "Account not found.");
                return Result<(Guid, bool)>.Fail("Invalid account number or password.");
            }

            // locked কিনা check (lockedUntil এখনকার UTC এর পরে হলে blocked)
            if (customer.LockedUntilUtc.HasValue && customer.LockedUntilUtc.Value > DateTimeOffset.UtcNow)
            {
                await _audit.WriteAsync("LOGIN_BLOCKED", customer.AccountNumber, customer.AccountNumber, ip, userAgent, "Account locked.");
                return Result<(Guid, bool)>.Fail("Account is locked. Try later.");
            }

            // password verify (BCrypt)
            if (!PasswordHasher.Verify(password, customer.PasswordHash))
            {
                // fail counter বৃদ্ধি
                customer.FailedLoginCount += 1;

                // lock after 5 failed attempts for 15 minutes
                if (customer.FailedLoginCount >= 5)
                {
                    // lock set
                    customer.LockedUntilUtc = DateTimeOffset.UtcNow.AddMinutes(15);

                    // reset counter (lock হয়ে গেলে আবার 0)
                    customer.FailedLoginCount = 0;

                    await _db.SaveChangesAsync();

                    await _audit.WriteAsync("LOGIN_LOCKED", customer.AccountNumber, customer.AccountNumber, ip, userAgent, "Locked due to failed attempts.");
                    return Result<(Guid, bool)>.Fail("Too many failed attempts. Account locked for 15 minutes.");
                }

                // 5 এর কম হলে শুধু save + audit fail
                await _db.SaveChangesAsync();
                await _audit.WriteAsync("LOGIN_FAIL", customer.AccountNumber, customer.AccountNumber, ip, userAgent, "Wrong password.");
                return Result<(Guid, bool)>.Fail("Invalid account number or password.");
            }

            // success: reset fail count + unlock
            customer.FailedLoginCount = 0;
            customer.LockedUntilUtc = null;
            await _db.SaveChangesAsync();

            // audit success
            await _audit.WriteAsync("LOGIN_SUCCESS", customer.AccountNumber, customer.AccountNumber, ip, userAgent, "Login ok.");

            // return customerId + role info
            return Result<(Guid, bool)>.Ok((customer.Id, customer.IsAdmin), "Login ok.");
        }
    }
}
