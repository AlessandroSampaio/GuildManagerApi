namespace GuildManagerApi.Application.DTOs;

public record RaiderIoKeyRequest(
    string ApiKey,
    string? Label = null
);

public record RaiderIoKeyStatusResponse(
    bool Configured,
    string? Label,
    DateTime? UpdatedAt,
    string Message
);

public record RaiderIoAffixDto(
    int AffixId,
    string Name,
    string IconUrl
);

public record RaiderIoMythicRunDto(
    string Dungeon,
    string ShortName,
    int MythicLevel,
    DateTimeOffset CompletedAt,
    double Score,
    string IconUrl,
    string BackgroundImageUrl,
    List<RaiderIoAffixDto> Affixes
);

public record RaiderIoRaidProgressionDto(
    string RaidSlug,
    string Summary,
    int ExpansionId,
    int TotalBosses,
    int NormalBossesKilled,
    int HeroicBossesKilled,
    int MythicBossesKilled
);

public record RaiderIoSnapshotDto(
    string ThumbnailUrl,
    DateTimeOffset LastCrawledAt,
    DateTimeOffset CachedAt,
    double Score,
    List<RaiderIoMythicRunDto> MythicRuns,
    List<RaiderIoRaidProgressionDto> RaidProgressions
);

public record RaiderIoProfileResponseDto(
    bool IsFresh,
    int CharacterId,
    string Name,
    string Server,
    string Region,
    string? ClassName,
    string? GuildName,
    int? PlayerId,
    string? PlayerName,
    string ThumbnailUrl,
    DateTimeOffset LastCrawledAt,
    DateTimeOffset CachedAt,
    double Score,
    List<RaiderIoMythicRunDto> MythicRuns,
    List<RaiderIoRaidProgressionDto> RaidProgressions
);
