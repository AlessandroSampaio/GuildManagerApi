
using GuildManagerApi.Application.DTOs;
using GuildManagerApi.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GuildManagerApi.Api.Controllers;

[ApiController]
[Route("api/guilds")]
[Produces("application/json")]
[Authorize]
public class GuildsController(IGuildRepository guilds, ICharacterRepository characters) : ControllerBase
{
    private readonly IGuildRepository _guilds = guilds;
    private readonly ICharacterRepository _characters = characters;


    /// <summary>
    /// Retorna detalhes de uma guilda.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(GuildDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGuild([FromRoute] int id, CancellationToken ct)
    {
        var guild = await _guilds.GetByIdAsync(id, ct);
        if (guild is null) return NotFound();

        return Ok(new GuildDto(guild.Id, guild.Name, guild.Server, guild.Region));
    }

    /// <summary>
    /// Lista reports de uma guilda (paginado).
    /// </summary>
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

    /// <summary>
    /// Lista personagens de uma guilda.
    /// </summary>
    [HttpGet("{id:int}/characters")]
    [ProducesResponseType(typeof(IEnumerable<CharacterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetGuildCharacters([FromRoute] int id, CancellationToken ct)
    {
        var guild = await _guilds.GetByIdAsync(id, ct);
        if (guild is null) return NotFound();

        var chars = await _characters.GetByGuildAsync(id, ct);
        var dtos = chars.Select(c => new CharacterDto(c.Id, c.Name, c.Server, c.Class.Name, guild.Name));
        return Ok(dtos);
    }
}
