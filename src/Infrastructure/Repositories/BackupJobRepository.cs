using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Enums;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GuildManagerApi.Infrastructure.Repositories;

public class BackupJobRepository(AppDbContext context) : IBackupJobRepository
{
    private readonly AppDbContext _context = context;

    public Task<BackupJob?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _context.BackupJobs.FirstOrDefaultAsync(b => b.Id == id, ct);

    public async Task<(IReadOnlyList<BackupJob> Items, int Total)> GetPagedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.BackupJobs.AsNoTracking().OrderByDescending(b => b.CreatedAt);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task<bool> HasActiveJobAsync(CancellationToken ct = default)
        => _context.BackupJobs.AnyAsync(
            b => b.Status == JobStatus.Queued || b.Status == JobStatus.Running, ct);

    public async Task CreateAsync(BackupJob job, CancellationToken ct = default)
    {
        _context.BackupJobs.Add(job);
        await _context.SaveChangesAsync(ct);
    }

    public async Task SetStatusAsync(
        Guid id, JobStatus status, string? errorMessage = null, DateTime? completedAt = null, CancellationToken ct = default)
    {
        await _context.BackupJobs
            .Where(b => b.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Status, status)
                .SetProperty(b => b.ErrorMessage, errorMessage)
                .SetProperty(b => b.CompletedAt, completedAt),
                ct);
    }

    public async Task SetSizeAsync(Guid id, long sizeBytes, CancellationToken ct = default)
    {
        await _context.BackupJobs
            .Where(b => b.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.SizeBytes, sizeBytes), ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        => await _context.BackupJobs.Where(b => b.Id == id).ExecuteDeleteAsync(ct) > 0;
}
