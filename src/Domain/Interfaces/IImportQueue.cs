namespace GuildManagerApi.Domain.Interfaces;

public record ImportJob(
    string ReportCode,
    Guid? UserId,
    string? Metric = "dps"
);

public record ImportProgressEvent(
    string ReportCode,
    ImportPhase Phase,
    string Message,
    object? Data = null
);

public enum ImportPhase
{
    Started = 0,
    FetchingReport = 1,
    SavingReport = 2,
    FetchingRankings = 3,
    SavingPerformance = 4,
    Completed = 5,
    Failed = 6
}

public interface IImportQueue
{
    ValueTask EnqueueAsync(ImportJob job, CancellationToken ct = default);
    IAsyncEnumerable<ImportJob> ReadAllAsync(CancellationToken ct = default);
}
