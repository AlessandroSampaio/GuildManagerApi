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
