using OnlineBanking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineBanking.Application.Contracts
{
    public interface IStatementPdfService
    {
        // Mini statement PDF তৈরি করে
        // byte[] ফেরত দেয় যাতে API থেকে download করা যায়
        byte[] BuildMiniStatementPdf(
            string currency,
            string tzId,
            string accountNumber,
            string customerName,
            decimal currentBalance,
            IReadOnlyList<Transaction> last10
        );
    }

}
