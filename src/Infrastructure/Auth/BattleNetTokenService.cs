using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GuildManagerApi.Infrastructure.Auth;

public class BNetAuthOptions
{
    public const string Section = "BattleNet";
    public string AuthorizeEndpoint { get; set; } = "https://oauth.battle.net/authorize";
    public string TokenEndpoint { get; set; } = "https://oauth.battle.net/token";
    public string UserInfoEndpoint { get; set; } = "https://oauth.battle.net/userinfo";
    /// <summary>Escopos necessários. wow.profile para dados de WoW; openid para obter battletag via userinfo.</summary>
    public string Scope { get; set; } = "wow.profile openid";
    /// <summary>URI de callback registrada no painel do Battle.net developer portal.</summary>
    public string RedirectUri { get; set; } = "http://localhost:5173/api/profile/bnet/callback";
    public string FrontendCallbackUrl { get; set; } = "http://localhost:1420";
}


public interface IBNetTokenService
{
    /// <summary>Token de aplicação via Client Credentials (para acesso a Game Data APIs públicas).</summary>
    Task<string> GetAppTokenAsync(CancellationToken ct = default);

    /// <summary>Gera a URL de autorização Battle.net para redirecionar o usuário.</summary>
    Task<(string AuthorizeUrl, string State)> BuildAuthorizeUrl(CancellationToken ct = default);

    /// <summary>Troca o authorization code por access token do usuário e busca BattleTag via /userinfo.</summary>
    Task<BattleNetUserToken> ExchangeCodeAsync(Guid userId, string code, CancellationToken ct = default);

    /// <summary>Retorna o access token do usuário. Battle.net não emite refresh tokens — lança exceção se expirado.</summary>
    Task<string> GetUserTokenAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Indica se o usuário já autorizou o acesso Battle.net.</summary>
    Task<bool> IsUserAuthorizedAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Remove o token Battle.net do usuário.</summary>
    Task RevokeUserTokenAsync(Guid userId, CancellationToken ct = default);
}

public partial class BattleNetTokenService(
    HttpClient httpClient,
    IOptions<BNetAuthOptions> opts,
    IBattleNetCredentialService credentials,
    AppDbContext context,
    ILogger<BattleNetTokenService> logger) : IBNetTokenService
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly BNetAuthOptions _opts = opts.Value;
    private readonly IBattleNetCredentialService _credentials = credentials;
    private readonly AppDbContext _db = context;
    private readonly ILogger<BattleNetTokenService> _logger = logger;

    // Cache do token de aplicação (Client Credentials, compartilhado)
    private string? _cachedAppToken;
    private DateTime _appTokenExpiresAt = DateTime.MinValue;
    private readonly SemaphoreSlim _appTokenLock = new(1, 1);

    public async Task<string> GetAppTokenAsync(CancellationToken ct = default)
    {
        if (_cachedAppToken is not null && DateTime.UtcNow < _appTokenExpiresAt)
            return _cachedAppToken;

        await _appTokenLock.WaitAsync(ct);
        try
        {
            _logger.LogInformation("Refreshing Battle.net OAuth app token...");

            var basicCreds = await BuildBasicCredentialsAsync(ct);

            var request = new HttpRequestMessage(HttpMethod.Post, _opts.TokenEndpoint)
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Basic", basicCreds) },
                Content = new FormUrlEncodedContent([
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                ])
            };

            var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);

            _cachedAppToken = doc.RootElement.GetProperty("access_token").GetString()
                ?? throw new InvalidOperationException("access_token not found in Battle.net response");

            var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();
            _appTokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);

            LogAppTokenRefreshed(_appTokenExpiresAt);
            return _cachedAppToken;
        }
        finally
        {
            _appTokenLock.Release();
        }
    }

    public async Task<(string AuthorizeUrl, string State)> BuildAuthorizeUrl(CancellationToken ct = default)
    {
        var clientId = await _credentials.GetClientIdAsync(ct);

        var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
            .Replace("+", "-").Replace("/", "_").Replace("=", "");

        var query = new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["scope"] = _opts.Scope,
            ["redirect_uri"] = _opts.RedirectUri,
            ["response_type"] = "code",
            ["state"] = state,
        };

        var qs = string.Join("&", query.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        return ($"{_opts.AuthorizeEndpoint}?{qs}", state);
    }

    public async Task<BattleNetUserToken> ExchangeCodeAsync(Guid userId, string code, CancellationToken ct = default)
    {
        var basicCreds = await BuildBasicCredentialsAsync(ct);

        var request = new HttpRequestMessage(HttpMethod.Post, _opts.TokenEndpoint)
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Basic", basicCreds) },
            Content = new FormUrlEncodedContent([
                new KeyValuePair<string, string>("grant_type",   "authorization_code"),
                new KeyValuePair<string, string>("code",         code),
                new KeyValuePair<string, string>("redirect_uri", _opts.RedirectUri),
            ])
        };

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var (accessToken, expiresIn) = await ParseTokenResponseAsync(response, ct);

        // Buscar BattleTag e Sub via /userinfo
        var (battleTag, sub) = await FetchUserInfoAsync(accessToken, ct);

        // Persistir token (upsert)
        var existing = await _db.BattleNetUserTokens.FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (existing is null)
        {
            existing = new BattleNetUserToken { UserId = userId };
            _db.BattleNetUserTokens.Add(existing);
        }

        existing.AccessToken = accessToken;
        existing.ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
        existing.LastRefreshedAt = DateTime.UtcNow;
        existing.BattleTag = battleTag;
        existing.Sub = sub;

        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<string> GetUserTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var token = await _db.BattleNetUserTokens
            .FirstOrDefaultAsync(t => t.UserId == userId, ct)
            ?? throw new InvalidOperationException(
                $"User {userId} has not authorized Battle.net access. Call GET /api/bnet-auth/authorize first.");

        if (!token.IsExpired)
            return token.AccessToken;

        // Battle.net não emite refresh tokens — remover e exigir re-autorização
        _db.BattleNetUserTokens.Remove(token);
        await _db.SaveChangesAsync(ct);
        throw new InvalidOperationException(
            "Battle.net token expired. Re-authorization required. Call GET /api/bnet-auth/authorize.");
    }

    public Task<bool> IsUserAuthorizedAsync(Guid userId, CancellationToken ct = default)
        => _db.BattleNetUserTokens.AnyAsync(t => t.UserId == userId, ct);

    public async Task RevokeUserTokenAsync(Guid userId, CancellationToken ct = default)
    {
        var token = await _db.BattleNetUserTokens.FirstOrDefaultAsync(t => t.UserId == userId, ct);
        if (token is null) return;
        _db.BattleNetUserTokens.Remove(token);
        await _db.SaveChangesAsync(ct);
    }


    // Helpers
    private async Task<string> BuildBasicCredentialsAsync(CancellationToken ct)
    {
        var clientId = await _credentials.GetClientIdAsync(ct);
        var clientSecret = await _credentials.GetClientSecretAsync(ct);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
    }

    private static async Task<(string AccessToken, int ExpiresIn)>
        ParseTokenResponseAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var json = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var accessToken = root.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("access_token missing in Battle.net response");
        var expiresIn = root.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 86400;

        return (accessToken, expiresIn);
    }

    /// <summary>
    /// Busca BattleTag e Sub numérico via GET /userinfo com o access token recém obtido.
    /// </summary>
    private async Task<(string? BattleTag, string? Sub)> FetchUserInfoAsync(string accessToken, CancellationToken ct)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, _opts.UserInfoEndpoint);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var res = await _httpClient.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode) return (null, null);

            var json = await res.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var battleTag = root.TryGetProperty("battletag", out var bt) ? bt.GetString() : null;
            var sub = root.TryGetProperty("sub", out var s) ? s.GetString() : null;

            return (battleTag, sub);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Battle.net userinfo. BattleTag will be null.");
            return (null, null);
        }
    }

    [LoggerMessage(LogLevel.Information, Message = "Battle.net app token refreshed. Expires at {ExpiresAt}")]
    public partial void LogAppTokenRefreshed(DateTime ExpiresAt);
}
