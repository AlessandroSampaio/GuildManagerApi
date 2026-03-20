namespace GuildManagerApi.Domain.Interfaces;

public record GuildSyncJob(int GuildId, Guid? UserId);

public record GuildSyncProgressEvent(
    int GuildId,
    GuildSyncPhase Phase,
    string Message,
    object? Data = null
);

public enum GuildSyncPhase
{
    Started           = 0,
    FetchingPage      = 1,
    SavingCharacters  = 2,
    Completed         = 3,
    Failed            = 4
}

public interface IGuildSyncQueue
{
    ValueTask EnqueueAsync(GuildSyncJob job, CancellationToken ct = default);
    IAsyncEnumerable<GuildSyncJob> ReadAllAsync(CancellationToken ct = default);
}
