namespace GuildManagerApi.Domain.Entities;

public class PerformanceEntry
{
    public int Id { get; set; }
    public int FightId { get; set; }
    public int CharacterId { get; set; }
    public string Spec { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;   // dps | healer | tank
    public float Amount { get; set; }                  // Average DPS/HPS
    public float? RankPercent { get; set; }            // %  Global ranking WCL
    public int? TotalParses { get; set; }
    public float? BestPercent { get; set; }            // Best parse history

    // Navigation
    public Fight Fight { get; set; } = null!;
    public Character Character { get; set; } = null!;
}
