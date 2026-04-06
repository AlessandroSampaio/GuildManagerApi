using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GuildManagerApi.Infrastructure.Repositories;

public class RaidWeekRepository(AppDbContext context) : IRaidWeekRepository
{
    private readonly AppDbContext _context = context;

    public async Task<bool> AddReportAsync(int raidWeekId, string reportCode, CancellationToken ct = default)
    {
        var weekExists = await _context.RaidWeeks.AnyAsync(w => w.Id == raidWeekId, ct);
        if (!weekExists) return false;

        var alreadyLinked = await _context.RaidWeekReports.AnyAsync(
            r => r.RaidWeekId == raidWeekId && r.ReportCode == reportCode, ct);
        if (alreadyLinked) return false;

        _context.RaidWeekReports.Add(new RaidWeekReport
        {
            RaidWeekId = raidWeekId,
            ReportCode = reportCode,
            AddedAt = DateTime.UtcNow,
        });

        await _context.SaveChangesAsync(ct);
        return true;
    }

    public Task<int> CountAsync(CancellationToken ct = default)
        => _context.RaidWeeks.CountAsync(ct);

    public async Task<int> CreateAsync(RaidWeek raidWeek, CancellationToken ct = default)
    {
        _context.RaidWeeks.Add(raidWeek);
        await _context.SaveChangesAsync(ct);
        return raidWeek.Id;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var rows = await _context.RaidWeeks
                    .Where(w => w.Id == id)
                    .ExecuteDeleteAsync(ct);
        return rows > 0;
    }

    public async Task<IEnumerable<RaidWeek>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
        => await _context.RaidWeeks
                     .Include(w => w.ReportEntries)
                     .Include(w => w.Core)
                     .OrderByDescending(w => w.StartsAt)
                     .Skip((page - 1) * pageSize)
                     .Take(pageSize)
                     .ToListAsync(ct);


    public Task<RaidWeek?> GetByDateAsync(DateTime date, CancellationToken ct = default)
    {
        var tuesday = TuesdayOf(date);
        return _context.RaidWeeks
                  .Include(w => w.ReportEntries)
                  .Include(w => w.Core)
                  .FirstOrDefaultAsync(w => w.StartsAt == tuesday, ct);
    }

    public Task<RaidWeek?> GetByIdAsync(int id, CancellationToken ct = default)
        => _context.RaidWeeks
                 .Include(w => w.ReportEntries)
                 .Include(w => w.Core)
                 .FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<bool> RemoveReportAsync(int raidWeekId, string reportCode, CancellationToken ct = default)
    {
        var rows = await _context.RaidWeekReports
                  .Where(r => r.RaidWeekId == raidWeekId && r.ReportCode == reportCode)
                  .ExecuteDeleteAsync(ct);
        return rows > 0;
    }

    public async Task<bool> UpdateAsync(int id, string label, DateTime startsAt, CancellationToken ct = default)
    {
        var rows = await _context.RaidWeeks
                    .Where(w => w.Id == id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(w => w.Label, label)
                        .SetProperty(w => w.StartsAt, startsAt)
                        .SetProperty(w => w.UpdatedAt, DateTime.UtcNow),
                        ct);
        return rows > 0;
    }

    //Helpers
    public static DateTime TuesdayOf(DateTime date)
    {
        // DayOfWeek: Sun=0, Mon=1, Tue=2, Wed=3, Thu=4, Fri=5, Sat=6
        // Offset para trás até a terça mais próxima:
        int daysBack = ((int)date.DayOfWeek - (int)DayOfWeek.Tuesday + 7) % 7;
        return date.Date.AddDays(-daysBack);
    }
}
