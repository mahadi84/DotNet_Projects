using OnlineBanking.Application.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineBanking.Application.Contracts
{
    public interface IAuthService
    {
        // নতুন customer register করে
        // name, email, password, city থেকে account তৈরি হয়
        // ip ও userAgent audit log এর জন্য ব্যবহার হয়
        Task<Result<string>> RegisterAsync(
            string name,
            string email,
            string password,
            string city,
            string ip,
            string userAgent
        );

        // login validate করে
        // accountNumber ও password মিললে customerId ফেরত দেয়
        // isAdmin দিয়ে role নির্ধারণ করা যায়
        Task<Result<(Guid CustomerId, bool IsAdmin)>> ValidateLoginAsync(
            string accountNumber,
            string password,
            string ip,
            string userAgent
        );
    }
}
