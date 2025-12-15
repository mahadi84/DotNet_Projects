using System.Threading.Channels;

namespace WesterUnionPD.Services;

public sealed class UploadJobQueue : IUploadJobQueue
{
    private readonly Channel<(Guid, string)> _ch =
        Channel.CreateUnbounded<(Guid, string)>();

    public ValueTask EnqueueAsync(Guid jobId, string path, CancellationToken ct)
        => _ch.Writer.WriteAsync((jobId, path), ct);

    public ValueTask<(Guid jobId, string path)> DequeueAsync(CancellationToken ct)
        => _ch.Reader.ReadAsync(ct);
}
