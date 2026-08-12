using GuildManagerApi.Domain.Enums;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Backup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GuildManagerApi.Application.Services;

public sealed partial class BackupWorker(
        IBackupQueue queue,
        BackupHub hub,
        IServiceScopeFactory scopeFactory,
        ILogger<BackupWorker> logger) : BackgroundService
{
    private readonly IBackupQueue _queue = queue;
    private readonly BackupHub _hub = hub;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<BackupWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerStatus("started");

        await foreach (var job in _queue.ReadAllAsync(stoppingToken))
        {
            LogProcessingJob(job.BackupId, job.UserId?.ToString() ?? "unknown");
            await ProcessJobAsync(job, stoppingToken);
        }

        LogWorkerStatus("stopped");
    }

    private async Task ProcessJobAsync(BackupJobMessage job, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repo     = scope.ServiceProvider.GetRequiredService<IBackupJobRepository>();
        var svc      = scope.ServiceProvider.GetRequiredService<IDatabaseBackupService>();
        var audit    = scope.ServiceProvider.GetRequiredService<IAuditService>();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<BackupSettings>>().Value;

        var progress = new HubProgress(_hub, ct);
        var entityId = job.BackupId.ToString();

        try
        {
            var record = await repo.GetByIdAsync(job.BackupId, ct: default)
                ?? throw new InvalidOperationException($"BackupJob {job.BackupId} not found.");

            await repo.SetStatusAsync(job.BackupId, JobStatus.Running, ct: default);
            await audit.LogAsync("Backup.Started", "Backup", entityId, job.UserId, ct: default);
            await _hub.BroadcastAsync(
                new BackupProgressEvent(job.BackupId, BackupPhase.Started, "Backup iniciado."), ct: default);

            var outputPath = Path.Combine(settings.ResolvedStorageDirectory, record.FileName);
            var size = await svc.DumpAsync(outputPath, job.BackupId, progress, ct);

            await repo.SetSizeAsync(job.BackupId, size, ct: default);
            await repo.SetStatusAsync(job.BackupId, JobStatus.Completed, completedAt: DateTime.UtcNow, ct: default);
            await audit.LogAsync("Backup.Completed", "Backup", entityId, job.UserId, ct: default);
            await _hub.BroadcastAsync(
                new BackupProgressEvent(job.BackupId, BackupPhase.Completed, "Backup concluído."), ct: default);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Backup job cancelled: {BackupId}", job.BackupId);
            await repo.SetStatusAsync(
                job.BackupId, JobStatus.Failed, "Backup cancelado.", DateTime.UtcNow, ct: default);
            await audit.LogAsync("Backup.Failed", "Backup", entityId, job.UserId,
                details: "Backup cancelado.", ct: default);

            await _hub.BroadcastAsync(new BackupProgressEvent(
                job.BackupId, BackupPhase.Failed, "Backup cancelado."), ct: default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup job failed: {BackupId}", job.BackupId);

            var errorMsg = $"{ex.GetType().Name}: {ex.Message}";
            try
            {
                await repo.SetStatusAsync(
                    job.BackupId, JobStatus.Failed, errorMsg, DateTime.UtcNow, ct: default);
                await audit.LogAsync("Backup.Failed", "Backup", entityId, job.UserId,
                    details: errorMsg, ct: default);
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Failed to persist error status for backup {BackupId}", job.BackupId);
            }

            await _hub.BroadcastAsync(new BackupProgressEvent(
                job.BackupId, BackupPhase.Failed, errorMsg), ct: default);
        }
    }

    [LoggerMessage(LogLevel.Information, Message = "Processing backup job: backupId={BackupId}, userId={UserId}")]
    private partial void LogProcessingJob(Guid BackupId, string UserId);

    [LoggerMessage(LogLevel.Information, Message = "BackupWorker {Status}")]
    private partial void LogWorkerStatus(string Status);

    private sealed class HubProgress(BackupHub hub, CancellationToken ct) : IProgress<BackupProgressEvent>
    {
        private readonly BackupHub _hub = hub;
        private readonly CancellationToken _ct = ct;

        void IProgress<BackupProgressEvent>.Report(BackupProgressEvent value)
        {
            // IProgress.Report() é síncrono por contrato — inicia broadcast async
            // sem await (fire-and-forget) para não bloquear o worker.
            _ = _hub.BroadcastAsync(value, _ct);
        }
    }
}
