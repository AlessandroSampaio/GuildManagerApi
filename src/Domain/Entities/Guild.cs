namespace GuildManagerApi.Domain.Entities;

public class Guild
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;



    // Navigation
    public ICollection<Report> Reports { get; set; } = [];
    public ICollection<Character> Characters { get; set; } = [];
    public ICollection<Core> Cores { get; set; } = [];
}
