namespace GuildManagerApi.Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public Guid? ActorId { get; set; }
    public string? ActorUsername { get; set; }
    public string? Details { get; set; }
    public DateTime OccurredAt { get; set; }
}
