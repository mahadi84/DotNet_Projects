using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineBanking.Application.Common;
using OnlineBanking.Application.Contracts;
using OnlineBanking.Application.Options;
using OnlineBanking.Domain.Entities;
using OnlineBanking.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineBanking.Infrastructure.Services
{
    public sealed class BankingService : IBankingService
    {
        // DB access
        private readonly AppDbContext _db;

        // audit writer
        private readonly AuditWriter _audit;

        // business rules (from config/appsettings)
        private readonly BankingRulesOptions _rules;

        public BankingService(AppDbContext db, AuditWriter audit, IOptions<BankingRulesOptions> rules)
        {
            _db = db;
            _audit = audit;
            _rules = rules.Value; // Options pattern থেকে actual rules object পাওয়া
        }

        public async Task<(string CustomerName, string AccountNumber, decimal Balance)> GetProfileAsync(Guid customerId)
        {
            // AsNoTracking -> শুধু read, change tracking off, faster
            // Select -> শুধু প্রয়োজনীয় field আনা
            var row = await _db.Customers.AsNoTracking()
                .Where(c => c.Id == customerId)
                .Select(c => new { c.Name, c.AccountNumber, Balance = c.Account.Balance })
                .FirstAsync();

            // tuple return -> UI friendly
            return (row.Name, row.AccountNumber, row.Balance);
        }

        public async Task<decimal> GetBalanceAsync(Guid customerId)
        {
            // account table থেকে balance read
            return await _db.Accounts.AsNoTracking()
                .Where(a => a.CustomerId == customerId)
                .Select(a => a.Balance)
                .FirstAsync();
        }

        public async Task<Result> DepositAsync(Guid customerId, decimal amount, string ip, string userAgent)
        {
            // basic validation
            if (amount <= 0) return Result.Fail("Amount must be greater than 0.");

            // DB transaction start
            // deposit + transaction entry atomic রাখতে
            await using var tx = await _db.Database.BeginTransactionAsync();

            // customer + account include করে load
            var customer = await _db.Customers.Include(c => c.Account).FirstAsync(c => c.Id == customerId);

            // balance increase
            customer.Account.Balance += amount;

            // transaction table এ record add
            _db.Transactions.Add(new Transaction
            {
                AccountId = customer.Account.Id,           // কোন account
                Type = TransactionType.Deposit,            // deposit type
                Amount = amount,                           // কত টাকা
                BalanceAfter = customer.Account.Balance,   // নতুন balance
                Reference = "Cash deposit"                 // reference text
            });

            // persist
            await _db.SaveChangesAsync();

            // commit transaction
            await tx.CommitAsync();

            // audit log
            await _audit.WriteAsync("DEPOSIT", customer.AccountNumber, customer.AccountNumber, ip, userAgent, $"Deposit {amount:0.00}");

            return Result.Ok("Deposit successful.");
        }

        public async Task<Result> WithdrawAsync(Guid customerId, decimal amount, string ip, string userAgent)
        {
            // basic validation
            if (amount <= 0) return Result.Fail("Amount must be greater than 0.");

            // DB transaction start (withdraw + transaction record atomic)
            await using var tx = await _db.Database.BeginTransactionAsync();

            var customer = await _db.Customers.Include(c => c.Account).FirstAsync(c => c.Id == customerId);

            // যথেষ্ট balance আছে কিনা
            if (customer.Account.Balance < amount) return Result.Fail("Insufficient balance.");

            // withdraw এর পর balance কত হবে
            var newBalance = customer.Account.Balance - amount;

            // minimum balance rule enforce
            if (newBalance < _rules.MinimumBalance)
                return Result.Fail($"Minimum balance rule: balance cannot go below {_rules.MinimumBalance:0.00}");

            // apply new balance
            customer.Account.Balance = newBalance;

            // transaction record
            _db.Transactions.Add(new Transaction
            {
                AccountId = customer.Account.Id,
                Type = TransactionType.Withdraw,
                Amount = amount,
                BalanceAfter = customer.Account.Balance,
                Reference = "Cash withdraw"
            });

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            // audit write
            await _audit.WriteAsync("WITHDRAW", customer.AccountNumber, customer.AccountNumber, ip, userAgent, $"Withdraw {amount:0.00}");

            return Result.Ok("Withdraw successful.");
        }

        public async Task<Result> TransferAsync(Guid customerId, string toAccountNumber, decimal amount, string ip, string userAgent)
        {
            // basic validation
            if (amount <= 0) return Result.Fail("Amount must be greater than 0.");

            // normalize receiver account number
            toAccountNumber = toAccountNumber.Trim();

            // DB transaction start (2 accounts update + 2 tx rows atomic)
            await using var tx = await _db.Database.BeginTransactionAsync();

            // sender customer + account
            var fromCustomer = await _db.Customers.Include(c => c.Account)
                .FirstAsync(c => c.Id == customerId);

            // receiver customer + account
            var toCustomer = await _db.Customers.Include(c => c.Account)
                .FirstOrDefaultAsync(c => c.AccountNumber == toAccountNumber);

            // receiver না থাকলে fail
            if (toCustomer is null) return Result.Fail("Receiver account not found.");

            // নিজের account এ transfer করা যাবে না
            if (toCustomer.Id == fromCustomer.Id) return Result.Fail("Cannot transfer to same account.");

            // daily transfer limit check (sum of TransferOut today UTC date)
            var todayUtc = DateTimeOffset.UtcNow.Date; // আজকের UTC day start
            var fromAccountId = fromCustomer.Account.Id;

            // আজকের total transfer out কত হয়েছে
            var todayTransferOut = await _db.Transactions.AsNoTracking()
                .Where(t => t.AccountId == fromAccountId
                            && t.Type == TransactionType.TransferOut
                            && t.CreatedAtUtc >= todayUtc
                            && t.CreatedAtUtc < todayUtc.AddDays(1))
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;

            // limit exceed করলে fail
            if (todayTransferOut + amount > _rules.DailyTransferLimit)
                return Result.Fail($"Daily transfer limit exceeded. Limit: {_rules.DailyTransferLimit:0.00}");

            // sender এর balance পর্যাপ্ত কিনা
            if (fromCustomer.Account.Balance < amount) return Result.Fail("Insufficient balance.");

            // sender এর নতুন balance
            var newBalance = fromCustomer.Account.Balance - amount;

            // minimum balance rule
            if (newBalance < _rules.MinimumBalance)
                return Result.Fail($"Minimum balance rule: balance cannot go below {_rules.MinimumBalance:0.00}");

            // apply balances
            fromCustomer.Account.Balance = newBalance; // sender কমে
            toCustomer.Account.Balance += amount;      // receiver বাড়ে

            // sender side transaction (TransferOut)
            _db.Transactions.Add(new Transaction
            {
                AccountId = fromCustomer.Account.Id,
                Type = TransactionType.TransferOut,
                Amount = amount,
                BalanceAfter = fromCustomer.Account.Balance,
                Reference = $"Transfer to {toCustomer.AccountNumber}"
            });

            // receiver side transaction (TransferIn)
            _db.Transactions.Add(new Transaction
            {
                AccountId = toCustomer.Account.Id,
                Type = TransactionType.TransferIn,
                Amount = amount,
                BalanceAfter = toCustomer.Account.Balance,
                Reference = $"Transfer from {fromCustomer.AccountNumber}"
            });

            // persist
            await _db.SaveChangesAsync();

            // commit
            await tx.CommitAsync();

            // audit log
            await _audit.WriteAsync("TRANSFER", fromCustomer.AccountNumber, toCustomer.AccountNumber, ip, userAgent, $"Transfer {amount:0.00} to {toCustomer.AccountNumber}");

            return Result.Ok("Transfer successful.");
        }

        public async Task<IReadOnlyList<Transaction>> GetLastTransactionsAsync(Guid customerId, int take = 10)
        {
            // প্রথমে customerId থেকে accountId বের করি
            var accountId = await _db.Accounts
                .Where(a => a.CustomerId == customerId)
                .Select(a => a.Id)
                .FirstAsync();

            // তারপর transaction টেবিল থেকে latest take সংখ্যক record
            return await _db.Transactions.AsNoTracking()
                .Where(t => t.AccountId == accountId)
                .OrderByDescending(t => t.CreatedAtUtc)
                .Take(take)
                .ToListAsync();
        }
    }
}
