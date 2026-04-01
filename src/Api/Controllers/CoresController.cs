using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GuildManagerApi.Application.DTOs;
using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;

namespace GuildManagerApi.Api.Controllers;

[ApiController]
[Route("api/cores")]
[Produces("application/json")]
[Authorize]
public class CoresController(
    ICoreRepository cores,
    IGuildRepository guilds,
    IPlayerRepository players) : ControllerBase
{
    private readonly ICoreRepository _cores = cores;
    private readonly IGuildRepository _guilds = guilds;
    private readonly IPlayerRepository _players = players;

    /// <summary>
    /// Lista todos os cores paginados.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CoreDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(1, page);

        var items = await _cores.GetAllAsync(page, pageSize, ct);
        var total = await _cores.CountAsync(ct);

        var dtos = items.Select(c => new CoreDto(
            c.Id,
            c.Name,
            c.GuildId,
            c.Guild.Name,
            c.Players.Count,
            c.CreatedAt
        )).ToList();

        return Ok(new PagedResult<CoreDto>(dtos, page, pageSize, total));
    }

    /// <summary>
    /// Retorna detalhes de um core com seus players.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CoreDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var core = await _cores.GetByIdAsync(id, ct);
        if (core is null) return NotFound();

        return Ok(ToDetailDto(core));
    }

    /// <summary>
    /// Cria um novo core associado a uma guilda.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CoreDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CoreCreateRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required." });

        if (request.Name.Length > 128)
            return BadRequest(new { error = "Name must be at most 128 characters." });

        var guild = await _guilds.GetByIdAsync(request.GuildId, ct);
        if (guild is null) return NotFound(new { error = $"Guild {request.GuildId} not found." });

        var core = new Core
        {
            Name = request.Name.Trim(),
            GuildId = request.GuildId,
        };

        var id = await _cores.CreateAsync(core, ct);
        var created = await _cores.GetByIdAsync(id, ct);
        return CreatedAtAction(nameof(GetById), new { id }, ToDetailDto(created!));
    }

    /// <summary>
    /// Atualiza o nome de um core.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CoreDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] CoreUpdateRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required." });

        if (request.Name.Length > 128)
            return BadRequest(new { error = "Name must be at most 128 characters." });

        var updated = await _cores.UpdateAsync(id, request.Name.Trim(), ct);
        if (!updated) return NotFound();

        var core = await _cores.GetByIdAsync(id, ct);
        return Ok(ToDetailDto(core!));
    }

    /// <summary>
    /// Remove um core.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        var deleted = await _cores.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>
    /// Adiciona um player ao core.
    /// </summary>
    [HttpPost("{id:int}/players/{playerId:int}")]
    [ProducesResponseType(typeof(CoreDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddPlayer(
        [FromRoute] int id,
        [FromRoute] int playerId,
        CancellationToken ct)
    {
        var player = await _players.GetByIdAsync(playerId, ct);
        if (player is null) return NotFound(new { error = $"Player {playerId} not found." });

        var ok = await _cores.AddPlayerAsync(id, playerId, ct);
        if (!ok) return NotFound(new { error = $"Core {id} not found." });

        var core = await _cores.GetByIdAsync(id, ct);
        return Ok(ToDetailDto(core!));
    }

    /// <summary>
    /// Remove um player do core.
    /// </summary>
    [HttpDelete("{id:int}/players/{playerId:int}")]
    [ProducesResponseType(typeof(CoreDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePlayer(
        [FromRoute] int id,
        [FromRoute] int playerId,
        CancellationToken ct)
    {
        var ok = await _cores.RemovePlayerAsync(id, playerId, ct);
        if (!ok) return NotFound(new { error = $"Player {playerId} is not a member of core {id}." });

        var core = await _cores.GetByIdAsync(id, ct);
        return Ok(ToDetailDto(core!));
    }

    private static CoreDetailDto ToDetailDto(Core core) =>
        new(
            core.Id,
            core.Name,
            core.GuildId,
            core.Guild.Name,
            core.CreatedAt,
            core.UpdatedAt,
            [.. core.Players.Select(cp => new CorePlayerDto(cp.PlayerId, cp.Player.Name))]
        );
}
