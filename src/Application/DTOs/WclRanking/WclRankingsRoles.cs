using System.Text.Json.Serialization;

namespace GuildManagerApi.Application.DTOs.WclRanking;

public class WclRankingsRoles
{
    [JsonPropertyName("tanks")]
    public WclRoleGroup Tanks { get; set; } = new();

    [JsonPropertyName("healers")]
    public WclRoleGroup Healers { get; set; } = new();

    [JsonPropertyName("dps")]
    public WclRoleGroup Dps { get; set; } = new();
}
