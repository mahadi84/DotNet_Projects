namespace OnlineBanking.Options;

public sealed class BankingRulesOptions
{
    public string CurrencySymbol { get; set; } = "৳";
    public string TimeZoneId { get; set; } = "Asia/Dhaka";
    public decimal MinimumBalance { get; set; } = 0m;
    public decimal DailyTransferLimit { get; set; } = 0m;
}
