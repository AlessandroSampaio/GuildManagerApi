namespace GuildManagerApi.Domain.Entities;


public class Fight
{
    public int Id { get; set; }
    public int FightIndex { get; set; }              //Original Id from WCL
    public string ReportId { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public bool? Kill { get; set; }
    public long StartTimeMs { get; set; }
    public long EndTimeMs { get; set; }
    public long DurationMs => EndTimeMs - StartTimeMs;
    public int Difficulty { get; set; }

    // Navigation
    public virtual Report Report { get; set; } = null!;
    public virtual ICollection<PerformanceEntry> PerformanceEntries { get; set; } = [];
}
