namespace GuildManagerApi.Application.DTOs;

public record PlayerScoringRequest(
    /// <summary>
    /// Lista de report codes a considerar no cálculo (ex: ["aAbBcC", "xXyYzZ"]).
    /// Mínimo 1, máximo 20 por requisição.
    /// </summary>
    List<string> ReportCodes
);

public record PlayerScoringResult(
    List<PlayerScoreDto> Players,
    List<ReportStatusDto> Reports,
    ScoringSettingsDto ScoringSettings,
    RaidWeekSummaryDto? RaidWeek,
    int TotalEntriesEvaluated,
    int EntriesWithoutRankPercent
);

public record PlayerScoreDto(
    int PlayerId,
    string PlayerName,
    int TotalPoints,
    float? AverageRankPercent,
    int ScoredEntries,
    int UnscoredEntries,
    List<PlayerCharacterScoreDto> Characters
);

public record PlayerCharacterScoreDto(
    int CharacterId,
    string CharacterName,
    string Class,
    string Server,
    int TotalPoints,
    float? AverageRankPercent,
    List<FightScoreEntryDto> Fights
);

public record FightScoreEntryDto(
    string ReportCode,
    string ReportTitle,
    int FightId,
    string FightName,
    bool? Kill,
    string Spec,
    string Role,
    float Amount,
    float? RankPercent,
    int? Points,
    string? TierLabel
);

public record ReportStatusDto(
    string ReportCode,
    string Title,
    string ImportStatus,
    bool IncludedInScoring
);
