namespace GuildManagerApi.Application.DTOs;

public record BNetAuthorizeResponseDto(
    string AuthorizeUrl,
    string State,
    string Instructions
);

public record BNetStatusDto(
    Guid UserId,
    bool IsAuthorized,
    string? BattleTag,
    string Message
);

public record BNetCredentialRequest(
    string ClientId,
    string ClientSecret,
    string? Label = null
);

public record BNetCredentialStatusResponse(
    bool Configured,
    string? Label,
    DateTime? UpdatedAt,
    string Message
);
