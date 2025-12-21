using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineBanking.Domain.Entities
{
    public sealed class AuditLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public string Action { get; set; } = "";          // e.g. LOGIN_SUCCESS, TRANSFER, ADMIN_RESET_PASSWORD
        public string ActorAccountNumber { get; set; } = ""; // who did it (accountNumber) or "SYSTEM"
        public string? TargetAccountNumber { get; set; }     // optional
        public string? Ip { get; set; }
        public string? UserAgent { get; set; }

        public string Message { get; set; } = "";
    }
}
