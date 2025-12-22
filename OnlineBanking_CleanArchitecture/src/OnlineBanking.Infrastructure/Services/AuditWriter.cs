using OnlineBanking.Domain.Entities;
using OnlineBanking.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineBanking.Infrastructure.Services
{
    public sealed class AuditWriter
    {
        // DbContext dependency -> audit log DB তে write করবে
        private readonly AppDbContext _db;

        // constructor injection
        public AuditWriter(AppDbContext db) => _db = db;

        // যে কোনো action ঘটলে audit log লিখে
        // action: LOGIN_SUCCESS, TRANSFER etc.
        // actorAcc: যিনি কাজ করেছেন
        // targetAcc: যার উপর কাজ হয়েছে (optional)
        // ip/ua: traceability
        // message: human readable details
        public async Task WriteAsync(string action, string actorAcc, string? targetAcc, string ip, string ua, string message)
        {
            // AuditLogs টেবিলে নতুন row add
            _db.AuditLogs.Add(new AuditLog
            {
                // কি action ঘটেছে
                Action = action,

                // কে করেছে
                ActorAccountNumber = actorAcc,

                // কার জন্য/কাকে target করা হয়েছে (optional)
                TargetAccountNumber = targetAcc,

                // কোন IP থেকে request
                Ip = ip,

                // কোন device/browser থেকে request
                UserAgent = ua,

                // details message
                Message = message
            });

            // DB তে persist করে
            await _db.SaveChangesAsync();
        }
    }
}
