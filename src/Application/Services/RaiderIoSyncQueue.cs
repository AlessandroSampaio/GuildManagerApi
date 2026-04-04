using System.Runtime.CompilerServices;
using System.Threading.Channels;
using GuildManagerApi.Domain.Interfaces;

namespace GuildManagerApi.Application.Services;

public sealed class RaiderIoSyncQueue : IRaiderIoSyncQueue
{
    private readonly Channel<RaiderIoSyncJob> _channel =
        Channel.CreateBounded<RaiderIoSyncJob>(new BoundedChannelOptions(capacity: 50)
        {
            FullMode     = BoundedChannelFullMode.DropWrite,
            SingleWriter = false,
            SingleReader = true
        });

    public ValueTask EnqueueAsync(RaiderIoSyncJob job, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(job, ct);

    public async IAsyncEnumerable<RaiderIoSyncJob> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(ct))
            yield return job;
    }
}
