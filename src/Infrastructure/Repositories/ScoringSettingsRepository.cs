using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Exceptions;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GuildManagerApi.Infrastructure.Repositories;

public class ScoringSettingsRepository(AppDbContext context) : IScoringSettingsRepository
{
    private readonly AppDbContext _context = context;
    private ScoringSettings? _cache;

    public async Task<int?> CalculateScoreAsync(float rankPercent, CancellationToken ct = default)
    {
        var settings = await GetAsync(ct);
        if (settings is null || settings.Tiers.Count != 0) return null;

        // Intervalo semi-aberto [Min, Max), fechado no 100 da última faixa
        var tier = settings.Tiers
            .OrderBy(t => t.MinPercent)
            .FirstOrDefault(t =>
                rankPercent >= t.MinPercent &&
                (rankPercent < t.MaxPercent || t.MaxPercent >= 100f));

        return tier?.Points;
    }

    public async Task<bool> DeleteAsync(CancellationToken ct = default)
    {
        var settings = await _context.ScoringSettings
                   .Include(s => s.Tiers)
                   .FirstOrDefaultAsync(s => s.Id == 1, ct);

        if (settings is null) return false;

        _context.ScoringTiers.RemoveRange(settings.Tiers);
        _context.ScoringSettings.Remove(settings);
        await _context.SaveChangesAsync(ct);

        _cache = null;
        return true;
    }

    public async Task<ScoringSettings?> GetAsync(CancellationToken ct = default)
    {
        if (_cache is not null) return _cache;

        _cache = await _context.ScoringSettings
            .Include(s => s.Tiers.OrderBy(t => t.MinPercent))
            .FirstOrDefaultAsync(s => s.Id == 1, ct);

        return _cache;
    }

    public async Task<ScoringSettings> SaveAsync(IEnumerable<ScoringTierInput> tiers, CancellationToken ct = default)
    {
        var tierList = tiers.ToList();

        Validate(tierList);

        await using var tx = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            var settings = await _context.ScoringSettings
                .Include(s => s.Tiers)
                .FirstOrDefaultAsync(s => s.Id == 1, ct);

            if (settings is null)
            {

                settings = new() { Id = 1 };
                _context.ScoringSettings.Add(settings);
                await _context.SaveChangesAsync(ct);
            }
            else
            {
                _context.ScoringTiers.RemoveRange(settings.Tiers);
                settings.Tiers.Clear();
            }

            foreach (var input in tierList.OrderBy(t => t.MinPercent))
            {
                settings.Tiers.Add(new ScoringTier
                {
                    MinPercent = input.MinPercent,
                    MaxPercent = input.MaxPercent,
                    Points = input.Points,
                    Label = input.Label?.Trim() ?? "unknown",
                    ScoringSettingsId = 1,
                });
            }

            settings.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            _cache = null; // invalidar cache
            return (await GetAsync(ct))!;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }


    private static void Validate(List<ScoringTierInput> tiers)
    {
        if (tiers.Count == 0)
            throw new ScoringValidationException("At least one scoring tier is required.");

        foreach (var t in tiers)
        {
            if (t.MinPercent < 0 || t.MaxPercent > 100)
                throw new ScoringValidationException(
                    $"Tier [{t.MinPercent}–{t.MaxPercent}]: percentages must be between 0 and 100.");

            if (t.MinPercent >= t.MaxPercent)
                throw new ScoringValidationException(
                    $"Tier [{t.MinPercent}–{t.MaxPercent}]: MinPercent must be less than MaxPercent.");

            if (t.Points < 0)
                throw new ScoringValidationException(
                    $"Tier [{t.MinPercent}–{t.MaxPercent}]: Points must be non-negative.");
        }

        var sorted = tiers.OrderBy(t => t.MinPercent).ToList();

        // Verificar se começa em 0
        if (sorted[0].MinPercent != 0)
            throw new ScoringValidationException(
                $"Tiers must start at 0. First tier starts at {sorted[0].MinPercent}.");

        // Verificar se termina em 100
        if (sorted[^1].MaxPercent != 100)
            throw new ScoringValidationException(
                $"Tiers must end at 100. Last tier ends at {sorted[^1].MaxPercent}.");

        // Verificar gaps e overlaps entre faixas consecutivas
        for (int i = 0; i < sorted.Count - 1; i++)
        {
            var current = sorted[i];
            var next = sorted[i + 1];

            if (current.MaxPercent > next.MinPercent)
                throw new ScoringValidationException(
                    $"Tiers overlap: [{current.MinPercent}–{current.MaxPercent}] and " +
                    $"[{next.MinPercent}–{next.MaxPercent}].");

            if (current.MaxPercent < next.MinPercent)
                throw new ScoringValidationException(
                    $"Gap between tiers: [{current.MinPercent}–{current.MaxPercent}] and " +
                    $"[{next.MinPercent}–{next.MaxPercent}]. " +
                    $"Range [{current.MaxPercent}–{next.MinPercent}] is not covered.");
        }
    }
}
