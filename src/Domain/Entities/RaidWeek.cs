namespace GuildManagerApi.Domain.Entities;


public class RaidWeek
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime EndsAt => StartsAt.AddDays(7);



    public ICollection<RaidWeekReport> ReportEntries { get; set; } = [];
}
