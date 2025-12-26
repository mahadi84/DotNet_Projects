using OnlineBanking.Common;
using OnlineBanking.Entities;

namespace OnlineBanking.Contracts;

public interface IBankingService
{
    Task<decimal> GetBalanceAsync(Guid customerId);
    Task<Result> DepositAsync(Guid customerId, decimal amount, string ip, string userAgent);
    Task<Result> WithdrawAsync(Guid customerId, decimal amount, string ip, string userAgent);
    Task<Result> TransferAsync(Guid customerId, string toAccountNumber, decimal amount, string ip, string userAgent);

    Task<(string CustomerName, string AccountNumber, decimal Balance)> GetProfileAsync(Guid customerId);

    Task<IReadOnlyList<Transaction>> GetLastTransactionsAsync(Guid customerId, int take = 10);
}
