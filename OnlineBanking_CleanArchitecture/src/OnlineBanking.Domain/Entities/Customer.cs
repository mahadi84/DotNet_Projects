using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace OnlineBanking.Domain.Entities
{
    public sealed class Customer
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string City { get; set; } = "";

        // 5-digit account number used for login
        public string AccountNumber { get; set; } = "";

        public string PasswordHash { get; set; } = "";

        // admin panel support
        public bool IsAdmin { get; set; } = false;

        // security: lock account after too many failed logins
        public int FailedLoginCount { get; set; } = 0;
        public DateTimeOffset? LockedUntilUtc { get; set; }

        public Account Account { get; set; } = new();
    }
}
