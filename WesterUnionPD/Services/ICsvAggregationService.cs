using WesterUnionPD.Models;
using WesterUnionPD.Data;

namespace WesterUnionPD.Services;

public interface ICsvAggregationService
{
    Task<List<BranchSummary>> AggregateAsync(
        Stream csvStream,
        UploadJob job,
        AppDbContext db,
        CancellationToken ct);
}
