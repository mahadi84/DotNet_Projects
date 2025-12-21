using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineBanking.Application.Common
{
    // সাধারণ success / failure result
    public sealed record Result(bool Success, string Message)
    {
        // অপারেশন সফল হলে ব্যবহার করা হয়
        public static Result Ok(string msg = "OK")
            => new(true, msg);

        // অপারেশন ব্যর্থ হলে ব্যবহার করা হয়
        public static Result Fail(string msg)
            => new(false, msg);
    }



    // data সহ result (generic version)
    public sealed record Result<T>(bool Success, string Message, T? Data)
    {
        // সফল হলে data সহ result ফেরত দেয়
        public static Result<T> Ok(T data, string msg = "OK")
            => new(true, msg, data);

        // ব্যর্থ হলে data = null থাকবে
        public static Result<T> Fail(string msg)
            => new(false, msg, default);
    }

}
