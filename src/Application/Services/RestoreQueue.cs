using System.Runtime.CompilerServices;
using System.Threading.Channels;
using GuildManagerApi.Domain.Interfaces;

namespace GuildManagerApi.Application.Services;

public sealed class RestoreQueue : IRestoreQueue
{
    private readonly Channel<RestoreJobMessage> _channel =
        Channel.CreateBounded<RestoreJobMessage>(new BoundedChannelOptions(capacity: 10)
        {
            FullMode     = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = true
        });

    public ValueTask EnqueueAsync(RestoreJobMessage job, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(job, ct);

    public async IAsyncEnumerable<RestoreJobMessage> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(ct))
            yield return job;
    }
}
