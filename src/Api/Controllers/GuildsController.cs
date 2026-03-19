
using GuildManagerApi.Application.DTOs;
using GuildManagerApi.Application.Services;
using GuildManagerApi.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GuildManagerApi.Api.Controllers;

[ApiController]
[Route("api/guilds")]
[Produces("application/json")]
[Authorize]
public class GuildsController(IGuildRepository guilds, ICharacterRepository characters, IGuildSyncService guildSync) : ControllerBase
{
    private readonly IGuildRepository _guilds = guilds;
    private readonly ICharacterRepository _characters = characters;
    private readonly IGuildSyncService _guildSync = guildSync;


    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GuildDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGuild([FromRoute] int id, CancellationToken ct)
    {
        var guild = await _guilds.GetByIdAsync(id, ct);
        if (guild is null) return NotFound();

        return Ok(new GuildDto(guild.Id, guild.Name, guild.Server, guild.Region));
    }

    [HttpGet("{id:int}/reports")]
    [ProducesResponseType(typeof(IEnumerable<ReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGuildReports(
        [FromRoute] int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var guild = await _guilds.GetByIdAsync(id, ct);
        if (guild is null) return NotFound();

        pageSize = Math.Clamp(pageSize, 1, 100);
        var reports = await _guilds.GetReportsByGuildAsync(id, page, pageSize, ct);

        var dtos = reports.Select(r => new ReportDto(
            r.Id, r.Title, r.StartTime, r.EndTime,
            guild.Name, r.Fights.Count, r.ImportedAt, r.LastSyncedAt));

        return Ok(dtos);
    }

    [HttpGet("{id:int}/characters")]
    [ProducesResponseType(typeof(IEnumerable<CharacterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGuildCharacters([FromRoute] int id, CancellationToken ct)
    {
        var guild = await _guilds.GetByIdAsync(id, ct);
        if (guild is null) return NotFound();

        var chars = await _characters.GetByGuildAsync(id, ct);
        var dtos = chars.Select(c => new CharacterDto(c.Id, c.Name, c.Server, c.Class?.Name ?? "unknown", guild.Name));
        return Ok(dtos);
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<GuildListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGuilds(
           [FromQuery] int page = 1,
           [FromQuery] int pageSize = 20,
           CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);

        var guilds = await _guilds.GetAllAsync(page, pageSize, ct);
        var total = await _guilds.CountAsync(ct);

        // Character count: carregado pelo Include na GuildRepository.GetAllAsync
        // Report count:    consultado via navigation
        var dtos = guilds.Select(g => new GuildListDto(
            g.Id,
            g.Name,
            g.Server,
            g.Region,
            g.Characters.Count,
            g.Reports.Count
        )).ToList();

        return Ok(new PagedResult<GuildListDto>(dtos, page, pageSize, total));
    }

    [HttpGet("{id:int}/roster")]
    [ProducesResponseType(typeof(IEnumerable<RosterEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGuildRoster([FromRoute] int id, CancellationToken ct)
    {
        var guild = await _guilds.GetByIdAsync(id, ct);
        if (guild is null) return NotFound();

        var chars = await _characters.GetByGuildAsync(id, ct);
        var dtos = chars.Select(c => new RosterEntryDto(
            CharacterId: c.Id,
            CharacterName: c.Name,
            Server: c.Server,
            Region: c.Region,
            Class: c.Class?.Name ?? "unknown",
            PlayerId: c.PlayerId,
            PlayerName: c.Player?.Name
        ));

        return Ok(dtos);
    }

    [HttpPost("{id:int}/sync-characters")]
    [ProducesResponseType(typeof(GuildSyncResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SyncCharacters([FromRoute] int id, CancellationToken ct)
    {
        var userId = GetUserId();
        try
        {
            var result = await _guildSync.SyncCharactersAsync(id, userId, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
