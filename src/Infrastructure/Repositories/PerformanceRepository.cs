using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GuildManagerApi.Infrastructure.Repositories;

public class PerformanceRepository(AppDbContext context) : IPerformanceRepository
{
    private readonly AppDbContext _context = context;

    public async Task BulkUpsertAsync(IEnumerable<PerformanceEntry> entries, CancellationToken ct = default)
    {
        foreach (var entry in entries)
        {
            var existing = await _context.PerformanceEntries
                .FirstOrDefaultAsync(p => p.FightId == entry.FightId && p.CharacterId == entry.CharacterId, ct);

            if (existing is null)
                _context.PerformanceEntries.Add(entry);
            else
            {
                existing.Spec = entry.Spec;
                existing.Role = entry.Role;
                existing.Amount = entry.Amount;
                existing.RankPercent = entry.RankPercent;
                existing.TotalParses = entry.TotalParses;
                existing.BestPercent = entry.BestPercent;
            }
        }
        await _context.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<PerformanceEntry>> GetByCharacterAsync(int characterId, CancellationToken ct = default)
        => await _context.PerformanceEntries
            .Include(p => p.Fight)
            .ThenInclude(f => f.Report)
            .OrderByDescending(p => p.Fight.StartTimeMs)
            .Take(50)
            .ToListAsync(ct);

    public async Task<IEnumerable<PerformanceEntry>> GetByFightAsync(int fightId, CancellationToken ct = default)
        => await _context.PerformanceEntries
            .Include(p => p.Character)
            .Where(p => p.FightId == fightId)
            .OrderByDescending(p => p.Amount)
            .ToListAsync(ct);
}
