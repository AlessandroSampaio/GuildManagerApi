using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Enums;

namespace GuildManagerApi.Domain.Interfaces;

public interface IRestoreJobRepository
{
    Task<RestoreJob?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> HasActiveJobAsync(CancellationToken ct = default);
    Task CreateAsync(RestoreJob job, CancellationToken ct = default);
    Task SetStatusAsync(Guid id, JobStatus status, string? errorMessage = null, DateTime? completedAt = null, CancellationToken ct = default);
}
