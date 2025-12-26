using OnlineBanking.Entities;

namespace OnlineBanking.Contracts;

public interface IStatementPdfService
{
    byte[] BuildMiniStatementPdf(string currency, string tzId, string accountNumber, string customerName, decimal currentBalance, IReadOnlyList<Transaction> last10);
}
