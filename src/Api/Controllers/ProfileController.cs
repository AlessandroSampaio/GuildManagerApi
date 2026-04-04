using System.Security.Claims;
using GuildManagerApi.Application.DTOs;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Auth;
using GuildManagerApi.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GuildManagerApi.Api.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]
[Produces("application/json")]
public class ProfileController(
    IBNetTokenService bnetTokenService,
    ICharacterRepository characterRepository,
    IMemoryCache cache,
    IOptions<BNetAuthOptions> opts,
    AppDbContext db) : ControllerBase
{
    private readonly IBNetTokenService _bnetTokenService = bnetTokenService;
    private readonly ICharacterRepository _characterRepository = characterRepository;
    private readonly IMemoryCache _cache = cache;
    private const string StatePrefix = "bnet_oauth_state:";
    private readonly BNetAuthOptions _opts = opts.Value;
    private readonly AppDbContext _db = db;

    /// <summary>
    /// Inicia o fluxo OAuth Battle.net: retorna a URL de autorização.
    /// O usuário deve abrir essa URL no browser para conceder acesso.
    /// </summary>
    [HttpGet("bnet/authorize")]
    [ProducesResponseType(typeof(BNetAuthorizeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> BNetAuthorize(CancellationToken ct)
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
    [HttpGet("bnet/callback")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public async Task<IActionResult> BNetCallback(
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
    [HttpGet("bnet/status")]
    [ProducesResponseType(typeof(BNetStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> BNetStatus(CancellationToken ct)
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
                : "Not authorized. Call GET /api/profile/bnet/authorize."));
    }

    /// <summary>
    /// Remove o token Battle.net do usuário autenticado.
    /// </summary>
    [HttpDelete("bnet/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> BNetRevoke(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        await _bnetTokenService.RevokeUserTokenAsync(userId.Value, ct);
        return NoContent();
    }

    /// <summary>
    /// Retorna todos os personagens vinculados ao Player do usuário autenticado.
    /// Passe includeRaiderIo=true para incluir o snapshot do Raider.IO de cada personagem.
    /// </summary>
    [HttpGet("characters")]
    [ProducesResponseType(typeof(IEnumerable<CharacterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCharacters(
        [FromQuery] bool includeRaiderIo = false,
        CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var baseQuery = _db.Players
            .Include(p => p.Characters)
                .ThenInclude(c => c.Class)
            .Include(p => p.Characters)
                .ThenInclude(c => c.Guild);

        IQueryable<GuildManagerApi.Domain.Entities.Player> playerQuery = baseQuery;

        if (includeRaiderIo)
            playerQuery = _db.Players
                .Include(p => p.Characters)
                    .ThenInclude(c => c.Class)
                .Include(p => p.Characters)
                    .ThenInclude(c => c.Guild)
                .Include(p => p.Characters)
                    .ThenInclude(c => c.RaiderIoSnapshot!)
                        .ThenInclude(s => s.MythicRuns)
                            .ThenInclude(r => r.Affixes);

        var player = await playerQuery
            .FirstOrDefaultAsync(p => p.AppUserId == userId.Value, ct);

        if (player is null)
            return NotFound("No player associated with this account.");

        var bnetLinked = await _db.BattleNetUserTokens.AnyAsync(t => t.UserId == userId.Value, ct);
        if (!bnetLinked)
            return BadRequest("Battle.net account not linked. Call GET /api/profile/bnet/authorize first.");

        var characters = player.Characters.Select(c => new CharacterDto(
            c.Id,
            c.Name,
            c.Server,
            c.Class?.Name ?? "Unknown",
            c.Guild?.Name,
            includeRaiderIo && c.RaiderIoSnapshot is not null
                ? MapSnapshot(c.RaiderIoSnapshot)
                : null
        ));

        return Ok(characters);
    }

    private static RaiderIoSnapshotDto MapSnapshot(GuildManagerApi.Domain.Entities.RaiderIoCharacterSnapshot s) =>
        new(
            s.ThumbnailUrl,
            s.LastCrawledAt,
            s.CachedAt,
            s.MythicRuns.Sum(r => r.Score),
            [..s.MythicRuns.Select(r => new RaiderIoMythicRunDto(
                r.KeystoneRunId,
                r.Dungeon,
                r.ShortName,
                r.MythicLevel,
                r.CompletedAt,
                r.Score,
                r.IconUrl,
                r.BackgroundImageUrl,
                [..r.Affixes.Select(a => new RaiderIoAffixDto(a.AffixId, a.Name, a.IconUrl))]
            ))]
        );

    /// <summary>
    /// Busca os personagens WoW da conta Battle.net do usuário e vincula os que correspondem a Characters existentes
    /// (via WclActorId == BNet character id) ao Player do usuário.
    /// </summary>
    [HttpPost("bnet/link-characters")]
    [ProducesResponseType(typeof(LinkCharactersResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LinkBNetCharacters(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var player = await _db.Players.FirstOrDefaultAsync(p => p.AppUserId == userId.Value, ct);
        if (player is null)
            return NotFound("No player associated with this account.");

        IReadOnlyList<long> bnetCharacterIds;
        try
        {
            bnetCharacterIds = await _bnetTokenService.FetchWowCharacterIdsAsync(userId.Value, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        if (bnetCharacterIds.Count == 0)
            return Ok(new LinkCharactersResponseDto(0, [], "No WoW characters found on this Battle.net account."));

        var matchedCharacters = (await _characterRepository.FindByWclActorIdsAsync(bnetCharacterIds, ct))
            .Where(c => c.PlayerId is null)
            .ToList();

        foreach (var character in matchedCharacters)
            character.PlayerId = player.Id;

        await _db.SaveChangesAsync(ct);

        var names = matchedCharacters.Select(c => c.Name).ToList();
        return Ok(new LinkCharactersResponseDto(
            names.Count,
            names,
            names.Count > 0
                ? $"Successfully linked {names.Count} character(s) to player '{player.Name}'."
                : "No new characters to link. All matching characters are already linked."));
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
