namespace GuildManagerApi.Domain.Entities;

public class RaiderIoRunAffix
{
    public int Id { get; set; }
    public int MythicRunId { get; set; }
    public int AffixId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;

    public virtual RaiderIoMythicRun MythicRun { get; set; } = null!;
}
