namespace WesterUnionPD.Models;

/// <summary>
/// Security audit log (no sensitive data).
/// </summary>
public sealed class AuditLog
{
    public int Id { get; set; }
    public string Action { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
