using GuildManagerApi.Application.DTOs;
using GuildManagerApi.Application.GraphQL;
using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GuildManagerApi.Application.Services;

public interface IGuildSyncService
{
    Task<GuildSyncResultDto> SyncCharactersAsync(
        int guildId,
        Guid? userId,
        IProgress<GuildSyncProgressEvent>? progress = null,
        CancellationToken ct = default);
}

public class GuildSyncService(
    IGuildRepository guilds,
    ICharacterRepository characters,
    IWclGraphQLClient wclClient,
    ILogger<GuildSyncService> logger) : IGuildSyncService
{
    public async Task<GuildSyncResultDto> SyncCharactersAsync(
        int guildId,
        Guid? userId,
        IProgress<GuildSyncProgressEvent>? progress = null,
        CancellationToken ct = default)
    {
        var guild = await guilds.GetByIdAsync(guildId, ct)
            ?? throw new KeyNotFoundException($"Guild {guildId} not found.");

        progress?.Report(new GuildSyncProgressEvent(
            guildId, GuildSyncPhase.Started,
            $"Iniciando sincronização da guilda \"{guild.Name}\"."));

        var serverSlug = guild.Server.ToLowerInvariant().Replace(' ', '-');
        var allClasses = (await characters.GetClassesAsync(ct)).ToList();

        int synced  = 0;
        int skipped = 0;
        int page    = 1;
        int lastPage;

        do
        {
            // ── Busca página ───────────────────────────────────────────────────
            var (members, lp) = await wclClient.GetGuildMembersPageAsync(
                guild.Name, serverSlug, guild.Region,
                page, limit: 100, userId, ct);

            lastPage = lp;

            progress?.Report(new GuildSyncProgressEvent(
                guildId, GuildSyncPhase.FetchingPage,
                $"Página {page}/{lastPage} recebida — {members.Count} membro(s).",
                new { currentPage = page, lastPage, count = members.Count }));

            // ── Processa membros da página ─────────────────────────────────────
            foreach (var member in members)
            {
                // gameData.error: WCL sinalizou que não há dados válidos do jogo
                var gameDataError = WclGraphQLClient.GetGameDataError(member.GameData);
                if (gameDataError is not null)
                {
                    logger.LogWarning(
                        "Membro {Name} ignorado: gameData retornou erro — \"{Error}\".",
                        member.Name, gameDataError);
                    skipped++;
                    continue;
                }

                var cls = allClasses.FirstOrDefault(c => c.Id == member.ClassId);

                // Usa gameData.global.id (ID do jogo) — consistente com ImportReportService.
                var gameId = WclGraphQLClient.ExtractGameIdFromGameData(member.GameData);
                if (gameId is null)
                {
                    logger.LogWarning(
                        "Membro {Name} não possui gameData.global.id — usando ID interno WCL ({WclId}) como fallback.",
                        member.Name, member.Id);
                }

                var character = new Character
                {
                    WclActorId = gameId ?? member.Id,
                    Name       = member.Name,
                    Server     = member.Server?.Name ?? guild.Server,
                    Region     = member.Server?.Region?.Name ?? guild.Region,
                    GuildId    = guildId,
                    Class      = cls
                };

                await characters.UpsertAsync(character, ct);
                synced++;
            }

            progress?.Report(new GuildSyncProgressEvent(
                guildId, GuildSyncPhase.SavingCharacters,
                $"{synced} personagem(ns) salvos, {skipped} ignorado(s) até agora.",
                new { synced, skipped, page, lastPage }));

            page++;
        }
        while (page <= lastPage);

        if (skipped > 0)
            logger.LogWarning("{Skipped} membro(s) ignorado(s) por erro em gameData.", skipped);

        logger.LogInformation(
            "Sync concluído para guild {Guild}: {Synced} salvos, {Skipped} ignorados.",
            guild.Name, synced, skipped);

        progress?.Report(new GuildSyncProgressEvent(
            guildId, GuildSyncPhase.Completed,
            $"Sincronização concluída: {synced} personagem(ns) atualizado(s), {skipped} ignorado(s).",
            new { synced, skipped }));

        return new GuildSyncResultDto(guildId, guild.Name, synced);
    }
}
