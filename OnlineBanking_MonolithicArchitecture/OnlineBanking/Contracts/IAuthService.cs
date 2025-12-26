using OnlineBanking.Common;

namespace OnlineBanking.Contracts;

public interface IAuthService
{
    Task<Result<string>> RegisterAsync(string name, string email, string password, string city, string ip, string userAgent);
    Task<Result<(Guid CustomerId, bool IsAdmin)>> ValidateLoginAsync(string accountNumber, string password, string ip, string userAgent);
}
