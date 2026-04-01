namespace GuildManagerApi.Domain.Entities;

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Character> Characters { get; set; } = [];
    public ICollection<CorePlayer> CoreMemberships { get; set; } = [];
}
