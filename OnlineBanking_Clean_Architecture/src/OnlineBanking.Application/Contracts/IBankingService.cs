using OnlineBanking.Application.Common;
using OnlineBanking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineBanking.Application.Contracts
{
    public interface IBankingService
    {
        // নির্দিষ্ট customer এর বর্তমান balance দেখায়
        Task<decimal> GetBalanceAsync(Guid customerId);

        // টাকা জমা (Deposit)
        Task<Result> DepositAsync(
            Guid customerId,
            decimal amount,
            string ip,
            string userAgent
        );

        // টাকা তোলা (Withdraw)
        // minimum balance rule এখানে প্রয়োগ হবে
        Task<Result> WithdrawAsync(
            Guid customerId,
            decimal amount,
            string ip,
            string userAgent
        );

        // টাকা transfer করা
        // daily transfer limit + balance check হবে
        Task<Result> TransferAsync(
            Guid customerId,
            string toAccountNumber,
            decimal amount,
            string ip,
            string userAgent
        );

        // Dashboard profile info
        Task<(string CustomerName, string AccountNumber, decimal Balance)>
            GetProfileAsync(Guid customerId);

        // সর্বশেষ কিছু transaction
        // default = last 10
        Task<IReadOnlyList<Transaction>>
            GetLastTransactionsAsync(Guid customerId, int take = 10);
    }

}
