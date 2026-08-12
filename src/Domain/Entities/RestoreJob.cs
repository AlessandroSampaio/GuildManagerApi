using GuildManagerApi.Domain.Enums;

namespace GuildManagerApi.Domain.Entities;

public class RestoreJob
{
    public Guid Id { get; set; }
    public Guid? SourceBackupId { get; set; }
    public string SourceFileName { get; set; } = string.Empty;
    public bool IsUpload { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Queued;
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }

    public virtual BackupJob? SourceBackup { get; set; }
}
