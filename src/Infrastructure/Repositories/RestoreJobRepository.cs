using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Enums;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GuildManagerApi.Infrastructure.Repositories;

public class RestoreJobRepository(AppDbContext context) : IRestoreJobRepository
{
    private readonly AppDbContext _context = context;

    public Task<RestoreJob?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.RestoreJobs.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<bool> HasActiveJobAsync(CancellationToken ct = default)
        => _context.RestoreJobs.AnyAsync(
            r => r.Status == JobStatus.Queued || r.Status == JobStatus.Running, ct);

    public async Task CreateAsync(RestoreJob job, CancellationToken ct = default)
    {
        _context.RestoreJobs.Add(job);
        await _context.SaveChangesAsync(ct);
    }

    public async Task SetStatusAsync(
        Guid id, JobStatus status, string? errorMessage = null, DateTime? completedAt = null, CancellationToken ct = default)
    {
        await _context.RestoreJobs
            .Where(r => r.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, status)
                .SetProperty(r => r.ErrorMessage, errorMessage)
                .SetProperty(r => r.CompletedAt, completedAt),
                ct);
    }
}
