using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GuildManagerApi.Infrastructure.Repositories;

public class GuildRepository(AppDbContext context) : IGuildRepository
{
    private readonly AppDbContext _context = context;

    public Task<Guild?> FindByNameAndServerAsync(string name, string server, string region, CancellationToken ct = default)
            => _context.Guilds
                .FirstOrDefaultAsync(g => g.Name.Equals(name) && g.Server.Equals(server) && g.Region.Equals(region), ct);

    public Task<Guild?> GetByIdAsync(int id, CancellationToken ct = default)
        => _context.Guilds.FirstOrDefaultAsync(g => g.Id == id, ct);

    public async Task<IEnumerable<Report>> GetReportsByGuildAsync(int guildId, int page, int pageSize, CancellationToken ct = default)
        => await _context.Reports
                .Where(r => r.GuildId == guildId)
                .OrderByDescending(r => r.StartTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

    public async Task<int> UpsertAsync(Guild guild, CancellationToken ct = default)
    {
        var existing = await FindByNameAndServerAsync(guild.Name, guild.Server, guild.Region, ct);
        if (existing is null)
        {
            _context.Guilds.Add(guild);
            await _context.SaveChangesAsync(ct);
            return guild.Id;
        }
        return existing.Id;
    }
}
