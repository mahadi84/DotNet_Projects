namespace WesterUnionPD.Models;

/// <summary>
/// One CSV upload job.
/// </summary>
public sealed class UploadJob
{
    public Guid Id { get; set; }
    public string Status { get; set; } = "Queued";   //"Queued"; লেখার মানে হলো এটি একটি Default Value।
    public string? Error { get; set; }               // ?=এতে কোনো ভ্যালু না-ও থাকতে পারে।

    public long ProcessedRows { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
