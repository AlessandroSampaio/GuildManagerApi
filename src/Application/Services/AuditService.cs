using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GuildManagerApi.Application.Services;

public class AuditService(AppDbContext db) : IAuditService
{
    public async Task LogAsync(
        string action,
        string entityType,
        string? entityId = null,
        Guid? actorId = null,
        string? actorUsername = null,
        string? details = null,
        CancellationToken ct = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Action        = action,
            EntityType    = entityType,
            EntityId      = entityId,
            ActorId       = actorId,
            ActorUsername = actorUsername,
            Details       = details,
            OccurredAt    = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<AuditLog> Items, int Total)> QueryAsync(
        int page,
        int pageSize,
        string? entityType = null,
        string? action = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var q = db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(entityType))
            q = q.Where(a => a.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(action))
            q = q.Where(a => a.Action == action);
        if (from.HasValue)
            q = q.Where(a => a.OccurredAt >= from.Value);
        if (to.HasValue)
            q = q.Where(a => a.OccurredAt <= to.Value);

        var total = await q.CountAsync(ct);
        var items = await q
            .OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
