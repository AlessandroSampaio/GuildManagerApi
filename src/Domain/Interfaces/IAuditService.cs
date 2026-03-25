using GuildManagerApi.Domain.Entities;

namespace GuildManagerApi.Domain.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        string action,
        string entityType,
        string? entityId = null,
        Guid? actorId = null,
        string? actorUsername = null,
        string? details = null,
        CancellationToken ct = default);

    Task<(IReadOnlyList<AuditLog> Items, int Total)> QueryAsync(
        int page,
        int pageSize,
        string? entityType = null,
        string? action = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default);
}
