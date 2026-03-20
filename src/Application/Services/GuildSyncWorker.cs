using GuildManagerApi.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GuildManagerApi.Application.Services;

public sealed partial class GuildSyncWorker(
        IGuildSyncQueue queue,
        GuildSyncHub hub,
        IServiceScopeFactory scopeFactory,
        ILogger<GuildSyncWorker> logger) : BackgroundService
{
    private readonly IGuildSyncQueue    _queue        = queue;
    private readonly GuildSyncHub       _hub          = hub;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<GuildSyncWorker> _logger  = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerStatus("started");

        await foreach (var job in _queue.ReadAllAsync(stoppingToken))
        {
            LogProcessingJob(job.GuildId, job.UserId?.ToString() ?? "anonymous");
            await ProcessJobAsync(job, stoppingToken);
        }

        LogWorkerStatus("stopped");
    }

    private async Task ProcessJobAsync(GuildSyncJob job, CancellationToken ct)
    {
        await using var scope       = _scopeFactory.CreateAsyncScope();
        var syncService             = scope.ServiceProvider.GetRequiredService<IGuildSyncService>();
        IProgress<GuildSyncProgressEvent> progress = new HubProgress(_hub, ct);

        try
        {
            await syncService.SyncCharactersAsync(job.GuildId, job.UserId, progress, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Guild sync cancelled: guildId={GuildId}", job.GuildId);

            await _hub.BroadcastAsync(new GuildSyncProgressEvent(
                job.GuildId, GuildSyncPhase.Failed, "Sincronização cancelada."), ct: default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Guild sync failed: guildId={GuildId}", job.GuildId);

            await _hub.BroadcastAsync(new GuildSyncProgressEvent(
                job.GuildId, GuildSyncPhase.Failed,
                $"{ex.GetType().Name}: {ex.Message}"), ct: default);
        }
    }

    [LoggerMessage(LogLevel.Information,
        Message = "Processing guild sync job: guildId={GuildId}, userId={UserId}")]
    private partial void LogProcessingJob(int GuildId, string UserId);

    [LoggerMessage(LogLevel.Information, Message = "GuildSyncWorker {Status}")]
    private partial void LogWorkerStatus(string Status);

    // ── IProgress adapter ──────────────────────────────────────────────────────
    // Mesma estratégia fire-and-forget usada no ImportWorker.HubProgress.
    private sealed class HubProgress(GuildSyncHub hub, CancellationToken ct)
        : IProgress<GuildSyncProgressEvent>
    {
        private readonly GuildSyncHub _hub = hub;
        private readonly CancellationToken _ct = ct;

        void IProgress<GuildSyncProgressEvent>.Report(GuildSyncProgressEvent value)
            => _ = _hub.BroadcastAsync(value, _ct);
    }
}
