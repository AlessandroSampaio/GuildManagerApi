namespace GuildManagerApi.Domain.Entities;

public class PlayerWeekPenalty
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public int RaidWeekId { get; set; }
    public int PenaltyEventId { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Player Player { get; set; } = null!;
    public virtual RaidWeek RaidWeek { get; set; } = null!;
    public virtual PenaltyEvent PenaltyEvent { get; set; } = null!;
}
