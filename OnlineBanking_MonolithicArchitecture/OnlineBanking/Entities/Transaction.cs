namespace OnlineBanking.Entities;

public enum TransactionType
{
    Deposit = 1,
    Withdraw = 2,
    TransferOut = 3,
    TransferIn = 4
}

public sealed class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AccountId { get; set; }
    public Account Account { get; set; } = default!;

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public TransactionType Type { get; set; }

    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }

    public string? Reference { get; set; }
}
