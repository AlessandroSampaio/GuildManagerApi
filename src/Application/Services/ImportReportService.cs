using GuildManagerApi.Application.DTOs;
using GuildManagerApi.Application.GraphQL;
using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GuildManagerApi.Application.Services;

public interface IImportReportService
{
    Task<ImportResultDto> ImportAsync(string reportCode, Guid? userId, IProgress<ImportProgressEvent>? progress = null, CancellationToken ct = default);
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

    public async Task<ImportResultDto> ImportAsync(string reportCode, Guid? userId, IProgress<ImportProgressEvent>? progress = null, CancellationToken ct = default)
    {
        LogStartingImport(reportCode);
        Report(progress, reportCode, ImportPhase.Started,
                 "Importação iniciada.");

        // Fetch report data from WCL
        Report(progress, reportCode, ImportPhase.FetchingReport,
            "Buscando dados do report no WarcraftLogs...");
        var wclReport = await _wclClient.GetReportAsync(reportCode, userId, ct);

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
        Report(progress, reportCode, ImportPhase.SavingReport,
            $"Salvando report \"{wclReport.Title}\" com {wclReport.Fights.Count} fights...");
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
        var classes = await _characters.GetClassesAsync(ct);

        var playerActors = wclReport.MasterData.Actors
            .Where(a => a.Type == "Player")
            .ToList();
        var characterIdMap = new Dictionary<int, int>(); // GameId -> localId

        foreach (var actor in playerActors)
        {
            var characterClass = classes.FirstOrDefault(c => c.SlugName.Equals(actor.SubType, StringComparison.InvariantCultureIgnoreCase));
            var character = new Character
            {
                WclActorId = actor.GameId,
                Name = actor.Name,
                Server = actor.Server ?? string.Empty,
                Class = characterClass,
                GuildId = guildId,
                Region = wclReport.Guild?.Server.Region.Name ?? string.Empty
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

        Report(progress, reportCode, ImportPhase.FetchingRankings,
                 $"Buscando rankings de {killFightIds.Count} kill(s)...",
                 new { killCount = killFightIds.Count });
        var rankingsMap = new Dictionary<int, List<WclPlayerRanking>>();

        if (killFightIds.Count > 0)
        {
            rankingsMap = await _wclClient.GetRankingsAsync(reportCode, killFightIds, userId, "dps", ct);
        }

        // Build and persist performance entries
        Report(progress, reportCode, ImportPhase.SavingPerformance,
                  "Processando e salvando dados de performance...");
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
                    RankPercent = (float)ranking.RankPercent,
                    TotalParses = ranking.TotalParses,
                    BestPercent = (float)ranking.BracketPercent,
                });
            }
        }

        if (performanceEntries.Count > 0)
            await _performance.BulkUpsertAsync(performanceEntries, ct);

        var result = new ImportResultDto(
            ReportCode: reportCode,
            Title: wclReport.Title,
            FightsImported: fights.Count,
            KillsImported: fights.Count(f => f.Kill == true),
            PlayersImported: characterIdMap.Count,
            PerformanceEntriesSaved: performanceEntries.Count,
            GuildName: wclReport.Guild?.Name
        );
        LogFinishImport(performanceEntries.Count);

        Report(progress, reportCode, ImportPhase.Completed,
                 $"Importação concluída. {performanceEntries.Count} entradas de performance salvas.",
                 result);

        return result;
    }

    private static void Report(
         IProgress<ImportProgressEvent>? progress,
         string reportCode,
         ImportPhase phase,
         string message,
         object? data = null)
    {
        progress?.Report(new ImportProgressEvent(reportCode, phase, message, data));
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
