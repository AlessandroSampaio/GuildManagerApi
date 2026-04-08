using GuildManagerApi.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GuildManagerApi.Application.DTOs;
using GuildManagerApi.Domain.Interfaces;

namespace GuildManagerApi.Api.Controllers;

[ApiController]
[Route("api/characters")]
[Produces("application/json")]
[Authorize]
public class CharactersController(
    ICharacterRepository characters,
    IPerformanceRepository performance,
    IRaiderIoService raiderIoService,
    IRaiderIoProfileRepository raiderIoProfileRepository) : ControllerBase
{
    private readonly ICharacterRepository _characters = characters;
    private readonly IPerformanceRepository _performance = performance;
    private readonly IRaiderIoService _raiderIoService = raiderIoService;
    private readonly IRaiderIoProfileRepository _raiderIoProfileRepository = raiderIoProfileRepository;


    /// <summary>
    /// Retorna detalhes de um personagem com histórico de performance.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CharacterDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCharacter([FromRoute] int id, CancellationToken ct)
    {
        var character = await _characters.GetByIdAsync(id, ct);
        if (character is null) return NotFound();

        var perf = await _performance.GetByCharacterAsync(id, ct);

        var dto = new CharacterDetailDto(
            character.Id,
            character.Name,
            character.Server,
            character.Class?.Name ?? "unknown",
            character.Guild?.Name,
            [..perf.Select(p => new PerformanceSummaryDto(
                p.Fight.ReportId,
                p.Fight.Name,
                p.Spec,
                p.Role,
                p.Amount,
                p.RankPercent
            ))]
        );

        return Ok(dto);
    }

    /// <summary>
    /// Busca o perfil Raider.IO do personagem (mythic_plus_best_runs:all, raid_progression)
    /// e atualiza o snapshot local vinculado ao personagem.
    /// Retorna IsFresh=true quando os dados vêm diretamente do Raider.IO,
    /// ou IsFresh=false quando são dados em cache (falha na comunicação com o Raider.IO).
    /// </summary>
    [HttpGet("{id:int}/raider-io/profile")]
    [ProducesResponseType(typeof(RaiderIoProfileResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetRaiderIoProfile([FromRoute] int id, CancellationToken ct)
    {
        var character = await _characters.GetByIdAsync(id, ct);
        if (character is null) return NotFound();

        var region = NormalizeRegion(character.Region);

        bool isFresh = false;
        try
        {
            var (statusCode, _) = await _raiderIoService.GetCharacterProfileAsync(
                region, character.Server ?? "", character.Name ?? "", id, ct);
            isFresh = statusCode is >= 200 and < 300;
        }
        catch
        {
            // isFresh permanece false — fallback para o snapshot em cache
        }

        var snapshot = await _raiderIoProfileRepository.GetSnapshotByCharacterIdAsync(id, ct);

        if (snapshot is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                "Raider.IO está indisponível e não há dados em cache para este personagem.");

        var snap = MapSnapshot(snapshot);
        return Ok(new RaiderIoProfileResponseDto(
            isFresh,
            character.Id,
            character.Name ?? string.Empty,
            character.Server ?? string.Empty,
            character.Region ?? string.Empty,
            character.Class?.Name,
            character.Guild?.Name,
            character.PlayerId,
            character.Player?.Name,
            snap.ThumbnailUrl,
            snap.LastCrawledAt,
            snap.CachedAt,
            snap.Score,
            snap.MythicRuns,
            snap.RaidProgressions
        ));
    }

    private static RaiderIoSnapshotDto MapSnapshot(
        GuildManagerApi.Domain.Entities.RaiderIoCharacterSnapshot s) =>
        new(
            s.ThumbnailUrl,
            s.LastCrawledAt,
            s.CachedAt,
            Math.Round(s.MythicRuns.Sum(r => r.Score), 2, MidpointRounding.AwayFromZero),
            [..s.MythicRuns.Select(r => new RaiderIoMythicRunDto(
                r.Dungeon,
                r.ShortName,
                r.MythicLevel,
                r.CompletedAt,
                r.Score,
                r.IconUrl,
                r.BackgroundImageUrl,
                [..r.Affixes.Select(a => new RaiderIoAffixDto(a.AffixId, a.Name, a.IconUrl))]
            ))],
            [..s.RaidProgressions.Select(p => new RaiderIoRaidProgressionDto(
                p.RaidSlug,
                p.Summary,
                p.ExpansionId,
                p.TotalBosses,
                p.NormalBossesKilled,
                p.HeroicBossesKilled,
                p.MythicBossesKilled
            ))]
        );

    /// <summary>
    /// Busca characters por nome (substring) e/ou classe, paginado.
    /// Retorna o player vinculado se houver.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResult<CharacterSearchResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? q = null,
        [FromQuery] string? className = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);
        page = Math.Max(1, page);

        var (items, total) = await _characters.SearchAsync(q, className, page, pageSize, ct);

        var dtos = items.Select(c => new CharacterSearchResultDto(
            c.Id,
            c.Name,
            c.Server,
            c.Class?.Name ?? "unknown",
            c.Guild?.Name,
            c.PlayerId,
            c.Player?.Name
        )).ToList();

        return Ok(new PagedResult<CharacterSearchResultDto>(dtos, page, pageSize, total));
    }

    private static readonly Dictionary<string, string> _regionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "United States", "us" },
        { "Europe",        "eu" },
        { "Korea",         "kr" },
        { "Taiwan",        "tw" },
        { "China",         "cn" },
    };

    private static string NormalizeRegion(string? region)
        => region is not null && _regionMap.TryGetValue(region.Trim(), out var code)
            ? code
            : region?.Trim().ToLower() ?? "";
}
