using System.Text.Json.Serialization;

namespace GuildManagerApi.Application.DTOs.WclRanking;

public class WclRankingServer
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("region")]
    public string Region { get; set; } = string.Empty;
}
