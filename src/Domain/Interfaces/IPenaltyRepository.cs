using GuildManagerApi.Domain.Entities;

namespace GuildManagerApi.Domain.Interfaces;

public interface IPenaltyRepository
{
    // PenaltyEvent CRUD
    Task<PenaltyEvent?> GetEventByIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<PenaltyEvent>> GetAllEventsAsync(CancellationToken ct = default);
    Task<int> CreateEventAsync(PenaltyEvent penaltyEvent, CancellationToken ct = default);
    Task<bool> UpdateEventAsync(int id, string description, int points, CancellationToken ct = default);
    Task<bool> DeleteEventAsync(int id, CancellationToken ct = default);

    // PlayerWeekPenalty
    Task<IEnumerable<PlayerWeekPenalty>> GetPenaltiesByWeekAsync(int raidWeekId, CancellationToken ct = default);
    Task<PlayerWeekPenalty?> GetPenaltyByIdAsync(int id, CancellationToken ct = default);
    Task<int> AddPlayerPenaltyAsync(PlayerWeekPenalty penalty, CancellationToken ct = default);
    Task<bool> RemovePlayerPenaltyAsync(int id, CancellationToken ct = default);
}
