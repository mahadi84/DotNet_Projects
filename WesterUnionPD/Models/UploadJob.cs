namespace WesterUnionPD.Models;

/// <summary>
/// One CSV upload job.
/// </summary>
public sealed class UploadJob
{
    public Guid Id { get; set; }
    public string Status { get; set; } = "Queued";
    public string? Error { get; set; }

    public long ProcessedRows { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
