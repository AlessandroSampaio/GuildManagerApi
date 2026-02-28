using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GuildManagerApi.Infrastructure.Repositories;

public class ReportRepository(AppDbContext context) : IReportRepository
{
    private readonly AppDbContext _context = context;

    public Task<bool> ExistsAsync(string reportId, CancellationToken ct = default)
        => _context.Reports.AnyAsync(r => r.Id.Equals(reportId), ct);

    public async Task<IEnumerable<Report>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
        => await _context.Reports
                .Include(r => r.Guild)
                .OrderByDescending(r => r.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);


    public async Task<Report?> GetByIdAsync(string reportId, CancellationToken ct = default)
        => await _context.Reports
                .Include(r => r.Guild)
                .Include(r => r.Fights)
                .FirstOrDefaultAsync(r => r.Id.Equals(reportId), ct);

    public async Task UpsertAsync(Report report, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(report.Id, ct);

        if (existing is null)
        {
            _context.Reports.Add(report);
        }
        else
        {
            existing.Title = report.Title;
            existing.StartTime = report.StartTime;
            existing.EndTime = report.EndTime;
            existing.GuildId = report.GuildId;
            existing.LastSyncedAt = report.LastSyncedAt;

            // Upsert fights
            foreach (var newFight in report.Fights)
            {
                var existingFight = existing.Fights
                    .FirstOrDefault(f => f.FightIndex == newFight.FightIndex);

                if (existingFight is null)
                    existing.Fights.Add(newFight);
                else
                {
                    existingFight.Name = newFight.Name;
                    existingFight.Kill = newFight.Kill;
                    existingFight.StartTimeMs = newFight.StartTimeMs;
                    existingFight.EndTimeMs = newFight.EndTimeMs;
                }
            }
        }

        await _context.SaveChangesAsync(ct);
    }
}
