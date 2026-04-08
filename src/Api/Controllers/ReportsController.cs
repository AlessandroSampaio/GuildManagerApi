using System.Security.Claims;
using GuildManagerApi.Application.DTOs;
using GuildManagerApi.Application.Auth;
using GuildManagerApi.Application.Services;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GuildManagerApi.Domain.Entities;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text;

namespace GuildManagerApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class ReportsController(
        ImportProgressHub hub,
        IReportRepository reports,
        IPerformanceRepository performance,
        IImportQueue queue,
        IJwtService jwtService) : ControllerBase
{
    private readonly IImportQueue _queue = queue;
    private readonly ImportProgressHub _hub = hub;
    private readonly IReportRepository _reports = reports;
    private readonly IPerformanceRepository _performance = performance;
    private readonly IJwtService _jwtService = jwtService;
    private static readonly JsonSerializerOptions _jsonOpts =
            new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };



    [HttpPost("import/{reportCode}")]
    [ProducesResponseType(typeof(ImportAcceptedDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ImportReport([FromRoute] string reportCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reportCode) || reportCode.Length > 16)
            return BadRequest("Invalid report code");

        var userId = GetUserId();

        var existing = await _reports.GetByIdAsync(reportCode, ct);
        if (existing is { ImportStatus: ImportStatus.Importing or ImportStatus.Queued })
        {
            return Conflict(new
            {
                error = "This report is already being imported.",
                status = existing.ImportStatus.ToString()
            });
        }

        if (existing is null)
        {
            await _reports.UpsertAsync(new Report
            {
                Id = reportCode,
                Title = reportCode,               // placeholder até o worker buscar o título real
                ImportStatus = ImportStatus.Queued,
                ImportedAt = DateTime.UtcNow
            }, ct);
        }
        else
        {
            await _reports.SetImportStatusAsync(reportCode, ImportStatus.Queued, null, ct);
        }

        await _queue.EnqueueAsync(new ImportJob(reportCode, userId), ct);

        return AcceptedAtAction(
            nameof(GetReport),
            new { reportCode },
            new ImportAcceptedDto(
                ReportCode: reportCode,
                Status: "queued",
                WsUrl: $"/api/reports/{reportCode}/ws",
                Message: "Report enfileirado. Conecte-se ao WebSocket para acompanhar o progresso."
            ));
    }

    [HttpPost("{reportCode}/update")]
    [ProducesResponseType(typeof(ImportAcceptedDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateReport([FromRoute] string reportCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reportCode) || reportCode.Length > 16)
            return BadRequest("Invalid report code");

        var userId = GetUserId();

        var existing = await _reports.GetByIdAsync(reportCode, ct);
        if (existing is null)
            return NotFound();

        if (existing.ImportStatus is ImportStatus.Importing or ImportStatus.Queued)
        {
            return Conflict(new
            {
                error = "This report is already being imported.",
                status = existing.ImportStatus.ToString()
            });
        }

        await _reports.SetImportStatusAsync(reportCode, ImportStatus.Queued, null, ct);
        await _queue.EnqueueAsync(new ImportJob(reportCode, userId), ct);

        return AcceptedAtAction(
            nameof(GetReport),
            new { reportCode },
            new ImportAcceptedDto(
                ReportCode: reportCode,
                Status: "queued",
                WsUrl: $"/api/reports/{reportCode}/ws",
                Message: "Reimportação enfileirada. Conecte-se ao WebSocket para acompanhar o progresso."
            ));
    }

    [HttpGet("{reportCode}/ws")]
    [AllowAnonymous]
    public async Task StreamProgress(
            [FromRoute] string reportCode,
            [FromQuery] string? access_token,
            CancellationToken ct)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await HttpContext.Response.WriteAsync(
                "Requisição WebSocket esperada. Use ws:// ou wss://.", ct);
            return;
        }

        if (string.IsNullOrWhiteSpace(access_token) || _jwtService.ValidateToken(access_token) is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        // Aceitar a conexão WebSocket
        var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();

        // Verificar se o report existe
        var report = await _reports.GetByIdAsync(reportCode, ct);
        if (report is null)
        {
            await SendAndCloseAsync(ws, new
            {
                reportCode,
                phase = "Failed",
                phaseCode = 6,
                message = $"Report '{reportCode}' não encontrado.",
                timestamp = DateTime.UtcNow
            }, WebSocketCloseStatus.InvalidPayloadData, ct);
            return;
        }

        // Se já está concluído, enviar evento imediato e fechar
        if (report.ImportStatus == ImportStatus.Done)
        {
            await SendAndCloseAsync(ws, new
            {
                reportCode,
                phase = "Completed",
                phaseCode = 5,
                message = "Report já importado.",
                timestamp = DateTime.UtcNow
            }, WebSocketCloseStatus.NormalClosure, ct);
            return;
        }

        // Se falhou, reportar e fechar
        if (report.ImportStatus == ImportStatus.Failed)
        {
            await SendAndCloseAsync(ws, new
            {
                reportCode,
                phase = "Failed",
                phaseCode = 6,
                message = report.ImportError ?? "Importação falhou.",
                timestamp = DateTime.UtcNow
            }, WebSocketCloseStatus.NormalClosure, ct);
            return;
        }

        // Report está Queued ou Importing — registrar e aguardar eventos
        _hub.Register(reportCode, ws);
        await _hub.WaitForCloseAsync(ws, ct);
    }



    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReports(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var reports = await _reports.GetAllAsync(page, pageSize, ct);

        var dtos = reports.Select(r => new ReportDto(
            r.Id, r.Title, r.StartTime, r.EndTime,
            r.Guild?.Name, r.Fights.Count, r.ImportedAt, r.LastSyncedAt));

        return Ok(dtos);
    }


    /// <summary>
    /// Return a specifica report data.
    /// </summary>
    /// <param name="reportCode">O código do report.</param>
    /// <param name="ct"></param>
    /// <returns>Report data</returns>
    [HttpGet("{reportCode}")]
    [ProducesResponseType(typeof(ReportDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReport([FromRoute] string reportCode, CancellationToken ct)
    {
        var report = await _reports.GetByIdAsync(reportCode, ct);
        if (report is null) return NotFound();

        var dto = new ReportDetailDto(
            report.Id,
            report.Title,
            report.StartTime,
            report.EndTime,
            report.Guild is null ? null : new GuildDto(
                report.Guild.Id, report.Guild.Name, report.Guild.Server, report.Guild.Region),
            [..report.Fights.Select(f => new FightDto(
                f.Id, f.FightIndex, f.Name, f.Kill, f.DurationMs, f.Difficulty))],
            report.ImportedAt
        );

        return Ok(dto);
    }

    /// <summary>
    /// Return report performance data.
    /// </summary>
    /// <param name="reportCode">The report code</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>Performance data</returns>
    [HttpGet("{reportCode}/performance")]
    [ProducesResponseType(typeof(Dictionary<int, IEnumerable<PerformanceDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPerformance([FromRoute] string reportCode, CancellationToken ct)
    {
        var report = await _reports.GetByIdAsync(reportCode, ct);
        if (report is null) return NotFound();

        var result = new Dictionary<int, List<PerformanceDto>>();

        foreach (var fight in report.Fights)
        {
            var entries = await _performance.GetByFightAsync(fight.Id, ct);
            result[fight.Id] = [..entries.Select(p => new PerformanceDto(
                p.FightId,
                fight.Name,
                p.Character.Name,
                p.Spec,
                p.Role,
                p.Amount,
                p.RankPercent,
                p.BestPercent
            ))];
        }

        return Ok(result);
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
        return sub is null ? null : Guid.Parse(sub);
    }


    private static async Task SendAndCloseAsync(
           WebSocket ws,
           object payload,
           WebSocketCloseStatus closeStatus,
           CancellationToken ct)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload, _jsonOpts);
            var bytes = Encoding.UTF8.GetBytes(json);
            await ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
            await ws.CloseAsync(closeStatus, "done", ct);
        }
        catch { }
    }
}
