using System.Runtime.CompilerServices;
using System.Threading.Channels;
using GuildManagerApi.Domain.Interfaces;

namespace GuildManagerApi.Application.Services;

public sealed class BackupQueue : IBackupQueue
{
    private readonly Channel<BackupJobMessage> _channel =
        Channel.CreateBounded<BackupJobMessage>(new BoundedChannelOptions(capacity: 10)
        {
            FullMode     = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = true
        });

    public ValueTask EnqueueAsync(BackupJobMessage job, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(job, ct);

    public async IAsyncEnumerable<BackupJobMessage> ReadAllAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var job in _channel.Reader.ReadAllAsync(ct))
            yield return job;
    }
}
