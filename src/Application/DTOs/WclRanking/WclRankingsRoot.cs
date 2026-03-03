using System.Text.Json.Serialization;

namespace GuildManagerApi.Application.DTOs.WclRanking;

public class WclRankingsRoot
{
    [JsonPropertyName("data")]
    public List<WclRankingsFight> Data { get; set; } = [];
}
