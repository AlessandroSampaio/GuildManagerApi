using System.Security.Claims;
using GuildManagerApi.Application.DTOs;
using GuildManagerApi.Application.Services;
using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GuildManagerApi.Api.Controllers;

[ApiController]
[Route("api/raid-weeks")]
[Produces("application/json")]
[Authorize]
public class RaidWeeksController(
        IRaidWeekRepository repository,
        IPenaltyRepository penalties,
        IPlayerRepository players,
        ICoreRepository cores,
        RaidWeekHub hub,
        IAuditService audit) : ControllerBase
{
    private readonly IRaidWeekRepository _repository = repository;
    private readonly IPenaltyRepository _penalties = penalties;
    private readonly IPlayerRepository _players = players;
    private readonly ICoreRepository _cores = cores;
    private readonly RaidWeekHub _hub = hub;
    private readonly IAuditService _audit = audit;



    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RaidWeekSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);

        var weeks = await _repository.GetAllAsync(page, pageSize, ct);
        var total = await _repository.CountAsync(ct);

        var dtos = weeks.Select(w => new RaidWeekSummaryDto(
            w.Id,
            w.Label,
            w.StartsAt,
            w.EndsAt,
            w.ReportEntries.Count,
            w.CoreId,
            w.Core?.Name
        )).ToList();

        return Ok(new PagedResult<RaidWeekSummaryDto>(dtos, page, pageSize, total));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(RaidWeekDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken ct)
    {
        var week = await _repository.GetByIdAsync(id, ct);
        if (week is null) return NotFound();
        return Ok(ToDto(week));
    }

    [HttpGet("by-date")]
    [ProducesResponseType(typeof(RaidWeekDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByDate(
            [FromQuery] DateTime date,
            CancellationToken ct)
    {
        var week = await _repository.GetByDateAsync(date, ct);
        if (week is null) return NotFound(new
        {
            message = $"No raid week registered for date {date:yyyy-MM-dd}. " +
                      $"The week would start on {RaidWeekRepository.TuesdayOf(date):yyyy-MM-dd} (Tuesday)."
        });
        return Ok(ToDto(week));
    }

    /// <summary>
    /// WebSocket — notifica em tempo real quando a semana de raid sofre alterações.
    /// Conecte-se a <c>GET /api/raid-weeks/{id}/ws</c> para receber eventos de mudança.
    /// </summary>
    [HttpGet("{id:int}/ws")]
    [AllowAnonymous]
    public async Task ChangesWebSocket([FromRoute] int id, CancellationToken ct)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await HttpContext.Response.WriteAsync(
                "Requisição WebSocket esperada. Use ws:// ou wss://.", ct);
            return;
        }

        var week = await _repository.GetByIdAsync(id, ct);
        if (week is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        _hub.Register(id, socket);
        await _hub.WaitForCloseAsync(socket, ct);
    }

    [HttpPost]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(RaidWeekDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
            [FromBody] CreateRaidWeekRequest request,
            CancellationToken ct)
    {
        if (!ValidateTuesdayRequest(request.StartsAt, out var normalizedStart, out var error))
            return BadRequest(new { error });

        if (string.IsNullOrWhiteSpace(request.Label))
            return BadRequest(new { error = "Label is required." });

        if (request.CoreId is not null)
        {
            var coreExists = await _cores.GetByIdAsync(request.CoreId.Value, ct);
            if (coreExists is null)
                return NotFound(new { error = $"Core {request.CoreId} not found." });
        }

        var week = new RaidWeek
        {
            Label = request.Label.Trim(),
            StartsAt = normalizedStart,
            CoreId = request.CoreId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        // Adicionar report codes iniciais
        if (request.ReportCodes is { Count: > 0 })
        {
            var distinctCodes = request.ReportCodes
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => c.Trim())
                .Distinct()
                .ToList();

            foreach (var code in distinctCodes)
                week.ReportEntries.Add(new RaidWeekReport
                {
                    ReportCode = code,
                    AddedAt = DateTime.UtcNow,
                });
        }

        var id = await _repository.CreateAsync(week, ct);
        var created = await _repository.GetByIdAsync(id, ct);

        return CreatedAtAction(nameof(GetById), new { id }, ToDto(created!));
    }



    [HttpPut("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(RaidWeekDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
            [FromRoute] int id,
            [FromBody] UpdateRaidWeekRequest request,
            CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Label))
            return BadRequest(new { error = "Label is required." });

        if (!ValidateTuesdayRequest(request.StartsAt, out var normalizedStart, out var error))
            return BadRequest(new { error });

        var updated = await _repository.UpdateAsync(id, request.Label.Trim(), normalizedStart, ct);
        if (!updated) return NotFound();

        var week = await _repository.GetByIdAsync(id, ct);
        _ = _hub.BroadcastAsync(id, "updated");
        return Ok(ToDto(week!));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        var deleted = await _repository.DeleteAsync(id, ct);
        if (!deleted) return NotFound();

        _ = _hub.BroadcastAsync(id, "deleted");
        return NoContent();
    }

    [HttpPost("{id:int}/reports/{reportCode}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(RaidWeekDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddReport(
            [FromRoute] int id,
            [FromRoute] string reportCode,
            CancellationToken ct)
    {
        var week = await _repository.GetByIdAsync(id, ct);
        if (week is null) return NotFound();

        var code = reportCode.Trim();

        if (week.ReportEntries.Any(r => r.ReportCode == code))
            return Conflict(new { error = $"Report '{code}' is already associated with this raid week." });

        await _repository.AddReportAsync(id, code, ct);

        var updated = await _repository.GetByIdAsync(id, ct);
        _ = _hub.BroadcastAsync(id, "reportAdded");
        return Ok(ToDto(updated!));
    }


    [HttpDelete("{id:int}/reports/{reportCode}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(RaidWeekDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveReport(
            [FromRoute] int id,
            [FromRoute] string reportCode,
            CancellationToken ct)
    {
        var removed = await _repository.RemoveReportAsync(id, reportCode.Trim(), ct);
        if (!removed) return NotFound(new
        {
            error = $"Report '{reportCode}' is not associated with raid week {id}."
        });

        var week = await _repository.GetByIdAsync(id, ct);
        _ = _hub.BroadcastAsync(id, "reportRemoved");
        await _audit.LogAsync("Report.RemovedFromWeek", "RaidWeek", id.ToString(),
            GetActorId(), GetActorUsername(),
            details: $"reportCode={reportCode.Trim()}", ct: ct);
        return Ok(ToDto(week!));
    }



    // ── Penalidades da semana ─────────────────────────────────────────────────

    /// <summary>
    /// Lista todas as penalidades aplicadas a players nessa semana de raid.
    /// </summary>
    [HttpGet("{id:int}/penalties")]
    [ProducesResponseType(typeof(IEnumerable<PlayerWeekPenaltyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPenalties([FromRoute] int id, CancellationToken ct)
    {
        var weekExists = await _repository.GetByIdAsync(id, ct);
        if (weekExists is null) return NotFound();

        var items = await _penalties.GetPenaltiesByWeekAsync(id, ct);
        return Ok(items.Select(ToPenaltyDto));
    }

    /// <summary>
    /// Aplica uma penalidade a um player na semana de raid informada.
    /// </summary>
    [HttpPost("{id:int}/penalties")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(PlayerWeekPenaltyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddPenalty(
        [FromRoute] int id,
        [FromBody] AddPlayerPenaltyRequest request,
        CancellationToken ct)
    {
        var week = await _repository.GetByIdAsync(id, ct);
        if (week is null) return NotFound(new { error = $"Raid week {id} not found." });

        var player = await _players.GetByIdAsync(request.PlayerId, ct);
        if (player is null) return NotFound(new { error = $"Player {request.PlayerId} not found." });

        if (week.CoreId is not null)
        {
            var isMember = await _cores.IsPlayerInCoreAsync(week.CoreId.Value, request.PlayerId, ct);
            if (!isMember)
                return UnprocessableEntity(new
                {
                    error = $"Player {request.PlayerId} is not a member of core {week.CoreId} associated with this raid week."
                });
        }

        var penaltyEvent = await _penalties.GetEventByIdAsync(request.PenaltyEventId, ct);
        if (penaltyEvent is null)
            return NotFound(new { error = $"Penalty event {request.PenaltyEventId} not found." });

        var penalty = new PlayerWeekPenalty
        {
            PlayerId = request.PlayerId,
            RaidWeekId = id,
            PenaltyEventId = request.PenaltyEventId,
            Note = request.Note?.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        var penaltyId = await _penalties.AddPlayerPenaltyAsync(penalty, ct);
        var created = await _penalties.GetPenaltyByIdAsync(penaltyId, ct);
        _ = _hub.BroadcastAsync(id, "penaltyAdded");
        await _audit.LogAsync("Penalty.Added", "RaidWeek", id.ToString(),
            GetActorId(), GetActorUsername(),
            details: $"playerId={request.PlayerId}, penaltyEventId={request.PenaltyEventId}", ct: ct);
        return Created($"api/raid-weeks/{id}/penalties", ToPenaltyDto(created!));
    }

    /// <summary>
    /// Remove uma penalidade aplicada a um player nessa semana de raid.
    /// </summary>
    [HttpDelete("{id:int}/penalties/{penaltyId:int}")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePenalty(
        [FromRoute] int id,
        [FromRoute] int penaltyId,
        CancellationToken ct)
    {
        var penalty = await _penalties.GetPenaltyByIdAsync(penaltyId, ct);
        if (penalty is null || penalty.RaidWeekId != id) return NotFound();

        await _penalties.RemovePlayerPenaltyAsync(penaltyId, ct);
        _ = _hub.BroadcastAsync(id, "penaltyRemoved");
        await _audit.LogAsync("Penalty.Removed", "RaidWeek", id.ToString(),
            GetActorId(), GetActorUsername(),
            details: $"penaltyId={penaltyId}", ct: ct);
        return NoContent();
    }

    // Helpers
    private Guid? GetActorId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private string? GetActorUsername() =>
        User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;

    private static RaidWeekDto ToDto(RaidWeek w) => new(
            w.Id,
            w.Label,
            w.StartsAt,
            w.EndsAt,
            w.CreatedAt,
            w.UpdatedAt,
            [.. w.ReportEntries.OrderBy(r => r.AddedAt).Select(r => r.ReportCode)],
            w.CoreId,
            w.Core?.Name
        );

    private static PlayerWeekPenaltyDto ToPenaltyDto(PlayerWeekPenalty p) => new(
        p.Id,
        p.PlayerId,
        p.Player.Name,
        p.RaidWeekId,
        p.PenaltyEventId,
        p.PenaltyEvent.Description,
        p.PenaltyEvent.Points,
        p.Note,
        p.CreatedAt
    );

    private static bool ValidateTuesdayRequest(
            DateTime input,
            out DateTime normalized,
            out string? error)
    {
        // Normalizar para 00:00 UTC (ignorar hora e timezone do input)
        normalized = input.Date.ToUniversalTime();

        if (normalized.DayOfWeek != DayOfWeek.Tuesday)
        {
            var suggestion = RaidWeekRepository.TuesdayOf(normalized);
            error = $"StartsAt must be a Tuesday. " +
                    $"'{input:yyyy-MM-dd}' is a {normalized.DayOfWeek}. " +
                    $"Did you mean '{suggestion:yyyy-MM-dd}'?";
            return false;
        }

        error = null;
        return true;
    }

}
