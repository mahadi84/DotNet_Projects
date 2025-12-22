using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineBanking.Infrastructure.Security
{
    public static class PasswordHasher
    {
        // plain password কে BCrypt দিয়ে hash করে (secure)
        // DB তে কখনো plain password save করা হয় না
        public static string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

        // user input password কে stored hash এর সাথে মিলিয়ে দেখে
        // match হলে true, না হলে false
        public static bool Verify(string password, string hash) => BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
