using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GuildManagerApi.Infrastructure.Repositories;

public class PenaltyRepository(AppDbContext context) : IPenaltyRepository
{
    private readonly AppDbContext _context = context;

    // ── PenaltyEvent CRUD ─────────────────────────────────────────────────────

    public Task<PenaltyEvent?> GetEventByIdAsync(int id, CancellationToken ct = default)
        => _context.PenaltyEvents.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IEnumerable<PenaltyEvent>> GetAllEventsAsync(CancellationToken ct = default)
        => await _context.PenaltyEvents.OrderBy(e => e.Description).ToListAsync(ct);

    public async Task<int> CreateEventAsync(PenaltyEvent penaltyEvent, CancellationToken ct = default)
    {
        _context.PenaltyEvents.Add(penaltyEvent);
        await _context.SaveChangesAsync(ct);
        return penaltyEvent.Id;
    }

    public async Task<bool> UpdateEventAsync(int id, string description, int points, CancellationToken ct = default)
    {
        var rows = await _context.PenaltyEvents
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Description, description)
                .SetProperty(e => e.Points, points),
                ct);
        return rows > 0;
    }

    public async Task<bool> DeleteEventAsync(int id, CancellationToken ct = default)
    {
        var rows = await _context.PenaltyEvents
            .Where(e => e.Id == id)
            .ExecuteDeleteAsync(ct);
        return rows > 0;
    }

    // ── PlayerWeekPenalty ─────────────────────────────────────────────────────

    public async Task<IEnumerable<PlayerWeekPenalty>> GetPenaltiesByWeekAsync(int raidWeekId, CancellationToken ct = default)
        => await _context.PlayerWeekPenalties
            .Include(p => p.Player)
            .Include(p => p.PenaltyEvent)
            .Where(p => p.RaidWeekId == raidWeekId)
            .OrderBy(p => p.Player.Name)
            .ThenBy(p => p.CreatedAt)
            .ToListAsync(ct);

    public Task<PlayerWeekPenalty?> GetPenaltyByIdAsync(int id, CancellationToken ct = default)
        => _context.PlayerWeekPenalties
            .Include(p => p.Player)
            .Include(p => p.PenaltyEvent)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<int> AddPlayerPenaltyAsync(PlayerWeekPenalty penalty, CancellationToken ct = default)
    {
        _context.PlayerWeekPenalties.Add(penalty);
        await _context.SaveChangesAsync(ct);
        return penalty.Id;
    }

    public async Task<bool> RemovePlayerPenaltyAsync(int id, CancellationToken ct = default)
    {
        var rows = await _context.PlayerWeekPenalties
            .Where(p => p.Id == id)
            .ExecuteDeleteAsync(ct);
        return rows > 0;
    }
}
