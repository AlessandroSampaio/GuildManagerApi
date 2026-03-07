namespace GuildManagerApi.Domain.Entities;

public class ScoringTier
{
    public int Id { get; set; }
    public float MinPercent { get; set; }
    public float MaxPercent { get; set; }
    public int Points { get; set; }
    public string Label { get; set; } = string.Empty;
    public int ScoringSettingsId { get; set; }

    // Navigation
    public ScoringSettings ScoringSettings { get; set; } = null!;
}
