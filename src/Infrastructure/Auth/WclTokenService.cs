using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GuildManagerApi.Infrastructure.Auth;

public class WclAuthOptions
{
    public const string Section = "WarcraftLogs";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string TokenEndpoint { get; set; } = "https://www.warcraftlogs.com/oauth/token";
    public string GraphQlEndpoint { get; set; } = "https://www.warcraftlogs.com/api/v2/client";
}


public interface IWclTokenService
{
    Task<string> GetAccessTokenAsync(CancellationToken ct);
}

public partial class WclTokenService(HttpClient httpClient, IOptions<WclAuthOptions> opts, ILogger<WclTokenService> logger) : IWclTokenService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly WclAuthOptions _opts = opts.Value;
    private readonly ILogger<WclTokenService> _logger = logger;

    private string? _cachedToken;
    private DateTime _tokenExpiresAt = DateTime.MinValue;
    private readonly SemaphoreSlim _lock = new(1, 1);


    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiresAt.AddMinutes(-2))
            return _cachedToken;


        await _lock.WaitAsync(ct);
        try
        {
            _logger.LogInformation("Refreshing WarcraftLogs OAuth token...");

            var credentials = Convert.ToBase64String(
                           Encoding.UTF8.GetBytes($"{_opts.ClientId}:{_opts.ClientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, _opts.TokenEndpoint)
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Basic", credentials) },
                Content = new FormUrlEncodedContent(new[]
                            {
                            new KeyValuePair<string, string>("grant_type", "client_credentials")
                        })
            };

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);

            _cachedToken = doc.RootElement.GetProperty("access_token").GetString()
                           ?? throw new InvalidOperationException("access_token not found in response");

            var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
            _tokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);

            LogRefresh(_tokenExpiresAt);
            return _cachedToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    [LoggerMessage(LogLevel.Information, Message = "Token refreshed. Expires at {ExpiresAt}")]
    public partial void LogRefresh(DateTime ExpiresAt);
}
