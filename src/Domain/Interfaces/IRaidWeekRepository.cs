using GuildManagerApi.Domain.Entities;

namespace GuildManagerApi.Domain.Interfaces;

public interface IRaidWeekRepository
{
    Task<RaidWeek?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<RaidWeek>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task<RaidWeek?> GetByDateAsync(DateTime date, CancellationToken ct = default);
    Task<int> CreateAsync(RaidWeek raidWeek, CancellationToken ct = default);
    Task<bool> UpdateAsync(int id, string label, DateTime startsAt, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<bool> AddReportAsync(int raidWeekId, string reportCode, CancellationToken ct = default);
    Task<bool> RemoveReportAsync(int raidWeekId, string reportCode, CancellationToken ct = default);
}
