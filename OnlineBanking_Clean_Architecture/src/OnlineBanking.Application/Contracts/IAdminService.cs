using OnlineBanking.Application.Common;
using OnlineBanking.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineBanking.Application.Contracts
{
    public interface IAdminService
    {
        // সব customer এর তালিকা
        Task<IReadOnlyList<Customer>> GetCustomersAsync();

        // account number দিয়ে নির্দিষ্ট customer খোঁজা
        Task<Customer?> GetCustomerByAccountAsync(string accountNumber);

        // কাউকে admin বানানো বা admin সরানো
        Task<Result> SetAdminAsync(
            string accountNumber,
            bool isAdmin,
            string actorAcc,
            string ip,
            string ua
        );

        // নির্দিষ্ট সময়ের জন্য account lock করা
        Task<Result> LockAsync(
            string accountNumber,
            int minutes,
            string actorAcc,
            string ip,
            string ua
        );

        // account unlock করা
        Task<Result> UnlockAsync(
            string accountNumber,
            string actorAcc,
            string ip,
            string ua
        );

        // admin দ্বারা password reset
        Task<Result> ResetPasswordAsync(
            string accountNumber,
            string newPassword,
            string actorAcc,
            string ip,
            string ua
        );

        // সাম্প্রতিক audit log দেখা
        Task<IReadOnlyList<AuditLog>> GetLatestAuditAsync(int take = 100);
    }


}
