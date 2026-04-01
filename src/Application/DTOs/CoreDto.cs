namespace GuildManagerApi.Application.DTOs;

public record CoreDto(
    int Id,
    string Name,
    int GuildId,
    string GuildName,
    int PlayerCount,
    DateTime CreatedAt
);

public record CoreDetailDto(
    int Id,
    string Name,
    int GuildId,
    string GuildName,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<CorePlayerDto> Players
);

public record CorePlayerDto(
    int PlayerId,
    string Name
);

public record CoreCreateRequest(
    string Name,
    int GuildId
);

public record CoreUpdateRequest(
    string Name
);
