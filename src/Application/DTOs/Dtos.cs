namespace GuildManagerApi.Application.DTOs;

// Import
public record ImportResultDto(
    string ReportCode,
    string Title,
    int FightsImported,
    int KillsImported,
    int PlayersImported,
    int PerformanceEntriesSaved,
    string? GuildName
);

// Reports
public record ReportDto(
    string Id,
    string Title,
    DateTime StartTime,
    DateTime EndTime,
    string? GuildName,
    int FightCount,
    DateTime ImportedAt,
    DateTime? LastSyncedAt
);

public record ReportDetailDto(
    string Id,
    string Title,
    DateTime StartTime,
    DateTime EndTime,
    GuildDto? Guild,
    List<FightDto> Fights,
    DateTime ImportedAt
);

// Fights
public record FightDto(
    int Id,
    int FightIndex,
    string Name,
    bool? Kill,
    long DurationMs,
    int Difficulty
);

// Characters
public record CharacterDto(
    int Id,
    string Name,
    string Server,
    string Class,
    string? GuildName
);

public record CharacterDetailDto(
    int Id,
    string Name,
    string Server,
    string Class,
    string? GuildName,
    List<PerformanceSummaryDto> RecentPerformance
);

// Guilds
public record GuildDto(
    int Id,
    string Name,
    string Server,
    string Region
);

// Performance
public record PerformanceDto(
    int FightId,
    string FightName,
    string CharacterName,
    string Spec,
    string Role,
    float Amount,
    float? RankPercent,
    float? BestPercent
);

public record PerformanceSummaryDto(
    string ReportCode,
    string FightName,
    string Spec,
    string Role,
    float Amount,
    float? RankPercent
);

// Pagination
public record PagedResult<T>(
    List<T> Data,
    int Page,
    int PageSize,
    int Total
);
