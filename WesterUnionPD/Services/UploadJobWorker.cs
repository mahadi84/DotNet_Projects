using WesterUnionPD.Data;

namespace WesterUnionPD.Services;

/// <summary>
/// Background worker to process CSV without timeout.
/// </summary>
public sealed class UploadJobWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IUploadJobQueue _queue;

    public UploadJobWorker(IServiceScopeFactory scopeFactory, IUploadJobQueue queue)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var (jobId, path) = await _queue.DequeueAsync(stoppingToken);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var csv = scope.ServiceProvider.GetRequiredService<ICsvAggregationService>();

            var job = await db.UploadJobs.FindAsync(jobId);

            try
            {
                job!.Status = "Processing";
                await db.SaveChangesAsync(stoppingToken);

                await using var fs = File.OpenRead(path);
                var summaries = await csv.AggregateAsync(fs, job, db, stoppingToken);

                using var tx = await db.Database.BeginTransactionAsync(stoppingToken);

                db.BranchSummaries.RemoveRange(
                    db.BranchSummaries.Where(x => x.UploadJobId == jobId));

                await db.SaveChangesAsync(stoppingToken);

                db.BranchSummaries.AddRange(summaries);

                job.Status = "Completed";
                await db.SaveChangesAsync(stoppingToken);

                await tx.CommitAsync(stoppingToken);
            }
            catch
            {
                job!.Status = "Failed";
                job.Error = "CSV processing failed.";
                await db.SaveChangesAsync(stoppingToken);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
