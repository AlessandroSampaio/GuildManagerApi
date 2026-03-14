using GuildManagerApi.Application.DTOs;
using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Enums;
using GuildManagerApi.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GuildManagerApi.Api.Controllers;

[ApiController]
[Route("api/player-scoring")]
[Produces("application/json")]
[Authorize(Policy = "MemberOrAdmin")]
public class PlayerScoringController(
        IPerformanceRepository performance,
        IReportRepository reports,
        IScoringSettingsRepository scoring,
        IRaidWeekRepository raidWeeks,
        IPenaltyRepository penalties) : ControllerBase
{
    private const int MaxReportCodes = 20;

    private readonly IPerformanceRepository _performance = performance;
    private readonly IReportRepository _reports = reports;
    private readonly IScoringSettingsRepository _scoring = scoring;
    private readonly IRaidWeekRepository _raidWeeks = raidWeeks;
    private readonly IPenaltyRepository _penalties = penalties;

    [HttpPost]
    [ProducesResponseType(typeof(PlayerScoringResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Calculate(
            [FromBody] PlayerScoringRequest request,
            CancellationToken ct)
    {
        if (request.ReportCodes is null || request.ReportCodes.Count == 0)
            return BadRequest(new { error = "At least one report code is required." });

        if (request.ReportCodes.Count > MaxReportCodes)
            return BadRequest(new { error = $"Maximum of {MaxReportCodes} report codes per request." });

        var codes = request.ReportCodes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct()
            .ToList();

        if (codes.Count == 0)
            return BadRequest(new { error = "No valid report codes provided." });

        return await RunCalculation(codes, raidWeek: null, weekPenalties: [], ct);
    }

    // ── Endpoint: por semana de raid ──────────────────────────────────────────

    /// <summary>
    /// Calcula a pontuação de performance dos Players para todos os reports
    /// associados a uma <see cref="RaidWeek"/> registrada.
    ///
    /// O resultado inclui o campo <c>raidWeek</c> com o contexto da semana calculada.
    ///
    /// Retorna 404 se a semana não existir ou não tiver nenhum report associado.
    /// </summary>
    [HttpPost("by-week/{raidWeekId:int}")]
    [ProducesResponseType(typeof(PlayerScoringResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CalculateByWeek(
        [FromRoute] int raidWeekId,
        CancellationToken ct)
    {
        var week = await _raidWeeks.GetByIdAsync(raidWeekId, ct);

        if (week is null)
            return NotFound(new { error = $"Raid week {raidWeekId} not found." });

        var codes = week.ReportEntries.Select(r => r.ReportCode).ToList();

        if (codes.Count == 0)
            return NotFound(new
            {
                error = $"Raid week '{week.Label}' has no reports associated.",
                raidWeekId = week.Id,
                suggestion = $"Add reports via POST /api/raid-weeks/{week.Id}/reports/{{reportCode}}"
            });

        var weekPenalties = await _penalties.GetPenaltiesByWeekAsync(week.Id, ct);
        return await RunCalculation(codes, raidWeek: week, weekPenalties: [.. weekPenalties], ct);
    }

    // ── Core calculation (shared by both endpoints) ───────────────────────────

    private async Task<IActionResult> RunCalculation(
        List<string> codes,
        RaidWeek? raidWeek,
        List<PlayerWeekPenalty> weekPenalties,
        CancellationToken ct)
    {
        // 1. Verificar ScoringSettings
        var settings = await _scoring.GetAsync(ct);
        if (settings is null || settings.Tiers.Count == 0)
            return NotFound(new
            {
                error = "Scoring settings have not been configured. " +
                        "Call PUT /api/admin/scoring-settings first."
            });

        // 2. Classificar reports: Done (incluídos no cálculo) vs demais (sinalizados)
        var reportStatuses = new List<ReportStatusDto>();
        var scorableCodes = new List<string>();

        foreach (var code in codes)
        {
            var report = await _reports.GetByIdAsync(code, ct);

            if (report is null)
            {
                reportStatuses.Add(new ReportStatusDto(
                    ReportCode: code,
                    Title: "(not found)",
                    ImportStatus: "NotFound",
                    IncludedInScoring: false));
                continue;
            }

            var isDone = report.ImportStatus == ImportStatus.Done;

            reportStatuses.Add(new ReportStatusDto(
                ReportCode: code,
                Title: report.Title,
                ImportStatus: report.ImportStatus.ToString(),
                IncludedInScoring: isDone));

            if (isDone) scorableCodes.Add(code);
        }

        if (scorableCodes.Count == 0)
            return BadRequest(new
            {
                error = "None of the provided reports have ImportStatus = Done.",
                reports = reportStatuses
            });

        // 3. Carregar PerformanceEntries (somente characters com Player vinculado)
        var entries = (await _performance.GetByReportsAsync(scorableCodes, ct)).ToList();

        // 4. Calcular pontos por tier e agregar por Player
        var tiers = settings.Tiers.OrderBy(t => t.MinPercent).ToList();

        int totalEntries = entries.Count;
        int entriesWithoutRankPct = entries.Count(e => e.RankPercent is null);

        var playerGroups = entries
            .GroupBy(e => e.Character.Player!)
            .Select(playerGroup =>
            {
                var player = playerGroup.Key;

                var characterScores = playerGroup
                    .GroupBy(e => e.Character)
                    .Select(charGroup =>
                    {
                        var character = charGroup.Key;

                        var fightEntries = charGroup
                            .OrderBy(e => e.Fight.Report.StartTime)
                            .ThenBy(e => e.Fight.FightIndex)
                            .Select(e =>
                            {
                                var (points, label) = ResolvePoints(e.RankPercent, tiers);
                                return new FightScoreEntryDto(
                                    ReportCode: e.Fight.ReportId,
                                    ReportTitle: e.Fight.Report.Title,
                                    FightId: e.FightId,
                                    FightName: e.Fight.Name,
                                    Kill: e.Fight.Kill,
                                    Spec: e.Spec,
                                    Role: e.Role,
                                    Amount: e.Amount,
                                    RankPercent: e.RankPercent,
                                    Points: points,
                                    TierLabel: label
                                );
                            })
                            .ToList();

                        int charPoints = fightEntries.Sum(f => f.Points ?? 0);
                        var rankSamples = fightEntries
                                             .Where(f => f.RankPercent.HasValue)
                                             .Select(f => f.RankPercent!.Value)
                                             .ToList();
                        float? charAvgRank = rankSamples.Count > 0 ? rankSamples.Average() : null;

                        return new PlayerCharacterScoreDto(
                            CharacterId: character.Id,
                            CharacterName: character.Name,
                            Class: character.Class?.Name ?? "unknown",
                            Server: character.Server,
                            TotalPoints: charPoints,
                            AverageRankPercent: charAvgRank,
                            Fights: fightEntries
                        );
                    })
                    .OrderByDescending(c => c.TotalPoints)
                    .ToList();

                int performancePoints = characterScores.Sum(c => c.TotalPoints);
                int scored = characterScores.Sum(c => c.Fights.Count(f => f.Points.HasValue));
                int unscored = characterScores.Sum(c => c.Fights.Count(f => !f.Points.HasValue));
                var allRanks = characterScores
                                       .SelectMany(c => c.Fights)
                                       .Where(f => f.RankPercent.HasValue)
                                       .Select(f => f.RankPercent!.Value)
                                       .ToList();
                float? playerAvgRank = allRanks.Count > 0 ? allRanks.Average() : null;

                var playerPenalties = weekPenalties
                    .Where(p => p.PlayerId == player.Id)
                    .Select(p => new PlayerWeekPenaltyDto(
                        p.Id,
                        p.PlayerId,
                        p.Player.Name,
                        p.RaidWeekId,
                        p.PenaltyEventId,
                        p.PenaltyEvent.Description,
                        p.PenaltyEvent.Points,
                        p.Note,
                        p.CreatedAt))
                    .ToList();

                int penaltyPoints = playerPenalties.Sum(p => p.Points);
                int totalPoints = performancePoints - penaltyPoints;

                return new PlayerScoreDto(
                    PlayerId: player.Id,
                    PlayerName: player.Name,
                    PerformancePoints: performancePoints,
                    PenaltyPoints: penaltyPoints,
                    TotalPoints: totalPoints,
                    AverageRankPercent: playerAvgRank,
                    ScoredEntries: scored,
                    UnscoredEntries: unscored,
                    Characters: characterScores,
                    Penalties: playerPenalties
                );
            })
            .OrderByDescending(p => p.TotalPoints)
            .ThenBy(p => p.PlayerName)
            .ToList();

        // 5. Montar resposta
        var settingsDto = new ScoringSettingsDto(
            settings.UpdatedAt,
            [.. tiers.Select(t => new ScoringTierDto(
                t.Id, t.MinPercent, t.MaxPercent, t.Points, t.Label))]
        );

        var raidWeekContext = raidWeek is null ? null : new RaidWeekSummaryDto(
            raidWeek.Id,
            raidWeek.Label,
            raidWeek.StartsAt,
            raidWeek.EndsAt,
            raidWeek.ReportEntries.Count
        );

        return Ok(new PlayerScoringResult(
            Players: playerGroups,
            Reports: reportStatuses,
            ScoringSettings: settingsDto,
            RaidWeek: raidWeekContext,
            TotalEntriesEvaluated: totalEntries,
            EntriesWithoutRankPercent: entriesWithoutRankPct
        ));
    }

    private static (int? Points, string? Label) ResolvePoints(
         float? rankPercent,
         List<ScoringTier> tiers)
    {
        if (rankPercent is null) return (null, null);

        var tier = tiers.FirstOrDefault(t =>
            rankPercent >= t.MinPercent &&
            (rankPercent < t.MaxPercent || t.MaxPercent >= 100f));

        return tier is null ? (null, null) : (tier.Points, tier.Label);
    }
}
