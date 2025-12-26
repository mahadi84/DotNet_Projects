using OnlineBanking.Data;
using OnlineBanking.Entities;

namespace OnlineBanking.Services;

public sealed class AuditWriter
{
    private readonly AppDbContext _db;
    public AuditWriter(AppDbContext db) => _db = db;

    // Writes an audit trail entry for security + traceability
    public async Task WriteAsync(string action, string actorAcc, string? targetAcc, string ip, string ua, string message)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            ActorAccountNumber = actorAcc,
            TargetAccountNumber = targetAcc,
            Ip = ip,
            UserAgent = ua,
            Message = message
        });
        await _db.SaveChangesAsync();
    }
}
