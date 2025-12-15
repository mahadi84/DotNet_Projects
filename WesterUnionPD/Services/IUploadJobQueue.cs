namespace WesterUnionPD.Services;

public interface IUploadJobQueue
{
    ValueTask EnqueueAsync(Guid jobId, string path, CancellationToken ct);
    ValueTask<(Guid jobId, string path)> DequeueAsync(CancellationToken ct);
}
