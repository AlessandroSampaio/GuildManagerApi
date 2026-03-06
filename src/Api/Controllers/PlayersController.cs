using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GuildManagerApi.Application.DTOs;
using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;

namespace GuildManagerApi.API.Controllers;

public record PlayerNameRequest(
    string Name
);

[ApiController]
[Route("api/players")]
[Produces("application/json")]
[Authorize]
public class PlayersController(
    IPlayerRepository players,
    ICharacterRepository characters) : ControllerBase
{
    private readonly IPlayerRepository _players = players;
    private readonly ICharacterRepository _characters = characters;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PlayerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlayers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);

        var players = await _players.GetAllAsync(page, pageSize, ct);
        var total = await _players.CountAsync(ct);

        var dtos = players.Select(p => new PlayerDto(
            p.Id,
            p.Name,
            p.Characters.Count,
            p.CreatedAt
        )).ToList();

        return Ok(new PagedResult<PlayerDto>(dtos, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PlayerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlayer([FromRoute] int id, CancellationToken ct)
    {
        var player = await _players.GetByIdAsync(id, ct);
        if (player is null) return NotFound();

        return Ok(ToDetailDto(player));
    }

    [HttpPost]
    [ProducesResponseType(typeof(PlayerDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePlayer(
        [FromBody] PlayerNameRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required." });

        if (request.Name.Length > 64)
            return BadRequest(new { error = "Name must be at most 64 characters." });

        var player = new Player
        {
            Name = request.Name.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var id = await _players.CreateAsync(player, ct);

        var created = await _players.GetByIdAsync(id, ct);
        return CreatedAtAction(nameof(GetPlayer), new { id }, ToDetailDto(created!));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PlayerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePlayer(
          [FromRoute] int id,
          [FromBody] PlayerNameRequest request,
          CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Name is required." });

        if (request.Name.Length > 64)
            return BadRequest(new { error = "Name must be at most 64 characters." });

        var updated = await _players.UpdateAsync(id, request.Name.Trim(), ct);
        if (!updated) return NotFound();

        var player = await _players.GetByIdAsync(id, ct);
        return Ok(ToDetailDto(player!));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePlayer([FromRoute] int id, CancellationToken ct)
    {
        var deleted = await _players.DeleteAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{id:int}/characters/{characterId:int}")]
    [ProducesResponseType(typeof(PlayerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddCharacter(
        [FromRoute] int id,
        [FromRoute] int characterId,
        CancellationToken ct)
    {
        var character = await _characters.GetByIdAsync(characterId, ct);
        if (character is null) return NotFound(new { error = $"Character {characterId} not found." });

        if (character.PlayerId.HasValue && character.PlayerId != id)
            return Conflict(new
            {
                error = $"Character {characterId} is already associated with player {character.PlayerId}.",
                currentPlayerId = character.PlayerId
            });

        var ok = await _players.AddCharacterAsync(id, characterId, ct);
        if (!ok) return NotFound(new { error = $"Player {id} not found." });

        var player = await _players.GetByIdAsync(id, ct);
        return Ok(ToDetailDto(player!));
    }

    [HttpDelete("{id:int}/characters/{characterId:int}")]
    [ProducesResponseType(typeof(PlayerDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveCharacter(
        [FromRoute] int id,
        [FromRoute] int characterId,
        CancellationToken ct)
    {
        var ok = await _players.RemoveCharacterAsync(id, characterId, ct);
        if (!ok) return NotFound(new
        {
            error = $"Character {characterId} is not associated with player {id}, or player does not exist."
        });

        var player = await _players.GetByIdAsync(id, ct);
        return Ok(ToDetailDto(player!));
    }

    private static PlayerDetailDto ToDetailDto(Player player) =>
        new(
            player.Id,
            player.Name,
            player.CreatedAt,
            player.UpdatedAt,
            [.. player.Characters.Select(c => new PlayerCharacterDto(
                c.Id,
                c.Name,
                c.Server,
                c.Region,
                c.Class?.Name ?? "unknown",
                c.Guild?.Name
            ))]
        );
}
