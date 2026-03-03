using System.Text.Json.Serialization;

namespace GuildManagerApi.Application.DTOs.WclRanking;

public class WclRankingsFight
{
    [JsonPropertyName("fightID")]
    public int FightId { get; set; }

    [JsonPropertyName("difficulty")]
    public int Difficulty { get; set; }

    [JsonPropertyName("kill")]
    public int Kill { get; set; }                       // 1 = kill, 0 = wipe

    [JsonPropertyName("duration")]
    public long Duration { get; set; }

    [JsonPropertyName("roles")]
    public WclRankingsRoles Roles { get; set; } = new();

    [JsonPropertyName("speed")]
    public WclSpeedRanking? Speed { get; set; }

    [JsonPropertyName("execution")]
    public WclExecutionRanking? Execution { get; set; }
}
