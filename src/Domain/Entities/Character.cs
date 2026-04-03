namespace GuildManagerApi.Domain.Entities;

public class Character
{

    public int Id { get; set; }
    public long WclActorId { get; set; } //Id on WCL
    public string Name { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public int? ClassId { get; set; } = 0;
    public int? GuildId { get; set; } = null;
    public int? PlayerId { get; set; }


    // Navigation
    public virtual Class? Class { get; set; } = null!;
    public virtual Guild? Guild { get; set; }
    public virtual Player? Player { get; set; }
    public ICollection<PerformanceEntry>? PerformanceEntries { get; set; } = [];
    public virtual RaiderIoCharacterSnapshot? RaiderIoSnapshot { get; set; }

}
