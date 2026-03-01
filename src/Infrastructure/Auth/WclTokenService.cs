using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GuildManagerApi.Infrastructure.Auth;

public class WclAuthOptions
{
    public const string Section = "WarcraftLogs";
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string AuthorizeEndpoint { get; set; } = "https://www.warcraftlogs.com/oauth/authorize";
    public string TokenEndpoint { get; set; } = "https://www.warcraftlogs.com/oauth/token";
    /// <summary>Public Route — client credentials, no user authenticated.</summary>
    public string PublicGraphQlEndpoint { get; set; } = "https://www.warcraftlogs.com/api/v2/client";
    /// <summary>Private Route — Must have a valid token (Authorization Code Flow).</summary>
    public string PrivateGraphQlEndpoint { get; set; } = "https://www.warcraftlogs.com/api/v2/user";
    /// <summary>URI de callback registrada no painel do WarcraftLogs.</summary>
    public string RedirectUri { get; set; } = "https://localhost:5000/api/wcl-auth/callback";
}


public interface IWclTokenService
{
    /// <summary>Token de aplicação via Client Credentials (rota pública /api/v2/client).</summary>
    Task<string> GetAppTokenAsync(CancellationToken ct = default);

    /// <summary>Gera a URL de autorização do WCL para redirecionar o usuário.</summary>
    (string AuthorizeUrl, string State) BuildAuthorizeUrl();

    /// <summary>Troca o authorization code por access + refresh token do usuário.</summary>
    Task<WclUserToken> ExchangeCodeAsync(Guid userId, string code, CancellationToken ct = default);

    /// <summary>Retorna o access token do usuário, renovando via refresh token se necessário.</summary>
    Task<string> GetUserTokenAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Indica se o usuário já autorizou o acesso WCL.</summary>
    Task<bool> IsUserAuthorizedAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Revoga e remove o token WCL do usuário.</summary>
    Task RevokeUserTokenAsync(Guid userId, CancellationToken ct = default);
}

public partial class WclTokenService(HttpClient httpClient, IOptions<WclAuthOptions> opts, AppDbContext context, ILogger<WclTokenService> logger) : IWclTokenService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly WclAuthOptions _opts = opts.Value;
    private readonly ILogger<WclTokenService> _logger = logger;
    private readonly AppDbContext _db = context;


    // Cache do token de aplicação (compartilhado, sem vínculo de usuário)
    private string? _cachedAppToken;
    private DateTime _appTokenExpiresAt = DateTime.MinValue;
    private readonly SemaphoreSlim _appTokenLock = new(1, 1);

    // Public route - Client credentials
    public async Task<string> GetAppTokenAsync(CancellationToken ct = default)
    {
        if (_cachedAppToken is not null && DateTime.UtcNow < _appTokenExpiresAt)
            return _cachedAppToken;


        await _appTokenLock.WaitAsync(ct);
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

            _cachedAppToken = doc.RootElement.GetProperty("access_token").GetString()
                           ?? throw new InvalidOperationException("access_token not found in response");

            var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
            _appTokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);

            LogRefresh(_appTokenExpiresAt);
            return _cachedAppToken;
        }
        finally
        {
            _appTokenLock.Release();
        }
    }

    // Private Route - Authorization Code Flow
    public (string AuthorizeUrl, string State) BuildAuthorizeUrl()
    {
        // Used to prevent CSRF
        var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
            .Replace("+", "-").Replace("/", "_").Replace("=", "");

        var query = new Dictionary<string, string>
        {
            ["client_id"] = _opts.ClientId,
            ["redirect_uri"] = _opts.RedirectUri,
            ["response_type"] = "code",
            ["state"] = state,
        };

        var qs = string.Join("&", query.Select(kv =>
                    $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        return ($"{_opts.AuthorizeEndpoint}?{qs}", state);
    }

    public async Task<WclUserToken> ExchangeCodeAsync(Guid userId, string code, CancellationToken ct = default)
    {
        var credentials = Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{_opts.ClientId}:{_opts.ClientSecret}"));


        var request = new HttpRequestMessage(HttpMethod.Post, _opts.TokenEndpoint)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Basic", credentials) },
            Content = new FormUrlEncodedContent(new[]
                   {
                       new KeyValuePair<string, string>("grant_type",    "authorization_code"),
                       new KeyValuePair<string, string>("code",          code),
                       new KeyValuePair<string, string>("redirect_uri",  _opts.RedirectUri),
                   })
        };

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var (accessToken, expiresIn, refreshToken) = await ParseTokenResponseAsync(response, ct);

        // Save user token
        var existing = await _db.WclUserTokens.FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (existing is null)
        {
            existing = new WclUserToken { UserId = userId };
            _db.WclUserTokens.Add(existing);
        }

        existing.AccessToken = accessToken;
        existing.WclRefreshToken = refreshToken ?? string.Empty;
        existing.ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
        existing.LastRefreshedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<string> GetUserTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var token = await _db.WclUserTokens
                    .FirstOrDefaultAsync(t => t.UserId == userId, ct)
                    ?? throw new InvalidOperationException(
                        $"User {userId} has not authorized WarcraftLogs access. Call GET /api/wcl-auth/authorize first.");
        if (!token.IsExpired)
            return token.AccessToken;

        // Token expired — refresh token
        if (string.IsNullOrEmpty(token.WclRefreshToken))
            throw new InvalidOperationException(
                "WCL token expired and no refresh token available. Re-authorization required.");

        var credentials = Convert.ToBase64String(
               Encoding.UTF8.GetBytes($"{_opts.ClientId}:{_opts.ClientSecret}"));

        var request = new HttpRequestMessage(HttpMethod.Post, _opts.TokenEndpoint)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Basic", credentials) },
            Content = new FormUrlEncodedContent(new[]
                   {
                       new KeyValuePair<string, string>("grant_type",    "refresh_token"),
                       new KeyValuePair<string, string>("refresh_token", token.WclRefreshToken),
                   })
        };

        var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
        {
            // Refresh token inválido — remover e exigir re-autorização
            _db.WclUserTokens.Remove(token);
            await _db.SaveChangesAsync(ct);
            throw new InvalidOperationException(
                "WCL refresh token is invalid or expired. Re-authorization required.");
        }

        var (accessToken, expiresIn, newRefresh) = await ParseTokenResponseAsync(response, ct);

        token.AccessToken = accessToken;
        token.ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
        token.LastRefreshedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(newRefresh))
            token.WclRefreshToken = newRefresh;

        await _db.SaveChangesAsync(ct);

        return token.AccessToken;
    }

    public Task<bool> IsUserAuthorizedAsync(Guid userId, CancellationToken ct = default)
           => _db.WclUserTokens.AnyAsync(t => t.UserId == userId, ct);


    public async Task RevokeUserTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var token = await _db.WclUserTokens.FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (token is null) return;
        _db.WclUserTokens.Remove(token);
        await _db.SaveChangesAsync(ct);
    }


    // Helpers

    private static async Task<(string AccessToken, int ExpiresIn, string? RefreshToken)>
        ParseTokenResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var json = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var accessToken = root.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("access_token missing in WCL response");
        var expiresIn = root.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 3600;
        var refreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;

        return (accessToken, expiresIn, refreshToken);
    }

    [LoggerMessage(LogLevel.Information, Message = "Token refreshed. Expires at {ExpiresAt}")]
    public partial void LogRefresh(DateTime ExpiresAt);
}
