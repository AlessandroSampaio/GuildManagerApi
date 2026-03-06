namespace GuildManagerApi.Application.DTOs;

public record PlayerDto(
    int Id,
    string Name,
    int CharacterCount,
    DateTime CreatedAt
);

public record PlayerDetailDto(
    int Id,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<PlayerCharacterDto> Characters
);

public record PlayerCharacterDto(
    int Id,
    string Name,
    string Server,
    string Region,
    string Class,
    string? GuildName
);
