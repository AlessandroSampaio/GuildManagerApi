using System.Runtime.CompilerServices;
using System.Threading.Channels;
using GuildManagerApi.Domain.Interfaces;

namespace GuildManagerApi.Application.Services;

public sealed class ImportQueue : IImportQueue
{
    private readonly Channel<ImportJob> _channel =
           Channel.CreateBounded<ImportJob>(new BoundedChannelOptions(capacity: 100)
           {
               FullMode = BoundedChannelFullMode.Wait,
               SingleWriter = false,
               SingleReader = true
           });


    public ValueTask EnqueueAsync(ImportJob job, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(job, ct);

    public async IAsyncEnumerable<ImportJob> ReadAllAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(ct))
            yield return job;
    }
}
