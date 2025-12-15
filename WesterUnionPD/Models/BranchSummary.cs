namespace WesterUnionPD.Models;

/// <summary>
/// Aggregated result per ABD branch.
/// </summary>
public sealed class BranchSummary
{
    public int Id { get; set; }
    public Guid UploadJobId { get; set; }

    public string AbdCode { get; set; } = "";
    public decimal ChargesLOC { get; set; }
    public decimal FxLOC { get; set; }

    public decimal GrandTotal => ChargesLOC + FxLOC;
}
