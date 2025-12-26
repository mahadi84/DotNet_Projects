using OnlineBanking.Common;
using OnlineBanking.Entities;

namespace OnlineBanking.Contracts;

public interface IAdminService
{
    Task<IReadOnlyList<Customer>> GetCustomersAsync();
    Task<Customer?> GetCustomerByAccountAsync(string accountNumber);

    Task<Result> SetAdminAsync(string accountNumber, bool isAdmin, string actorAcc, string ip, string ua);
    Task<Result> LockAsync(string accountNumber, int minutes, string actorAcc, string ip, string ua);
    Task<Result> UnlockAsync(string accountNumber, string actorAcc, string ip, string ua);
    Task<Result> ResetPasswordAsync(string accountNumber, string newPassword, string actorAcc, string ip, string ua);

    Task<IReadOnlyList<AuditLog>> GetLatestAuditAsync(int take = 100);
}
