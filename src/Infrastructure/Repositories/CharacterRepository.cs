using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GuildManagerApi.Infrastructure.Repositories;


public class CharacterRepository(AppDbContext context) : ICharacterRepository
{
    private readonly AppDbContext _context = context;

    public Task<Character?> FindByWclActorAsync(int wclActorId, string server, CancellationToken ct = default)
        => _context.Characters.FirstOrDefaultAsync(c => c.WclActorId == wclActorId && c.Server.Equals(server), ct);

    public async Task<IEnumerable<Character>> GetByGuildAsync(int guildId, CancellationToken ct = default)
        => await _context.Characters.Where(c => c.GuildId == guildId).ToListAsync(ct);

    public Task<Character?> GetByIdAsync(int id, CancellationToken ct = default)
        => _context.Characters.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<int> UpsertAsync(Character character, CancellationToken ct = default)
    {
        var existing = await GetByIdAsync(character.Id, ct);
        if (existing is null)
        {
            _context.Characters.Add(character);
            await _context.SaveChangesAsync(ct);
            return character.Id;
        }

        existing.Name = character.Name;
        existing.Class = character.Class;
        if (character.GuildId.HasValue)
            existing.GuildId = character.GuildId;

        await _context.SaveChangesAsync(ct);
        return existing.Id;
    }
}
