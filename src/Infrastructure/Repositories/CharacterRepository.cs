using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GuildManagerApi.Infrastructure.Repositories;


public class CharacterRepository(AppDbContext context) : ICharacterRepository
{
    private readonly AppDbContext _context = context;

    public Task<Character?> FindByWclActorAsync(long wclActorId, string server, CancellationToken ct = default)
        => _context.Characters.FirstOrDefaultAsync(c => c.WclActorId == wclActorId && c.Server.Equals(server), ct);

    public async Task<IEnumerable<Character>> FindByWclActorIdsAsync(IEnumerable<long> wclActorIds, CancellationToken ct = default)
    {
        var ids = wclActorIds.ToList();
        return await _context.Characters
            .Where(c => ids.Contains(c.WclActorId))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Character>> GetByGuildAsync(int guildId, CancellationToken ct = default)
        => await _context.Characters
                    .Include(c => c.Class)
                    .Include(c => c.Player)
                    .Where(c => c.GuildId == guildId)
                    .ToListAsync(ct);

    public async Task<IEnumerable<Class>> GetClassesAsync(CancellationToken ct = default)
        => await _context.Classes.ToListAsync(ct);

    public Task<Character?> GetByIdAsync(int id, CancellationToken ct = default)
        => _context.Characters.FirstOrDefaultAsync(c => c.Id == id, ct);



    public async Task<int> UpsertAsync(Character character, CancellationToken ct = default)
    {
        var existing = await _context.Characters
            .FirstOrDefaultAsync(c => c.WclActorId == character.WclActorId, ct);

        if (existing is null)
        {
            _context.Characters.Add(character);
            await _context.SaveChangesAsync(ct);
            return character.Id;
        }

        existing.Name   = character.Name;
        existing.Server = character.Server;
        existing.Region = character.Region;
        existing.Class  = character.Class;
        if (character.GuildId.HasValue)
            existing.GuildId = character.GuildId;
        if (character.PlayerId.HasValue)
            existing.PlayerId = character.PlayerId;

        await _context.SaveChangesAsync(ct);
        return existing.Id;
    }

    public async Task<(IEnumerable<Character> Items, int Total)> SearchAsync(
        string? query,
        string? className,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var q = _context.Characters
            .Include(c => c.Guild)
            .Include(c => c.Player)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var lower = query.Trim().ToLower();
            q = q.Where(c => c.Name.ToLower().Contains(lower));
        }

        if (!string.IsNullOrWhiteSpace(className))
            q = q.Where(c => c.Class != null && c.Class!.Name == className);

        var total = await q.CountAsync(ct);

        var items = await q
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
