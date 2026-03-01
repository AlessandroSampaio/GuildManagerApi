using GuildManagerApi.Application.DTOs;
using GuildManagerApi.Application.Services;
using GuildManagerApi.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GuildManagerApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class ReportsController(
        IImportReportService importService,
        IReportRepository reports,
        IPerformanceRepository performance) : ControllerBase
{
    private readonly IImportReportService _importService = importService;
    private readonly IReportRepository _reports = reports;
    private readonly IPerformanceRepository _performance = performance;


    /// <summary>
    /// Imports a report by its code.
    /// </summary>
    /// <param name="reportCode">The code of the report to import.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The import result route</returns>
    [HttpPost("import/{reportCode}")]
    [ProducesResponseType(typeof(ImportResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportReport([FromRoute] string reportCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reportCode) || reportCode.Length > 16)
            return BadRequest("Invalid report code");

        var result = await _importService.ImportAsync(reportCode.Trim(), ct);
        return CreatedAtAction(nameof(GetReport), new { reportCode }, result);
    }

    /// <summary>
    /// Gets a list of reports.
    /// </summary>
    /// <param name="page">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A list of report DTOs.</returns>
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
            result[fight.FightIndex] = [..entries.Select(p => new PerformanceDto(
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
}
