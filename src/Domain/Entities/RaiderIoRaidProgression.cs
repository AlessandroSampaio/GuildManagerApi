namespace GuildManagerApi.Domain.Entities;

public class RaiderIoRaidProgression
{
    public int Id { get; set; }
    public int SnapshotId { get; set; }
    public string RaidSlug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int ExpansionId { get; set; }
    public int TotalBosses { get; set; }
    public int NormalBossesKilled { get; set; }
    public int HeroicBossesKilled { get; set; }
    public int MythicBossesKilled { get; set; }

    public virtual RaiderIoCharacterSnapshot Snapshot { get; set; } = null!;
}
