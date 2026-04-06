using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GuildManagerApi.Infrastructure.Repositories;

public class RaiderIoProfileRepository(AppDbContext context) : IRaiderIoProfileRepository
{
    private readonly AppDbContext _context = context;

    public Task<RaiderIoCharacterSnapshot?> GetSnapshotByCharacterIdAsync(int characterId, CancellationToken ct = default)
        => _context.RaiderIoCharacterSnapshots
            .Include(s => s.MythicRuns)
                .ThenInclude(r => r.Affixes)
            .Include(s => s.RaidProgressions)
            .FirstOrDefaultAsync(s => s.CharacterId == characterId, ct);

    public async Task UpsertSnapshotAsync(RaiderIoCharacterSnapshot snapshot, CancellationToken ct = default)
    {
        var nameLower = snapshot.Name.ToLower();
        var realmLower = snapshot.Realm.ToLower();
        var regionLower = snapshot.Region.ToLower();

        var existing = await _context.RaiderIoCharacterSnapshots
            .Include(s => s.MythicRuns)
            .Include(s => s.RaidProgressions)
            .FirstOrDefaultAsync(s =>
                s.Name == nameLower &&
                s.Realm == realmLower &&
                s.Region == regionLower, ct);


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
        else
        {
            // Full replacement: delete all existing runs and progressions (cascade)
            _context.RaiderIoMythicRuns.RemoveRange(existing.MythicRuns);
            existing.MythicRuns.Clear();
            _context.RaiderIoRaidProgressions.RemoveRange(existing.RaidProgressions);
            existing.RaidProgressions.Clear();
        }

        existing.ThumbnailUrl = snapshot.ThumbnailUrl;
        existing.LastCrawledAt = snapshot.LastCrawledAt;
        existing.CachedAt = snapshot.CachedAt;
        existing.CharacterId = snapshot.CharacterId;

        await _context.SaveChangesAsync(ct);

        // Load all known affixes once; only insert new ones
        var knownAffixes = await _context.RaiderIoRunAffixes
            .ToDictionaryAsync(a => a.AffixId, ct);

        foreach (var run in snapshot.MythicRuns)
        {
            var newRun = new RaiderIoMythicRun
            {
                SnapshotId = existing.Id,
                KeystoneRunId = run.KeystoneRunId,
                Dungeon = run.Dungeon,
                ShortName = run.ShortName,
                MythicLevel = run.MythicLevel,
                CompletedAt = run.CompletedAt,
                Score = run.Score,
                IconUrl = run.IconUrl,
                BackgroundImageUrl = run.BackgroundImageUrl,
            };

            foreach (var affix in run.Affixes)
            {
                if (!knownAffixes.TryGetValue(affix.AffixId, out var affixEntity))
                {
                    affixEntity = new RaiderIoRunAffix
                    {
                        AffixId = affix.AffixId,
                        Name = affix.Name,
                        IconUrl = affix.IconUrl,
                    };
                    _context.RaiderIoRunAffixes.Add(affixEntity);
                    knownAffixes[affix.AffixId] = affixEntity;
                }

                newRun.Affixes.Add(affixEntity);
            }

            existing.MythicRuns.Add(newRun);
        }

        foreach (var prog in snapshot.RaidProgressions)
        {
            existing.RaidProgressions.Add(new RaiderIoRaidProgression
            {
                SnapshotId = existing.Id,
                RaidSlug = prog.RaidSlug,
                Summary = prog.Summary,
                ExpansionId = prog.ExpansionId,
                TotalBosses = prog.TotalBosses,
                NormalBossesKilled = prog.NormalBossesKilled,
                HeroicBossesKilled = prog.HeroicBossesKilled,
                MythicBossesKilled = prog.MythicBossesKilled,
            });
        }

        await _context.SaveChangesAsync(ct);
    }
}
