using GuildManagerApi.Application.DTOs;
using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GuildManagerApi.Api.Controllers;

/// <summary>
/// Gerencia os eventos de penalidade pré-cadastrados que podem ser associados
/// a players em uma semana de raid.
/// </summary>
[ApiController]
[Route("api/penalty-events")]
[Produces("application/json")]
[Authorize]
public class PenaltyEventsController(IPenaltyRepository repository) : ControllerBase
{
    private readonly IPenaltyRepository _repository = repository;

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PenaltyEventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var events = await _repository.GetAllEventsAsync(ct);
        return Ok(events.Select(ToDto));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PenaltyEventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var ev = await _repository.GetEventByIdAsync(id, ct);
        return ev is null ? NotFound() : Ok(ToDto(ev));
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(PenaltyEventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePenaltyEventRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(new { error = "Description is required." });

        if (request.Points < 0)
            return BadRequest(new { error = "Points must be greater or equal than zero." });

        var ev = new PenaltyEvent
        {
            Description = request.Description.Trim(),
            Points = request.Points,
            CreatedAt = DateTime.UtcNow,
        };

        var id = await _repository.CreateEventAsync(ev, ct);
        var created = await _repository.GetEventByIdAsync(id, ct);
        return CreatedAtAction(nameof(GetById), new { id }, ToDto(created!));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(PenaltyEventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdatePenaltyEventRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(new { error = "Description is required." });

        if (request.Points <= 0)
            return BadRequest(new { error = "Points must be greater than zero." });

        var updated = await _repository.UpdateEventAsync(id, request.Description.Trim(), request.Points, ct);
        if (!updated) return NotFound();

        var ev = await _repository.GetEventByIdAsync(id, ct);
        return Ok(ToDto(ev!));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        var deleted = await _repository.DeleteEventAsync(id, ct);
        return deleted ? NoContent() : NotFound();
    }

    private static PenaltyEventDto ToDto(PenaltyEvent e) =>
        new(e.Id, e.Description, e.Points, e.CreatedAt);
}
