using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using GuildManagerApi.Infrastructure.Auth;
using Microsoft.Extensions.Logging;

namespace GuildManagerApi.Application.Services;

// ── Response models ────────────────────────────────────────────────────────────

public record BlizzardGuildRosterResponse(
    [property: JsonPropertyName("members")] List<BlizzardRosterMember> Members
);

public record BlizzardRosterMember(
    [property: JsonPropertyName("character")] BlizzardRosterCharacter Character,
    [property: JsonPropertyName("rank")] int Rank
);

public record BlizzardRosterCharacter(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("realm")] BlizzardRosterRealm Realm,
    [property: JsonPropertyName("level")] int Level,
    [property: JsonPropertyName("playable_class")] BlizzardKeyedRef? PlayableClass,
    [property: JsonPropertyName("playable_race")] BlizzardKeyedRef? PlayableRace
);

public record BlizzardRosterRealm(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("slug")] string Slug
);

public record BlizzardKeyedRef(
    [property: JsonPropertyName("id")] int Id
);

// ── Interface ──────────────────────────────────────────────────────────────────

public interface IBlizzardGuildClient
{
    /// <summary>
    /// Busca o roster da guilda via Blizzard Game Data API.
    /// Namespace e locale fixos: profile-us / en_US.
    /// </summary>
    Task<IReadOnlyList<BlizzardRosterMember>> GetGuildRosterAsync(
        string realmSlug,
        string guildSlug,
        Guid userId,
        CancellationToken ct = default);
}

// ── Implementation ─────────────────────────────────────────────────────────────

public class BlizzardGuildClient(
    HttpClient httpClient,
    IBNetTokenService bnetTokenService,
    ILogger<BlizzardGuildClient> logger) : IBlizzardGuildClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string BaseUrl = "https://us.api.blizzard.com";
    private const string Namespace = "profile-us";
    private const string Locale = "en_US";

    public async Task<IReadOnlyList<BlizzardRosterMember>> GetGuildRosterAsync(
        string realmSlug,
        string guildSlug,
        Guid userId,
        CancellationToken ct = default)
    {
        var accessToken = await bnetTokenService.GetUserTokenAsync(userId, ct);

        var url = $"{BaseUrl}/data/wow/guild/{realmSlug}/{guildSlug}/roster" +
                  $"?namespace={Namespace}&locale={Locale}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        logger.LogDebug("Fetching Blizzard guild roster: {Url}", url);

        var response = await httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var roster = JsonSerializer.Deserialize<BlizzardGuildRosterResponse>(json, JsonOptions);

        if (roster is null)
            throw new InvalidOperationException("Blizzard API returned an empty or invalid guild roster response.");

        logger.LogDebug("Blizzard roster fetched: {Count} membros.", roster.Members.Count);
        return roster.Members;
    }
}
