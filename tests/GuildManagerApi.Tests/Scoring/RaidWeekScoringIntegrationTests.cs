using GuildManagerApi.Api.Controllers;
using GuildManagerApi.Application.DTOs;
using Xunit;
using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Enums;
using GuildManagerApi.Infrastructure.Data;
using GuildManagerApi.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GuildManagerApi.Tests.Scoring;

/// <summary>
/// Valida que um Player presente apenas no segundo Report de uma RaidWeek
/// é incluído no cálculo de pontos ao recalcular após a adição desse Report.
/// </summary>
public class RaidWeekScoringIntegrationTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static PlayerScoringController CreateController(AppDbContext ctx) =>
        new(
            new PerformanceRepository(ctx),
            new ReportRepository(ctx),
            new ScoringSettingsRepository(ctx),
            new RaidWeekRepository(ctx),
            new PenaltyRepository(ctx)
        );

    [Fact]
    public async Task NewPlayerInSecondReport_IsIncludedInRaidWeekScoring()
    {
        // ── Arrange ────────────────────────────────────────────────────────────

        await using var ctx = CreateContext();

        // Scoring: único tier cobrindo 0-100% → 10 pontos
        ctx.ScoringSettings.Add(new ScoringSettings
        {
            Id = 1,
            UpdatedAt = DateTime.UtcNow,
            Tiers =
            [
                new ScoringTier
                {
                    Id = 1,
                    MinPercent = 0f,
                    MaxPercent = 100f,
                    Points = 10,
                    Label = "All",
                    ScoringSettingsId = 1
                }
            ]
        });

        // RaidWeek
        ctx.RaidWeeks.Add(new RaidWeek
        {
            Id = 1,
            Label = "Semana 1",
            StartsAt = new DateTime(2026, 3, 17, 0, 0, 0, DateTimeKind.Utc), // terça-feira
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // Players
        ctx.Players.AddRange(
            new Player { Id = 1, Name = "PlayerA" },
            new Player { Id = 2, Name = "PlayerB" },
            new Player { Id = 3, Name = "PlayerC" }   // presente apenas no Report B
        );

        // Characters vinculados aos Players
        ctx.Characters.AddRange(
            new Character { Id = 1, Name = "CharA", WclActorId = 1, Server = "realm", Region = "us", ClassId = null, PlayerId = 1 },
            new Character { Id = 2, Name = "CharB", WclActorId = 2, Server = "realm", Region = "us", ClassId = null, PlayerId = 2 },
            new Character { Id = 3, Name = "CharC", WclActorId = 3, Server = "realm", Region = "us", ClassId = null, PlayerId = 3 }
        );

        // Reports importados com sucesso
        ctx.Reports.AddRange(
            new Report { Id = "REP_A", Title = "Report A", ImportStatus = ImportStatus.Done, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow },
            new Report { Id = "REP_B", Title = "Report B", ImportStatus = ImportStatus.Done, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow }
        );

        // Fights: um por report
        ctx.Fights.AddRange(
            new Fight { Id = 1, ReportId = "REP_A", FightIndex = 1, Name = "Ansurek", Kill = true, StartTimeMs = 0, EndTimeMs = 300_000 },
            new Fight { Id = 2, ReportId = "REP_B", FightIndex = 1, Name = "Ansurek", Kill = true, StartTimeMs = 0, EndTimeMs = 280_000 }
        );

        // PerformanceEntries: REP_A → CharA + CharB | REP_B → CharC
        ctx.PerformanceEntries.AddRange(
            new PerformanceEntry { Id = 1, FightId = 1, CharacterId = 1, Spec = "Fire",  Role = "dps",    Amount = 120_000, RankPercent = 80f },
            new PerformanceEntry { Id = 2, FightId = 1, CharacterId = 2, Spec = "Holy",  Role = "healer", Amount =  60_000, RankPercent = 75f },
            new PerformanceEntry { Id = 3, FightId = 2, CharacterId = 3, Spec = "Fury",  Role = "dps",    Amount = 115_000, RankPercent = 85f }
        );

        // RaidWeekReport: inicialmente apenas REP_A está associado à semana
        ctx.RaidWeekReports.Add(new RaidWeekReport
        {
            Id = 1,
            RaidWeekId = 1,
            ReportCode = "REP_A",
            AddedAt = DateTime.UtcNow
        });

        await ctx.SaveChangesAsync();

        var controller = CreateController(ctx);

        // ── Act 1: calcular pontos com apenas REP_A ────────────────────────────
        var result1 = await controller.CalculateByWeek(1, CancellationToken.None);
        var ok1 = Assert.IsType<OkObjectResult>(result1);
        var scoring1 = Assert.IsType<PlayerScoringResult>(ok1.Value);

        // Apenas PlayerA e PlayerB devem aparecer; PlayerC ainda não está na semana
        Assert.Contains(scoring1.Players, p => p.PlayerName == "PlayerA");
        Assert.Contains(scoring1.Players, p => p.PlayerName == "PlayerB");
        Assert.DoesNotContain(scoring1.Players, p => p.PlayerName == "PlayerC");

        // ── Act 2: adicionar REP_B à RaidWeek ─────────────────────────────────
        ctx.RaidWeekReports.Add(new RaidWeekReport
        {
            Id = 2,
            RaidWeekId = 1,
            ReportCode = "REP_B",
            AddedAt = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();

        // ── Act 3: recalcular — PlayerC deve agora estar incluso ───────────────
        var result2 = await controller.CalculateByWeek(1, CancellationToken.None);
        var ok2 = Assert.IsType<OkObjectResult>(result2);
        var scoring2 = Assert.IsType<PlayerScoringResult>(ok2.Value);

        Assert.Contains(scoring2.Players, p => p.PlayerName == "PlayerA");
        Assert.Contains(scoring2.Players, p => p.PlayerName == "PlayerB");
        Assert.Contains(scoring2.Players, p => p.PlayerName == "PlayerC");

        // PlayerC deve ter pontos calculados a partir do REP_B
        var playerCScore = scoring2.Players.Single(p => p.PlayerName == "PlayerC");
        Assert.True(playerCScore.TotalPoints > 0,
            $"PlayerC deveria ter pontos > 0, mas TotalPoints = {playerCScore.TotalPoints}");
        Assert.Equal(10, playerCScore.TotalPoints); // 1 fight × 10 pontos (tier único)

        // Deve haver 2 reports no resultado final
        Assert.Equal(2, scoring2.Reports.Count);
        Assert.All(scoring2.Reports, r => Assert.True(r.IncludedInScoring));
    }
}
