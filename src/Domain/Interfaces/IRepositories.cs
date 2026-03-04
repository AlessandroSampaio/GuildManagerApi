using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Enums;

namespace GuildManagerApi.Domain.Interfaces;

public interface IReportRepository
{
    Task<Report?> GetByIdAsync(string reportId, CancellationToken ct = default);
    Task<IEnumerable<Report>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task UpsertAsync(Report report, CancellationToken ct = default);
    Task<bool> ExistsAsync(string reportId, CancellationToken ct = default);
    Task SetImportStatusAsync(
            string reportId,
            ImportStatus status,
            string? errorMessage = null,
            CancellationToken ct = default);
}

public interface ICharacterRepository
{
    Task<Character?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Character?> FindByWclActorAsync(long wclActorId, string server, CancellationToken ct = default);
    Task<IEnumerable<Character>> GetByGuildAsync(int guildId, CancellationToken ct = default);
    Task<int> UpsertAsync(Character character, CancellationToken ct = default);
    Task<IEnumerable<Class>> GetClassesAsync(CancellationToken ct = default);
}

public interface IGuildRepository
{
    Task<Guild?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Guild?> FindByNameAndServerAsync(string name, string server, string region, CancellationToken ct = default);
    Task<IEnumerable<Report>> GetReportsByGuildAsync(int guildId, int page, int pageSize, CancellationToken ct = default);
    Task<int> UpsertAsync(Guild guild, CancellationToken ct = default);
}

public interface IPerformanceRepository
{
    Task<IEnumerable<PerformanceEntry>> GetByFightAsync(int fightId, CancellationToken ct = default);
    Task<IEnumerable<PerformanceEntry>> GetByCharacterAsync(int characterId, CancellationToken ct = default);
    Task BulkUpsertAsync(IEnumerable<PerformanceEntry> entries, CancellationToken ct = default);
}
