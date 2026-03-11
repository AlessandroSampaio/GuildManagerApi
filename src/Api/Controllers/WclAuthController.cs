using System.Security.Claims;
using GuildManagerApi.Application.DTOs;
using GuildManagerApi.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GuildManagerApi.Api.Controllers;


[ApiController]
[Route("api/wcl-auth")]
[Produces("application/json")]
public class WclAuthController(IWclTokenService wclTokenService, IMemoryCache cache, IOptions<WclAuthOptions> opts) : ControllerBase
{
    private readonly IWclTokenService _wclTokenService = wclTokenService;
    private readonly IMemoryCache _cache = cache;
    private const string StatePrefix = "wcl_oauth_state:";
    private readonly WclAuthOptions _opts = opts.Value;


    /// <summary>
    /// Starts the OAuth flow: returns an  WarcraftLogs authorize URL.
    /// User must open that url on browser to allow access.
    /// </summary>
    [HttpGet("authorize")]
    [Authorize]
    [ProducesResponseType(typeof(WclAuthorizeResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Authorize(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var (authorizeUrl, state) = await _wclTokenService.BuildAuthorizeUrl(ct);

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
        var frontendBase = _opts.FrontendCallbackUrl.TrimEnd('/');

        // Usuário negou o acesso
        if (!string.IsNullOrEmpty(error))
            return RedirectToFrontend(frontendBase, false,
                $"WarcraftLogs negou o acesso: {error}");

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            return RedirectToFrontend(frontendBase, false,
                "Parâmetros obrigatórios ausentes (code/state).");

        // Valida o state para prevenir CSRF
        var cacheKey = $"{StatePrefix}{state}";
        if (!_cache.TryGetValue(cacheKey, out Guid userId))
            return RedirectToFrontend(frontendBase, false,
                "State inválido ou expirado. Reinicie o fluxo de autorização.");

        _cache.Remove(cacheKey);

        try
        {
            await _wclTokenService.ExchangeCodeAsync(userId, code, ct);
            return RedirectToFrontend(frontendBase, true, "Autorização concluída com sucesso.");
        }
        catch (Exception ex)
        {
            return RedirectToFrontend(frontendBase, false,
                $"Falha ao trocar código por token: {ex.Message}");
        }
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

    private RedirectResult RedirectToFrontend(string baseUrl, bool success, string message)
    {
        var encoded = Uri.EscapeDataString(message);
        // HashRouter do SolidJS usa #/ — a rota /wcl-callback fica em /#/wcl-callback
        var url = $"{baseUrl}/#/wcl-callback?success={success.ToString().ToLower()}&message={encoded}";
        return Redirect(url);
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
        return sub == null ? null : Guid.Parse(sub);
    }

}
