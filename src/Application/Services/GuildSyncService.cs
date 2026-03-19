using GuildManagerApi.Application.DTOs;
using GuildManagerApi.Application.GraphQL;
using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GuildManagerApi.Application.Services;

public interface IGuildSyncService
{
    Task<GuildSyncResultDto> SyncCharactersAsync(int guildId, Guid? userId, CancellationToken ct = default);
}

public class GuildSyncService(
    IGuildRepository guilds,
    ICharacterRepository characters,
    IWclGraphQLClient wclClient,
    ILogger<GuildSyncService> logger) : IGuildSyncService
{
    public async Task<GuildSyncResultDto> SyncCharactersAsync(int guildId, Guid? userId, CancellationToken ct = default)
    {
        var guild = await guilds.GetByIdAsync(guildId, ct)
            ?? throw new KeyNotFoundException($"Guild {guildId} not found.");

        var serverSlug = guild.Server.ToLowerInvariant().Replace(' ', '-');
        var members = await wclClient.GetGuildMembersAsync(guild.Name, serverSlug, guild.Region, userId, ct);

        var allClasses = (await characters.GetClassesAsync(ct)).ToList();
        int synced = 0;

        foreach (var member in members)
        {
            var cls = allClasses.FirstOrDefault(c => c.Id == member.ClassId);

            var character = new Character
            {
                WclActorId = member.Id,
                Name = member.Name,
                Server = member.Server?.Name ?? guild.Server,
                Region = member.Server?.Region?.Name ?? guild.Region,
                GuildId = guildId,
                Class = cls
            };

            await characters.UpsertAsync(character, ct);
            synced++;
        }

        logger.LogInformation("Synced {Count} characters for guild {Guild}", synced, guild.Name);
        return new GuildSyncResultDto(guildId, guild.Name, synced);
    }
}
