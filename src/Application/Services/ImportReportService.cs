using GuildManagerApi.Application.DTOs;
using GuildManagerApi.Application.GraphQL;
using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GuildManagerApi.Application.Services;

public interface IImportReportService
{
    Task<ImportResultDto> ImportAsync(string reportCode, CancellationToken ct = default);
}

public partial class ImportReportService(
        IWclGraphQLClient wclClient,
        IReportRepository reports,
        ICharacterRepository characters,
        IGuildRepository guilds,
        IPerformanceRepository performance,
        ILogger<ImportReportService> logger) : IImportReportService
{
    private readonly IWclGraphQLClient _wclClient = wclClient;
    private readonly IReportRepository _reports = reports;
    private readonly ICharacterRepository _characters = characters;
    private readonly IGuildRepository _guilds = guilds;
    private readonly IPerformanceRepository _performance = performance;

    public async Task<ImportResultDto> ImportAsync(string reportCode, CancellationToken ct = default)
    {
        LogStartingImport(reportCode);

        // Fetch report data from WCL
        var wclReport = await _wclClient.GetReportAsync(reportCode, ct);

        // Upsert guild
        int? guildId = null;
        if (wclReport.Guild is not null)
        {
            Guild guild = new()
            {
                Name = wclReport.Guild.Name,
                Server = wclReport.Guild.Server.Name,
                Region = wclReport.Guild.Server.Region.Name
            };
            guildId = await _guilds.UpsertAsync(guild, ct);
            LogGuildUpdated(guild.Name, guildId);
        }

        // Build fights
        var fights = wclReport.Fights.Select(f => new Fight
        {
            FightIndex = f.Id,
            ReportId = reportCode,
            Name = f.Name,
            Kill = f.Kill,
            StartTimeMs = f.StartTime,
            EndTimeMs = f.EndTime,
            Difficulty = f.Difficulty
        }).ToList();

        // Upsert Report
        var report = new Report
        {
            Id = reportCode,
            Title = wclReport.Title,
            StartTime = DateTimeOffset.FromUnixTimeMilliseconds(wclReport.StartTime).UtcDateTime,
            EndTime = DateTimeOffset.FromUnixTimeMilliseconds(wclReport.EndTime).UtcDateTime,
            GuildId = guildId,
            Fights = fights,
            LastSyncedAt = DateTime.UtcNow
        };

        await _reports.UpsertAsync(report, ct);
        LogReportStatus(reportCode, fights.Count);

        //Upsert characters
        var playerActors = wclReport.MasterData.Actors
            .Where(a => a.Type == "Player")
            .ToList();
        var characterIdMap = new Dictionary<int, int>(); // wclActorId -> localId

        foreach (var actor in playerActors)
        {
            var character = new Character
            {
                WclActorId = actor.Id,
                Name = actor.Name,
                Server = actor.Server ?? string.Empty,
                // Class = actor.SubType ?? string.Empty,
                GuildId = guildId
            };
            var localId = await _characters.UpsertAsync(character, ct);
            characterIdMap[actor.Id] = localId;
        }
        LogRegisterUpdated(characterIdMap.Count, "Characters");

        //Fetch rankings for kill fights only
        var killFightIds = fights
                   .Where(f => f.Kill == true)
                   .Select(f => f.FightIndex)
                   .ToList();
        var rankingsMap = new Dictionary<int, List<WclPlayerRanking>>();

        if (killFightIds.Count > 0)
        {
            rankingsMap = await _wclClient.GetRankingsAsync(reportCode, killFightIds, "dps", ct);
        }

        // Build and persist performance entries
        var savedFights = (await _reports.GetByIdAsync(reportCode, ct))!.Fights.ToList();
        var performanceEntries = new List<PerformanceEntry>();

        foreach (var fight in savedFights)
        {
            if (!rankingsMap.TryGetValue(fight.FightIndex, out var fightRankings))
                continue;

            foreach (var ranking in fightRankings)
            {
                // Match by name — best effort, WCL doesn't return actorId in rankings
                var matchedChar = playerActors.FirstOrDefault(a =>
                    string.Equals(a.Name, ranking.Name, StringComparison.OrdinalIgnoreCase));

                if (matchedChar is null || !characterIdMap.TryGetValue(matchedChar.Id, out var charLocalId))
                    continue;

                performanceEntries.Add(new PerformanceEntry
                {
                    FightId = fight.Id,
                    CharacterId = charLocalId,
                    Spec = ranking.Spec,
                    Role = ranking.Role,
                    Amount = (float)ranking.Amount,
                    RankPercent = ranking.RankPercent.HasValue ? (float)ranking.RankPercent.Value : null,
                    TotalParses = ranking.TotalParses,
                    BestPercent = ranking.BestPercent.HasValue ? (float)ranking.BestPercent.Value : null
                });
            }
        }

        if (performanceEntries.Count > 0)
            await _performance.BulkUpsertAsync(performanceEntries, ct);

        LogFinishImport(performanceEntries.Count);

        return new ImportResultDto(
            ReportCode: reportCode,
            Title: wclReport.Title,
            FightsImported: fights.Count,
            KillsImported: fights.Count(f => f.Kill == true),
            PlayersImported: characterIdMap.Count,
            PerformanceEntriesSaved: performanceEntries.Count,
            GuildName: wclReport.Guild?.Name
        );
    }


    [LoggerMessage(LogLevel.Information, Message = "Starting import for report {ReportCode}")]
    private partial void LogStartingImport(string reportCode);

    [LoggerMessage(LogLevel.Information, Message = "Import complete: {Perfs} performance entries saved")]
    private partial void LogFinishImport(int Perfs);

    [LoggerMessage(LogLevel.Information, Message = "Report {ReportCode} upserted with {FightCount} fights")]
    private partial void LogReportStatus(string ReportCode, int FightCount);

    [LoggerMessage(LogLevel.Debug, Message = "Guild updated: {Guild} ({Id})")]
    private partial void LogGuildUpdated(string Guild, int? id);

    [LoggerMessage(LogLevel.Information, Message = "{count} {register} updated.")]
    private partial void LogRegisterUpdated(int count, string register);
}
