namespace GuildManagerApi.Application.DTOs;

public record WclAuthorizeResponseDto(
    string AuthorizeUrl,
    string State,
    string Instructions
);

public record WclCallbackResponseDto(
    string Message,
    Guid UserId,
    DateTime ExpiresAt,
    bool HasRefreshToken
);

public record WclStatusDto(
    Guid UserId,
    bool IsAuthorized,
    string Message
);

public record WclCredentialRequest(
    string ClientId,
    string ClientSecret,
    string? Label = null
);

public record WclCredentialStatusResponse(
    bool Configured,
    string? Label,
    DateTime? UpdatedAt,
    string Message
);
