namespace GuildManagerApi.Domain.Entities;

public class RaiderIoMythicRun
{
    public int Id { get; set; }
    public int SnapshotId { get; set; }
    public string Dungeon { get; set; } = string.Empty;
    public string ShortName { get; set; } = string.Empty;
    public int MythicLevel { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
    public double Score { get; set; }
    public string IconUrl { get; set; } = string.Empty;
    public string BackgroundImageUrl { get; set; } = string.Empty;

    public virtual RaiderIoCharacterSnapshot Snapshot { get; set; } = null!;
    public ICollection<RaiderIoRunAffix> Affixes { get; set; } = [];
}
