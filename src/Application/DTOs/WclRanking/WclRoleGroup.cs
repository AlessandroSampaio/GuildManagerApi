using System.Text.Json.Serialization;

namespace GuildManagerApi.Application.DTOs.WclRanking;

public class WclRoleGroup
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("characters")]
    public List<WclRankingCharacter> Characters { get; set; } = [];
}
