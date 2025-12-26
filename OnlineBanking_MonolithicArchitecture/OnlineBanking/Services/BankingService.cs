using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OnlineBanking.Common;
using OnlineBanking.Contracts;
using OnlineBanking.Data;
using OnlineBanking.Entities;
using OnlineBanking.Options;

namespace OnlineBanking.Services;

public sealed class BankingService : IBankingService
{
    private readonly AppDbContext _db;
    private readonly AuditWriter _audit;
    private readonly BankingRulesOptions _rules;

    public BankingService(AppDbContext db, AuditWriter audit, IOptions<BankingRulesOptions> rules)
    {
        _db = db;
        _audit = audit;
        _rules = rules.Value;
    }

    // Returns profile info for the logged-in user
    public async Task<(string CustomerName, string AccountNumber, decimal Balance)> GetProfileAsync(Guid customerId)
    {
        var row = await _db.Customers.AsNoTracking()
            .Where(c => c.Id == customerId)
            .Select(c => new { c.Name, c.AccountNumber, Balance = c.Account.Balance })
            .FirstAsync();

        return (row.Name, row.AccountNumber, row.Balance);
    }

    // Returns current balance
    public async Task<decimal> GetBalanceAsync(Guid customerId)
    {
        return await _db.Accounts.AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .Select(a => a.Balance)
            .FirstAsync();
    }

    // Adds money to account balance
    public async Task<Result> DepositAsync(Guid customerId, decimal amount, string ip, string userAgent)
    {
        if (amount <= 0) return Result.Fail("Amount must be greater than 0.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var customer = await _db.Customers.Include(c => c.Account).FirstAsync(c => c.Id == customerId);
        customer.Account.Balance += amount;

        _db.Transactions.Add(new Transaction
        {
            AccountId = customer.Account.Id,
            Type = TransactionType.Deposit,
            Amount = amount,
            BalanceAfter = customer.Account.Balance,
            Reference = "Cash deposit"
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        await _audit.WriteAsync("DEPOSIT", customer.AccountNumber, customer.AccountNumber, ip, userAgent, $"Deposit {amount:0.00}");

        return Result.Ok("Deposit successful.");
    }

    // Withdraws money while respecting minimum balance rule
    public async Task<Result> WithdrawAsync(Guid customerId, decimal amount, string ip, string userAgent)
    {
        if (amount <= 0) return Result.Fail("Amount must be greater than 0.");

        await using var tx = await _db.Database.BeginTransactionAsync();

        var customer = await _db.Customers.Include(c => c.Account).FirstAsync(c => c.Id == customerId);

        if (customer.Account.Balance < amount) return Result.Fail("Insufficient balance.");

        var newBalance = customer.Account.Balance - amount;
        if (newBalance < _rules.MinimumBalance)
            return Result.Fail($"Minimum balance rule: balance cannot go below {_rules.MinimumBalance:0.00}");

        customer.Account.Balance = newBalance;

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

        await _audit.WriteAsync("WITHDRAW", customer.AccountNumber, customer.AccountNumber, ip, userAgent, $"Withdraw {amount:0.00}");

        return Result.Ok("Withdraw successful.");
    }

    // Transfers money between accounts with daily limit + minimum balance rule
    public async Task<Result> TransferAsync(Guid customerId, string toAccountNumber, decimal amount, string ip, string userAgent)
    {
        if (amount <= 0) return Result.Fail("Amount must be greater than 0.");
        toAccountNumber = (toAccountNumber ?? "").Trim();

        await using var tx = await _db.Database.BeginTransactionAsync();

        var fromCustomer = await _db.Customers.Include(c => c.Account)
            .FirstAsync(c => c.Id == customerId);

        var toCustomer = await _db.Customers.Include(c => c.Account)
            .FirstOrDefaultAsync(c => c.AccountNumber == toAccountNumber);

        if (toCustomer is null) return Result.Fail("Receiver account not found.");
        if (toCustomer.Id == fromCustomer.Id) return Result.Fail("Cannot transfer to same account.");

        // daily transfer limit check (sum of TransferOut today UTC date)
        var todayUtc = DateTimeOffset.UtcNow.Date;
        var fromAccountId = fromCustomer.Account.Id;

        var todayTransferOut = await _db.Transactions.AsNoTracking()
            .Where(t => t.AccountId == fromAccountId
                        && t.Type == TransactionType.TransferOut
                        && t.CreatedAtUtc >= todayUtc
                        && t.CreatedAtUtc < todayUtc.AddDays(1))
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        if (todayTransferOut + amount > _rules.DailyTransferLimit)
            return Result.Fail($"Daily transfer limit exceeded. Limit: {_rules.DailyTransferLimit:0.00}");

        if (fromCustomer.Account.Balance < amount) return Result.Fail("Insufficient balance.");

        var newBalance = fromCustomer.Account.Balance - amount;
        if (newBalance < _rules.MinimumBalance)
            return Result.Fail($"Minimum balance rule: balance cannot go below {_rules.MinimumBalance:0.00}");

        // apply balances
        fromCustomer.Account.Balance = newBalance;
        toCustomer.Account.Balance += amount;

        _db.Transactions.Add(new Transaction
        {
            AccountId = fromCustomer.Account.Id,
            Type = TransactionType.TransferOut,
            Amount = amount,
            BalanceAfter = fromCustomer.Account.Balance,
            Reference = $"Transfer to {toCustomer.AccountNumber}"
        });

        _db.Transactions.Add(new Transaction
        {
            AccountId = toCustomer.Account.Id,
            Type = TransactionType.TransferIn,
            Amount = amount,
            BalanceAfter = toCustomer.Account.Balance,
            Reference = $"Transfer from {fromCustomer.AccountNumber}"
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        await _audit.WriteAsync("TRANSFER", fromCustomer.AccountNumber, toCustomer.AccountNumber, ip, userAgent, $"Transfer {amount:0.00} to {toCustomer.AccountNumber}");

        return Result.Ok("Transfer successful.");
    }

    // Returns last N transactions for dashboard + PDF
    public async Task<IReadOnlyList<Transaction>> GetLastTransactionsAsync(Guid customerId, int take = 10)
    {
        var accountId = await _db.Accounts
            .Where(a => a.CustomerId == customerId)
            .Select(a => a.Id)
            .FirstAsync();

        return await _db.Transactions.AsNoTracking()
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(take)
            .ToListAsync();
    }
}
