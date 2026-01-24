namespace Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public int BranchCode { get; set; }
    public int CreatedBy { get; set; }
    public int UpdatedBy { get; set; }
    public int ApprovedBy { get; set; }
    public string TableName { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}