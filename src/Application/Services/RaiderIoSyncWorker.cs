using GuildManagerApi.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GuildManagerApi.Application.Services;

public sealed partial class RaiderIoSyncWorker(
    IRaiderIoSyncQueue queue,
    RaiderIoSyncHub hub,
    IServiceScopeFactory scopeFactory,
    IOptions<RaiderIoSyncOptions> options,
    ILogger<RaiderIoSyncWorker> logger) : BackgroundService
{
    private readonly IRaiderIoSyncQueue _queue        = queue;
    private readonly RaiderIoSyncHub _hub             = hub;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly RaiderIoSyncOptions _options      = options.Value;
    private readonly ILogger<RaiderIoSyncWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerStatus("started");

        using var timer = new PeriodicTimer(
            TimeSpan.FromHours(_options.SyncIntervalHours));

        var queueTask = DrainQueueAsync(stoppingToken);
        var timerTask = RunPeriodicAsync(timer, stoppingToken);

        await Task.WhenAny(queueTask, timerTask);
        await Task.WhenAll(queueTask, timerTask);

        LogWorkerStatus("stopped");
    }

    private async Task DrainQueueAsync(CancellationToken ct)
    {
        await foreach (var job in _queue.ReadAllAsync(ct))
        {
            LogProcessingJob(job.CoreId, job.UserId?.ToString() ?? "periodic");
            await ProcessJobAsync(job, ct);
        }
    }

    private async Task RunPeriodicAsync(PeriodicTimer timer, CancellationToken ct)
    {
        while (await timer.WaitForNextTickAsync(ct))
        {
            LogPeriodicTick();
            await EnqueueAllCoresAsync(ct);
        }
    }

    private async Task EnqueueAllCoresAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var coreRepo = scope.ServiceProvider.GetRequiredService<ICoreRepository>();

        var coreIds = await coreRepo.GetAllCoreIdsAsync(ct);
        foreach (var id in coreIds)
            await _queue.EnqueueAsync(new RaiderIoSyncJob(id, null), ct);
    }

    private async Task ProcessJobAsync(RaiderIoSyncJob job, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var syncService = scope.ServiceProvider.GetRequiredService<IRaiderIoSyncService>();
        var audit       = scope.ServiceProvider.GetRequiredService<IAuditService>();
        IProgress<RaiderIoSyncProgressEvent> progress = new HubProgress(_hub, ct);

        var entityId = job.CoreId.ToString();

        try
        {
            await audit.LogAsync("Core.RaiderIoSyncStarted", "Core", entityId, job.UserId, ct: default);
            await syncService.SyncCoreAsync(job.CoreId, job.UserId, progress, ct);
            await audit.LogAsync("Core.RaiderIoSyncCompleted", "Core", entityId, job.UserId, ct: default);
            LogJobCompleted(job.CoreId, job.UserId?.ToString() ?? "periodic");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Raider.IO sync cancelled: coreId={CoreId}", job.CoreId);
            await audit.LogAsync("Core.RaiderIoSyncFailed", "Core", entityId, job.UserId,
                details: "Sincronização cancelada.", ct: default);

            await _hub.BroadcastAsync(new RaiderIoSyncProgressEvent(
                job.CoreId, RaiderIoSyncPhase.Failed, "Sincronização cancelada."), ct: default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Raider.IO sync failed: coreId={CoreId}", job.CoreId);

            var errorMsg = $"{ex.GetType().Name}: {ex.Message}";
            await audit.LogAsync("Core.RaiderIoSyncFailed", "Core", entityId, job.UserId,
                details: errorMsg, ct: default);

            await _hub.BroadcastAsync(new RaiderIoSyncProgressEvent(
                job.CoreId, RaiderIoSyncPhase.Failed, errorMsg), ct: default);
        }
    }

    [LoggerMessage(LogLevel.Information, Message = "RaiderIoSyncWorker {Status}")]
    private partial void LogWorkerStatus(string Status);

    [LoggerMessage(LogLevel.Information,
        Message = "Processing Raider.IO sync job: coreId={CoreId}, userId={UserId}")]
    private partial void LogProcessingJob(int CoreId, string UserId);

    [LoggerMessage(LogLevel.Information,
        Message = "RaiderIoSyncWorker periodic tick — enqueueing all cores")]
    private partial void LogPeriodicTick();

    [LoggerMessage(LogLevel.Information,
        Message = "Raider.IO sync completed: coreId={CoreId}, userId={UserId}")]
    private partial void LogJobCompleted(int CoreId, string UserId);

    // ── IProgress adapter ──────────────────────────────────────────────────────
    private sealed class HubProgress(RaiderIoSyncHub hub, CancellationToken ct)
        : IProgress<RaiderIoSyncProgressEvent>
    {
        private readonly RaiderIoSyncHub _hub = hub;
        private readonly CancellationToken _ct = ct;

        void IProgress<RaiderIoSyncProgressEvent>.Report(RaiderIoSyncProgressEvent value)
            => _ = _hub.BroadcastAsync(value, _ct);
    }
}
