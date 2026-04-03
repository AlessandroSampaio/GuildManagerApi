using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GuildManagerApi.Infrastructure.Repositories;

public class RaiderIoProfileRepository(AppDbContext context) : IRaiderIoProfileRepository
{
    private readonly AppDbContext _context = context;

    public async Task UpsertSnapshotAsync(RaiderIoCharacterSnapshot snapshot, CancellationToken ct = default)
    {
        var nameLower = snapshot.Name.ToLower();
        var realmLower = snapshot.Realm.ToLower();
        var regionLower = snapshot.Region.ToLower();

        var existing = await _context.RaiderIoCharacterSnapshots
            .Include(s => s.MythicRuns)
                .ThenInclude(r => r.Affixes)
            .FirstOrDefaultAsync(s =>
                s.Name == nameLower &&
                s.Realm == realmLower &&
                s.Region == regionLower, ct);

        // Try to resolve the linked Character
        var character = await _context.Characters.FirstOrDefaultAsync(c =>
            c.Name.ToLower() == nameLower &&
            c.Server.ToLower() == realmLower &&
            c.Region.ToLower() == regionLower, ct);

        if (existing is null)
        {
            existing = new RaiderIoCharacterSnapshot
            {
                Name = nameLower,
                Realm = realmLower,
                Region = regionLower,
            };
            _context.RaiderIoCharacterSnapshots.Add(existing);
        }

        existing.ThumbnailUrl = snapshot.ThumbnailUrl;
        existing.LastCrawledAt = snapshot.LastCrawledAt;
        existing.CachedAt = snapshot.CachedAt;
        existing.CharacterId = character?.Id;

        // Flush snapshot so it gets an Id before upserting runs
        await _context.SaveChangesAsync(ct);

        foreach (var run in snapshot.MythicRuns)
        {
            var existingRun = existing.MythicRuns
                .FirstOrDefault(r => r.KeystoneRunId == run.KeystoneRunId);

            if (existingRun is null)
            {
                existingRun = new RaiderIoMythicRun
                {
                    SnapshotId = existing.Id,
                    KeystoneRunId = run.KeystoneRunId,
                };
                _context.RaiderIoMythicRuns.Add(existingRun);
                existing.MythicRuns.Add(existingRun);
            }

            existingRun.Dungeon = run.Dungeon;
            existingRun.ShortName = run.ShortName;
            existingRun.MythicLevel = run.MythicLevel;
            existingRun.CompletedAt = run.CompletedAt;
            existingRun.Score = run.Score;
            existingRun.IconUrl = run.IconUrl;
            existingRun.BackgroundImageUrl = run.BackgroundImageUrl;

            // Flush run to get its Id before upserting affixes
            await _context.SaveChangesAsync(ct);

            foreach (var affix in run.Affixes)
            {
                var existingAffix = existingRun.Affixes
                    .FirstOrDefault(a => a.AffixId == affix.AffixId);

                if (existingAffix is null)
                {
                    existingAffix = new RaiderIoRunAffix
                    {
                        MythicRunId = existingRun.Id,
                        AffixId = affix.AffixId,
                    };
                    _context.RaiderIoRunAffixes.Add(existingAffix);
                    existingRun.Affixes.Add(existingAffix);
                }

                existingAffix.Name = affix.Name;
                existingAffix.IconUrl = affix.IconUrl;
            }
        }

        await _context.SaveChangesAsync(ct);
    }
}
