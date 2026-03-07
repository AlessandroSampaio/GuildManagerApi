namespace GuildManagerApi.Application.DTOs;

public record ScoringTierDto(
    int Id,
    float MinPercent,
    float MaxPercent,
    int Points,
    string? Label
);

public record ScoringSettingsDto(
    DateTime UpdatedAt,
    List<ScoringTierDto> Tiers
);

public record ScoringTierRequest(
    float MinPercent,
    float MaxPercent,
    int Points,
    string? Label = null
);

public record ScoringSettingsRequest(
    List<ScoringTierRequest> Tiers
);

public record ScoreCalculationResult(
    float RankPercent,
    int? Points,
    string? TierLabel,
    string Message
);
