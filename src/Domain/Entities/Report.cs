namespace GuildManagerApi.Domain.Entities;

public class Report
{
    public string Id { get; set; } = string.Empty; // WCL code, e.g "aB192asda12"
    public string Title { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int? GuildId { get; set; }
    public DateTime ImportedAt { get; set; }
    public DateTime? LastSyncedAt { get; set; }

    //Navigation
    public virtual Guild? Guild { get; set; }
    public ICollection<Fight> Fights { get; set; } = [];
}
