namespace GuildManagerApi.Domain.Entities;

public class ScoringSettings
{
    public int Id { get; set; } = 1;
    public DateTime UpdatedAt { get; set; }

    public ICollection<ScoringTier> Tiers { get; set; } = [];

}
