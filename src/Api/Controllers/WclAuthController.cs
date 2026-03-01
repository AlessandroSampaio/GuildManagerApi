using System.Security.Claims;
using GuildManagerApi.Application.DTOs;
using GuildManagerApi.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace GuildManagerApi.Api.Controllers;


[ApiController]
[Route("api/wcl-auth")]
[Produces("application/json")]
public class WclAuthController(IWclTokenService wclTokenService, IMemoryCache cache) : ControllerBase
{
    private readonly IWclTokenService _wclTokenService = wclTokenService;
    private readonly IMemoryCache _cache = cache;
    private const string StatePrefix = "wcl_oauth_state:";


    /// <summary>
    /// Starts the OAuth flow: returns an  WarcraftLogs authorize URL.
    /// User must open that url on browser to allow access.
    /// </summary>
    [HttpGet("authorize")]
    [Authorize]
    [ProducesResponseType(typeof(WclAuthorizeResponseDto), StatusCodes.Status200OK)]
    public IActionResult Authorize()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var (authorizeUrl, state) = _wclTokenService.BuildAuthorizeUrl();

        // Save state + userId e cache for 10min so it can be validated in the callback
        _cache.Set($"{StatePrefix}{state}", userId.Value,
            TimeSpan.FromMinutes(10));

        return Ok(new WclAuthorizeResponseDto(authorizeUrl, state,
            "Abra a URL no browser para autorizar o acesso ao WarcraftLogs."));
    }

    /// <summary>
    /// Callback from WarcraftLogs OAuth flow.
    /// </summary>
    [HttpGet("callback")]
    [ProducesResponseType(typeof(WclCallbackResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Callback(
        [FromQuery] string code,
        [FromQuery] string state,
        [FromQuery] string? error = null,
        CancellationToken ct = default)
    {
        // User denied
        if (!string.IsNullOrEmpty(error))
            return BadRequest(new { error = $"WarcraftLogs authorization denied: {error}" });

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            return BadRequest(new { error = "Missing code or state parameter." });

        // Validate the cached state to prevent  CSRF
        var cacheKey = $"{StatePrefix}{state}";
        if (!_cache.TryGetValue(cacheKey, out Guid userId))
            return BadRequest(new { error = "Invalid or expired state. Please restart the authorization flow." });

        _cache.Remove(cacheKey);

        var token = await _wclTokenService.ExchangeCodeAsync(userId, code, ct);

        return Ok(new WclCallbackResponseDto(
            Message: "WarcraftLogs access authorized successfully.",
            UserId: userId,
            ExpiresAt: token.ExpiresAt,
            HasRefreshToken: !string.IsNullOrEmpty(token.WclRefreshToken)
        ));
    }

    [HttpGet("status")]
    [Authorize]
    [ProducesResponseType(typeof(WclStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var authorized = await _wclTokenService.IsUserAuthorizedAsync(userId.Value, ct);
        return Ok(new WclStatusDto(userId.Value, authorized,
            authorized ? "WarcraftLogs access is active." : "Not authorized. Call GET /api/wcl-auth/authorize."));
    }

    [HttpDelete("revoke")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Revoke(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        await _wclTokenService.RevokeUserTokenAsync(userId.Value, ct);
        return NoContent();
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
        return sub == null ? null : Guid.Parse(sub);
    }

}
