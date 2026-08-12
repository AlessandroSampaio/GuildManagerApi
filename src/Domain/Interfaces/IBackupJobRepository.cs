using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Enums;

namespace GuildManagerApi.Domain.Interfaces;

public interface IBackupJobRepository
{
    Task<BackupJob?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IReadOnlyList<BackupJob> Items, int Total)> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<bool> HasActiveJobAsync(CancellationToken ct = default);
    Task CreateAsync(BackupJob job, CancellationToken ct = default);
    Task SetStatusAsync(Guid id, JobStatus status, string? errorMessage = null, DateTime? completedAt = null, CancellationToken ct = default);
    Task SetSizeAsync(Guid id, long sizeBytes, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
