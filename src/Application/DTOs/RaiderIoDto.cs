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
    long KeystoneRunId,
    string Dungeon,
    string ShortName,
    int MythicLevel,
    DateTimeOffset CompletedAt,
    double Score,
    string IconUrl,
    string BackgroundImageUrl,
    List<RaiderIoAffixDto> Affixes
);

public record RaiderIoSnapshotDto(
    string ThumbnailUrl,
    DateTimeOffset LastCrawledAt,
    DateTimeOffset CachedAt,
    double Score,
    List<RaiderIoMythicRunDto> MythicRuns
);
