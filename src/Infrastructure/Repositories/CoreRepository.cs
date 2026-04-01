using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GuildManagerApi.Infrastructure.Repositories;

public class CoreRepository(AppDbContext context) : ICoreRepository
{
    private readonly AppDbContext _context = context;

    public Task<Core?> GetByIdAsync(int id, CancellationToken ct = default)
        => _context.Cores
            .Include(c => c.Guild)
            .Include(c => c.Players)
                .ThenInclude(cp => cp.Player)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IEnumerable<Core>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
        => await _context.Cores
            .Include(c => c.Guild)
            .Include(c => c.Players)
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public Task<int> CountAsync(CancellationToken ct = default)
        => _context.Cores.CountAsync(ct);

    public async Task<int> CreateAsync(Core core, CancellationToken ct = default)
    {
        _context.Cores.Add(core);
        await _context.SaveChangesAsync(ct);
        return core.Id;
    }

    public async Task<bool> UpdateAsync(int id, string name, CancellationToken ct = default)
    {
        var rows = await _context.Cores
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(c => c.Name, name)
                .SetProperty(c => c.UpdatedAt, DateTime.UtcNow),
                ct);
        return rows > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var rows = await _context.Cores
            .Where(c => c.Id == id)
            .ExecuteDeleteAsync(ct);
        return rows > 0;
    }

    public async Task<bool> AddPlayerAsync(int coreId, int playerId, CancellationToken ct = default)
    {
        var core = await _context.Cores.FindAsync([coreId], ct);
        var player = await _context.Players.FindAsync([playerId], ct);
        if (core is null || player is null) return false;

        var alreadyExists = await _context.CorePlayers
            .AnyAsync(cp => cp.CoreId == coreId && cp.PlayerId == playerId, ct);
        if (alreadyExists) return true;

        _context.CorePlayers.Add(new CorePlayer { CoreId = coreId, PlayerId = playerId });
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RemovePlayerAsync(int coreId, int playerId, CancellationToken ct = default)
    {
        var rows = await _context.CorePlayers
            .Where(cp => cp.CoreId == coreId && cp.PlayerId == playerId)
            .ExecuteDeleteAsync(ct);
        return rows > 0;
    }
}
