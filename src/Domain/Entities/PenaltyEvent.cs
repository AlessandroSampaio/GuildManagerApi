namespace GuildManagerApi.Domain.Entities;

public class PenaltyEvent
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Points { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PlayerWeekPenalty> PlayerPenalties { get; set; } = [];
}
