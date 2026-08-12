using GuildManagerApi.Domain.Enums;

namespace GuildManagerApi.Domain.Entities;

public class BackupJob
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long? SizeBytes { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Queued;
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
