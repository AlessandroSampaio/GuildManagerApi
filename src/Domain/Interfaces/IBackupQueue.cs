namespace GuildManagerApi.Domain.Interfaces;

public record BackupJobMessage(
    Guid BackupId,
    Guid? UserId
);

public record BackupProgressEvent(
    Guid BackupId,
    BackupPhase Phase,
    string Message,
    object? Data = null
);

public enum BackupPhase
{
    Started = 0,
    Dumping = 1,
    Finalizing = 2,
    Completed = 3,
    Failed = 4
}

public interface IBackupQueue
{
    ValueTask EnqueueAsync(BackupJobMessage job, CancellationToken ct = default);
    IAsyncEnumerable<BackupJobMessage> ReadAllAsync(CancellationToken ct = default);
}
