namespace OnlineBanking.Entities;

public sealed class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;

    public decimal Balance { get; set; } = 0m;

    // Concurrency token
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public List<Transaction> Transactions { get; set; } = new();
}
