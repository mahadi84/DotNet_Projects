using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineBanking.Application.Options
{
    public sealed class BankingRulesOptions
    {
        // কোন currency symbol ব্যবহার হবে
        // UI এবং PDF statement এ দেখানোর জন্য (৳, $, €)
        public string CurrencySymbol { get; set; } = "৳";

        // সিস্টেম কোন timezone follow করবে
        // transaction time ও statement time দেখাতে কাজে লাগে
        public string TimeZoneId { get; set; } = "Asia/Dhaka";

        // অ্যাকাউন্টে ন্যূনতম কত টাকা থাকতে হবে
        // এর নিচে নামলে withdraw / transfer ব্লক হবে
        public decimal MinimumBalance { get; set; } = 0m;

        // দৈনিক সর্বোচ্চ transfer limit
        // fraud ও risk control এর জন্য
        public decimal DailyTransferLimit { get; set; } = 0m;
    }

}
