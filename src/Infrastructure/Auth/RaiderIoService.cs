using System.Text.Json;
using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GuildManagerApi.Infrastructure.Auth;

public class RaiderIoOptions
{
    public const string Section = "RaiderIo";
    public string BaseUrl { get; set; } = "https://raider.io";
}

public interface IRaiderIoService
{
    /// <summary>
    /// Busca o perfil de um personagem no Raider.IO com fields fixos:
    /// mythic_plus_best_runs:all,raid_progression.
    /// Injeta o access_key automaticamente se configurado.
    /// </summary>
    Task<(int StatusCode, string Body)> GetCharacterProfileAsync(
        string region, string realm, string name, CancellationToken ct = default);
}

public partial class RaiderIoService(
    HttpClient httpClient,
    IOptions<RaiderIoOptions> opts,
    IRaiderIoCredentialService credentials,
    IRaiderIoProfileRepository profileRepository,
    ILogger<RaiderIoService> logger) : IRaiderIoService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly RaiderIoOptions _opts = opts.Value;
    private readonly IRaiderIoCredentialService _credentials = credentials;
    private readonly IRaiderIoProfileRepository _profileRepository = profileRepository;

    private const string FixedFields = "mythic_plus_best_runs:all,raid_progression";

    public async Task<(int StatusCode, string Body)> GetCharacterProfileAsync(
        string region, string realm, string name, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string>
        {
            ["region"] = region.Trim().ToLower(),
            ["realm"] = realm.Trim(),
            ["name"] = name.Trim(),
            ["fields"] = FixedFields,
        };

        var apiKey = await _credentials.GetApiKeyAsync(ct);
        if (!string.IsNullOrEmpty(apiKey))
            query["access_key"] = apiKey;

        var qs = string.Join("&", query.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        var url = $"{_opts.BaseUrl.TrimEnd('/')}/api/v1/characters/profile?{qs}";

        LogRequest(region, realm, name, apiKey is not null);

        var response = await _httpClient.GetAsync(url, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var snapshot = ParseSnapshot(body);
                await _profileRepository.UpsertSnapshotAsync(snapshot, ct);
            }
            catch (Exception ex)
            {
                LogCacheError(ex);
            }
        }

        return ((int)response.StatusCode, body);
    }

    private static RaiderIoCharacterSnapshot ParseSnapshot(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var snapshot = new RaiderIoCharacterSnapshot
        {
            Name = root.GetProperty("name").GetString() ?? string.Empty,
            Realm = root.GetProperty("realm").GetString() ?? string.Empty,
            Region = root.GetProperty("region").GetString() ?? string.Empty,
            ThumbnailUrl = root.TryGetProperty("thumbnail_url", out var thumb)
                ? thumb.GetString() ?? string.Empty
                : string.Empty,
            LastCrawledAt = root.TryGetProperty("last_crawled_at", out var crawled)
                ? DateTimeOffset.Parse(crawled.GetString()!)
                : DateTimeOffset.UtcNow,
            CachedAt = DateTimeOffset.UtcNow,
        };

        if (root.TryGetProperty("mythic_plus_best_runs", out var runs) &&
            runs.ValueKind == JsonValueKind.Array)
        {
            foreach (var run in runs.EnumerateArray())
            {
                var mythicRun = new RaiderIoMythicRun
                {
                    KeystoneRunId = run.TryGetProperty("keystone_run_id", out var rid)
                        ? rid.GetInt64() : 0,
                    Dungeon = run.TryGetProperty("dungeon", out var d)
                        ? d.GetString() ?? string.Empty : string.Empty,
                    ShortName = run.TryGetProperty("short_name", out var sn)
                        ? sn.GetString() ?? string.Empty : string.Empty,
                    MythicLevel = run.TryGetProperty("mythic_level", out var ml)
                        ? ml.GetInt32() : 0,
                    CompletedAt = run.TryGetProperty("completed_at", out var ca)
                        ? DateTimeOffset.Parse(ca.GetString()!) : DateTimeOffset.UtcNow,
                    Score = run.TryGetProperty("score", out var sc)
                        ? sc.GetDouble() : 0,
                    IconUrl = run.TryGetProperty("icon_url", out var iu)
                        ? iu.GetString() ?? string.Empty : string.Empty,
                    BackgroundImageUrl = run.TryGetProperty("background_image_url", out var bg)
                        ? bg.GetString() ?? string.Empty : string.Empty,
                };

                if (run.TryGetProperty("affixes", out var affixes) &&
                    affixes.ValueKind == JsonValueKind.Array)
                {
                    foreach (var affix in affixes.EnumerateArray())
                    {
                        mythicRun.Affixes.Add(new RaiderIoRunAffix
                        {
                            AffixId = affix.TryGetProperty("id", out var aid)
                                ? aid.GetInt32() : 0,
                            Name = affix.TryGetProperty("name", out var an)
                                ? an.GetString() ?? string.Empty : string.Empty,
                            IconUrl = affix.TryGetProperty("icon_url", out var aiu)
                                ? aiu.GetString() ?? string.Empty : string.Empty,
                        });
                    }
                }

                snapshot.MythicRuns.Add(mythicRun);
            }
        }

        return snapshot;
    }

    [LoggerMessage(LogLevel.Debug,
        Message = "RaiderIO profile request: region={Region} realm={Realm} name={Name} authenticated={Authenticated}")]
    public partial void LogRequest(string Region, string Realm, string Name, bool Authenticated);

    [LoggerMessage(LogLevel.Warning,
        Message = "Failed to cache RaiderIO profile to database")]
    public partial void LogCacheError(Exception ex);
}
