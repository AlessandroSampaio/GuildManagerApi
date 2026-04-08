using GuildManagerApi.Application.Auth;
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
public class GuildsController(
    IGuildRepository guilds,
    ICharacterRepository characters,
    IGuildSyncQueue syncQueue,
    GuildSyncHub syncHub,
    IJwtService jwtService) : ControllerBase
{
    private readonly IGuildRepository    _guilds    = guilds;
    private readonly ICharacterRepository _characters = characters;
    private readonly IGuildSyncQueue     _syncQueue  = syncQueue;
    private readonly GuildSyncHub        _syncHub    = syncHub;
    private readonly IJwtService         _jwtService = jwtService;


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
        var total  = await _guilds.CountAsync(ct);

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
            CharacterId:   c.Id,
            CharacterName: c.Name,
            Server:        c.Server,
            Region:        c.Region,
            Class:         c.Class?.Name ?? "unknown",
            PlayerId:      c.PlayerId,
            PlayerName:    c.Player?.Name
        ));

        return Ok(dtos);
    }

    /// <summary>
    /// Enfileira a sincronização de personagens da guilda para execução em background.
    /// Conecte-se ao WebSocket em <c>GET /api/guilds/{id}/sync/ws</c> para acompanhar o progresso.
    /// </summary>
    [HttpPost("{id:int}/sync-characters")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SyncCharacters([FromRoute] int id, CancellationToken ct)
    {
        var guild = await _guilds.GetByIdAsync(id, ct);
        if (guild is null) return NotFound(new { message = $"Guild {id} not found." });

        var userId = GetUserId();
        await _syncQueue.EnqueueAsync(new GuildSyncJob(id, userId), ct);

        return Accepted(new
        {
            message  = $"Sincronização da guilda \"{guild.Name}\" iniciada.",
            wsUrl    = $"/api/guilds/{id}/sync/ws"
        });
    }

    /// <summary>
    /// WebSocket — transmite eventos de progresso da sincronização em tempo real.
    /// Conecte-se antes ou durante a execução; a conexão permanece aberta até o processo concluir.
    /// </summary>
    [HttpGet("{id:int}/sync/ws")]
    public async Task SyncProgressWebSocket(
        [FromRoute] int id,
        [FromQuery] string? access_token,
        CancellationToken ct)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (string.IsNullOrWhiteSpace(access_token) || _jwtService.ValidateToken(access_token) is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var guild = await _guilds.GetByIdAsync(id, ct);
        if (guild is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        var socket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        _syncHub.Register(id, socket);
        await _syncHub.WaitForCloseAsync(socket, ct);
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
