using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GuildManagerApi.Infrastructure.Repositories;

public class PlayerRepository(AppDbContext context) : IPlayerRepository
{
    private readonly AppDbContext _context = context;

    public async Task<bool> AddCharacterAsync(int playerId, int characterId, CancellationToken ct = default)
    {
        var player = await _context.Players.FindAsync([playerId], ct);
        var charater = await _context.Characters.FindAsync([characterId], ct);
        if (player == null || charater == null)
            return false;

        player.Characters.Add(charater);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public Task<int> CountAsync(CancellationToken ct = default)
        => _context.Players.CountAsync(ct);

    public async Task<int> CreateAsync(Player player, CancellationToken ct = default)
    {
        _context.Players.Add(player);
        await _context.SaveChangesAsync(ct);
        return player.Id;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var rows = await _context.Players
              .Where(p => p.Id == id)
              .ExecuteDeleteAsync(ct);
        return rows > 0;
    }

    public async Task<IEnumerable<Player>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
        => await _context.Players
                    .Include(p => p.Characters)
                    .OrderBy(p => p.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

    public Task<Player?> GetByIdAsync(int id, CancellationToken ct = default)
        => _context.Players
                   .Include(p => p.Characters)
                       .ThenInclude(c => c.Guild)
                   .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<bool> RemoveCharacterAsync(int playerId, int characterId, CancellationToken ct = default)
    {
        var character = await _context.Characters.FindAsync([characterId], ct);

        if (character is null || character.PlayerId != playerId) return false;

        character.PlayerId = null;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpdateAsync(int id, string name, CancellationToken ct = default)
    {
        var rows = await _context.Players
                    .Where(p => p.Id == id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.Name, name)
                        .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                        ct);
        return rows > 0;
    }

}
