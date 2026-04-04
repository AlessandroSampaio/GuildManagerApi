namespace GuildManagerApi.Domain.Interfaces;

public record RaiderIoSyncJob(int CoreId, Guid? UserId);

public record RaiderIoSyncProgressEvent(
    int CoreId,
    RaiderIoSyncPhase Phase,
    string Message,
    object? Data = null
);

public enum RaiderIoSyncPhase
{
    Started   = 0,
    Syncing   = 1,
    Completed = 2,
    Failed    = 3
}

public interface IRaiderIoSyncQueue
{
    ValueTask EnqueueAsync(RaiderIoSyncJob job, CancellationToken ct = default);
    IAsyncEnumerable<RaiderIoSyncJob> ReadAllAsync(CancellationToken ct = default);
}
