namespace GuildManagerApi.Domain.Entities;

public class CorePlayer
{
    public int CoreId { get; set; }
    public int PlayerId { get; set; }

    // Navigation
    public Core Core { get; set; } = null!;
    public Player Player { get; set; } = null!;
}
