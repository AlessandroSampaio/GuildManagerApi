using GuildManagerApi.Domain.Entities;

namespace GuildManagerApi.Domain.Interfaces;

public interface IRaiderIoProfileRepository
{
    Task UpsertSnapshotAsync(RaiderIoCharacterSnapshot snapshot, CancellationToken ct = default);
    Task<RaiderIoCharacterSnapshot?> GetSnapshotByCharacterIdAsync(int characterId, CancellationToken ct = default);
}
