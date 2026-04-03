namespace GuildManagerApi.Domain.Entities;

public class RaiderIoCharacterSnapshot
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public DateTimeOffset LastCrawledAt { get; set; }
    public DateTimeOffset CachedAt { get; set; }

    public int? CharacterId { get; set; }
    public virtual Character? Character { get; set; }

    public ICollection<RaiderIoMythicRun> MythicRuns { get; set; } = [];
}
