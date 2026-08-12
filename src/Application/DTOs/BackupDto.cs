using Microsoft.AspNetCore.Http;

namespace GuildManagerApi.Application.DTOs;

public record BackupAcceptedDto(
    Guid BackupId,
    string Status,
    string WsUrl,
    string Message
);

public record RestoreAcceptedDto(
    Guid RestoreId,
    string Status,
    string WsUrl,
    string Message
);

public record BackupDto(
    Guid Id,
    string FileName,
    long? SizeBytes,
    string Status,
    DateTime CreatedAt,
    Guid? CreatedByUserId,
    DateTime? CompletedAt,
    string? ErrorMessage
);

public record RestoreConfirmRequest(bool Confirm);

public class RestoreUploadRequest
{
    public IFormFile File { get; set; } = null!;
    public bool Confirm { get; set; }
}
