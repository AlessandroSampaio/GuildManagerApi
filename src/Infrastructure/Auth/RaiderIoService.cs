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
    ILogger<RaiderIoService> logger) : IRaiderIoService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly RaiderIoOptions _opts = opts.Value;
    private readonly IRaiderIoCredentialService _credentials = credentials;

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

        return ((int)response.StatusCode, body);
    }

    [LoggerMessage(LogLevel.Debug,
        Message = "RaiderIO profile request: region={Region} realm={Realm} name={Name} authenticated={Authenticated}")]
    public partial void LogRequest(string Region, string Realm, string Name, bool Authenticated);
}
