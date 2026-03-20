using System.Runtime.CompilerServices;
using System.Threading.Channels;
using GuildManagerApi.Domain.Interfaces;

namespace GuildManagerApi.Application.Services;

public sealed class GuildSyncQueue : IGuildSyncQueue
{
    private readonly Channel<GuildSyncJob> _channel =
        Channel.CreateBounded<GuildSyncJob>(new BoundedChannelOptions(capacity: 50)
        {
            FullMode     = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = true
        });

    public ValueTask EnqueueAsync(GuildSyncJob job, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(job, ct);

    public async IAsyncEnumerable<GuildSyncJob> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(ct))
            yield return job;
    }
}
