using System.Security.Claims;
using GuildManagerApi.Application.DTOs;
using GuildManagerApi.Infrastructure.Auth;
using GuildManagerApi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GuildManagerApi.Api.Controllers;

[ApiController]
[Route("api/bnet-auth")]
[Produces("application/json")]
public class BattleNetAuthController(
    IBNetTokenService bnetTokenService,
    IMemoryCache cache,
    IOptions<BNetAuthOptions> opts,
    AppDbContext db) : ControllerBase
{
    private readonly IBNetTokenService _bnetTokenService = bnetTokenService;
    private readonly IMemoryCache _cache = cache;
    private const string StatePrefix = "bnet_oauth_state:";
    private readonly BNetAuthOptions _opts = opts.Value;
    private readonly AppDbContext _db = db;

    /// <summary>
    /// Inicia o fluxo OAuth Battle.net: retorna a URL de autorização.
    /// O usuário deve abrir essa URL no browser para conceder acesso.
    /// </summary>
    [HttpGet("authorize")]
    [Authorize]
    [ProducesResponseType(typeof(BNetAuthorizeResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Authorize(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var (authorizeUrl, state) = await _bnetTokenService.BuildAuthorizeUrl(ct);

        _cache.Set($"{StatePrefix}{state}", userId.Value, TimeSpan.FromMinutes(10));

        return Ok(new BNetAuthorizeResponseDto(authorizeUrl, state,
            "Abra a URL no browser para autorizar o acesso ao Battle.net."));
    }

    /// <summary>
    /// Callback do fluxo OAuth Battle.net. Troca o code pelo access token e busca o BattleTag.
    /// </summary>
    [HttpGet("callback")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> Callback(
        [FromQuery] string code,
        [FromQuery] string state,
        [FromQuery] string? error = null,
        CancellationToken ct = default)
    {
        var frontendBase = _opts.FrontendCallbackUrl.TrimEnd('/');

        if (!string.IsNullOrEmpty(error))
            return RedirectToFrontend(frontendBase, false,
                $"Battle.net negou o acesso: {error}");

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            return RedirectToFrontend(frontendBase, false,
                "Parâmetros obrigatórios ausentes (code/state).");

        var cacheKey = $"{StatePrefix}{state}";
        if (!_cache.TryGetValue(cacheKey, out Guid userId))
            return RedirectToFrontend(frontendBase, false,
                "State inválido ou expirado. Reinicie o fluxo de autorização.");

        _cache.Remove(cacheKey);

        try
        {
            await _bnetTokenService.ExchangeCodeAsync(userId, code, ct);
            return RedirectToFrontend(frontendBase, true, "Autorização Battle.net concluída com sucesso.");
        }
        catch (Exception ex)
        {
            return RedirectToFrontend(frontendBase, false,
                $"Falha ao trocar código por token: {ex.Message}");
        }
    }

    /// <summary>
    /// Retorna o status de autorização Battle.net do usuário autenticado, incluindo BattleTag se disponível.
    /// </summary>
    [HttpGet("status")]
    [Authorize]
    [ProducesResponseType(typeof(BNetStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var token = await _db.BattleNetUserTokens
            .FirstOrDefaultAsync(t => t.UserId == userId.Value, ct);

        var authorized = token is not null;
        return Ok(new BNetStatusDto(
            userId.Value,
            authorized,
            token?.BattleTag,
            authorized
                ? "Battle.net access is active."
                : "Not authorized. Call GET /api/bnet-auth/authorize."));
    }

    /// <summary>
    /// Remove o token Battle.net do usuário autenticado.
    /// </summary>
    [HttpDelete("revoke")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Revoke(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        await _bnetTokenService.RevokeUserTokenAsync(userId.Value, ct);
        return NoContent();
    }


    private RedirectResult RedirectToFrontend(string baseUrl, bool success, string message)
    {
        var encoded = Uri.EscapeDataString(message);
        var url = $"{baseUrl}/#/bnet-callback?success={success.ToString().ToLower()}&message={encoded}";
        return Redirect(url);
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
        return sub == null ? null : Guid.Parse(sub);
    }
}
