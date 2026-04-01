namespace GuildManagerApi.Domain.Entities;

public class Core
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int GuildId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Guild Guild { get; set; } = null!;
    public ICollection<CorePlayer> Players { get; set; } = [];
    public ICollection<RaidWeek> RaidWeeks { get; set; } = [];
}
