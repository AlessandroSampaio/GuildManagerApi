namespace GuildManagerApi.Domain.Interfaces;

public record RestoreJobMessage(
    Guid RestoreId,
    Guid? SourceBackupId,
    string? UploadedFilePath,
    Guid? UserId
);

public record RestoreProgressEvent(
    Guid RestoreId,
    RestorePhase Phase,
    string Message,
    object? Data = null
);

public enum RestorePhase
{
    Started = 0,
    Validating = 1,
    Restoring = 2,
    Finalizing = 3,
    Completed = 4,
    Failed = 5
}

public interface IRestoreQueue
{
    ValueTask EnqueueAsync(RestoreJobMessage job, CancellationToken ct = default);
    IAsyncEnumerable<RestoreJobMessage> ReadAllAsync(CancellationToken ct = default);
}
