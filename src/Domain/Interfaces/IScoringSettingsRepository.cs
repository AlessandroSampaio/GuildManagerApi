using GuildManagerApi.Domain.Entities;

namespace GuildManagerApi.Domain.Interfaces;

public record ScoringTierInput(
    float MinPercent,
    float MaxPercent,
    int Points,
    string? Label = null
);

public interface IScoringSettingsRepository
{
    Task<ScoringSettings?> GetAsync(CancellationToken ct = default);
    Task<ScoringSettings> SaveAsync(IEnumerable<ScoringTierInput> tiers, CancellationToken ct = default);
    Task<bool> DeleteAsync(CancellationToken ct = default);
    Task<int?> CalculateScoreAsync(float rankPercent, CancellationToken ct = default);
}
