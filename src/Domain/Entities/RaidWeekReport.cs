namespace GuildManagerApi.Domain.Entities;

public class RaidWeekReport
{
    public int Id { get; set; }
    public int RaidWeekId { get; set; }
    public string ReportCode { get; set; } = null!;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;


    public virtual RaidWeek RaidWeek { get; set; } = null!;
    public virtual Report Report { get; set; } = null!;

}
