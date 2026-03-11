using System.Text.Json.Serialization;
using GuildManagerApi.Application.Converters;

namespace GuildManagerApi.Application.DTOs.WclRanking;

public class WclRankingCharacter
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("server")]
    public WclRankingServer? Server { get; set; }

    [JsonPropertyName("class")]
    public string Class { get; set; } = string.Empty;

    [JsonPropertyName("spec")]
    public string Spec { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public double Amount { get; set; }

    [JsonPropertyName("rankPercent")]
    public double RankPercent { get; set; }

    [JsonPropertyName("bracketPercent")]
    public double BracketPercent { get; set; }

    [JsonPropertyName("rank")]
    [JsonConverter(typeof(JsonAproximatedRankingConverter))]
    public int Rank { get; set; }

    [JsonPropertyName("best")]
    [JsonConverter(typeof(JsonAproximatedRankingConverter))]
    public int Best { get; set; }

    [JsonPropertyName("totalParses")]
    public int TotalParses { get; set; }
}
