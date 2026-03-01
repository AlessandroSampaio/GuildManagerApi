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
