using System.Text.Json.Serialization;
using GuildManagerApi.Application.Converters;

namespace GuildManagerApi.Application.DTOs.WclRanking;

public class WclSpeedRanking
{
    [JsonPropertyName("rank")]
    [JsonConverter(typeof(JsonAproximatedRankingConverter))]
    public int Rank { get; set; }

    [JsonPropertyName("best")]
    [JsonConverter(typeof(JsonAproximatedRankingConverter))]
    public int Best { get; set; }

    [JsonPropertyName("totalParses")]
    public int TotalParses { get; set; }

    [JsonPropertyName("rankPercent")]
    public double RankPercent { get; set; }
}
