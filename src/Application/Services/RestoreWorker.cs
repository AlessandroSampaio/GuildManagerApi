using GuildManagerApi.Domain.Enums;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Backup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GuildManagerApi.Application.Services;

public sealed partial class RestoreWorker(
        IRestoreQueue queue,
        RestoreHub hub,
        IMaintenanceModeService maintenance,
        IServiceScopeFactory scopeFactory,
        ILogger<RestoreWorker> logger) : BackgroundService
{
    private readonly IRestoreQueue _queue = queue;
    private readonly RestoreHub _hub = hub;
    private readonly IMaintenanceModeService _maintenance = maintenance;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<RestoreWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerStatus("started");

        await foreach (var job in _queue.ReadAllAsync(stoppingToken))
        {
            LogProcessingJob(job.RestoreId, job.UserId?.ToString() ?? "unknown");
            await ProcessJobAsync(job, stoppingToken);
        }

        LogWorkerStatus("stopped");
    }

    private async Task ProcessJobAsync(RestoreJobMessage job, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var restoreRepo = scope.ServiceProvider.GetRequiredService<IRestoreJobRepository>();
        var backupRepo  = scope.ServiceProvider.GetRequiredService<IBackupJobRepository>();
        var svc         = scope.ServiceProvider.GetRequiredService<IDatabaseBackupService>();
        var audit       = scope.ServiceProvider.GetRequiredService<IAuditService>();
        var settings    = scope.ServiceProvider.GetRequiredService<IOptions<BackupSettings>>().Value;

        var progress = new HubProgress(_hub, ct);
        var entityId = job.RestoreId.ToString();

        _maintenance.Enter(job.RestoreId);
        try
        {
            await restoreRepo.SetStatusAsync(job.RestoreId, JobStatus.Running, ct: default);
            await audit.LogAsync("Restore.Started", "Restore", entityId, job.UserId, ct: default);
            await _hub.BroadcastAsync(
                new RestoreProgressEvent(job.RestoreId, RestorePhase.Started, "Restore iniciado."), ct: default);

            var filePath = job.UploadedFilePath;
            if (filePath is null)
            {
                var sourceBackup = job.SourceBackupId.HasValue
                    ? await backupRepo.GetByIdAsync(job.SourceBackupId.Value, ct: default)
                    : null;
                filePath = sourceBackup is null
                    ? throw new InvalidOperationException($"Source backup {job.SourceBackupId} not found.")
                    : Path.Combine(settings.ResolvedStorageDirectory, sourceBackup.FileName);
            }

            await _hub.BroadcastAsync(
                new RestoreProgressEvent(job.RestoreId, RestorePhase.Validating, "Validando arquivo de backup."), ct: default);
            if (!await svc.IsValidCustomFormatDumpAsync(filePath, ct))
                throw new InvalidOperationException("Arquivo não é um dump válido do pg_dump (formato custom / cabeçalho PGDMP ausente).");

            await svc.RestoreAsync(filePath, job.RestoreId, progress, ct);

            await restoreRepo.SetStatusAsync(job.RestoreId, JobStatus.Completed, completedAt: DateTime.UtcNow, ct: default);
            await audit.LogAsync("Restore.Completed", "Restore", entityId, job.UserId, ct: default);
            await _hub.BroadcastAsync(
                new RestoreProgressEvent(job.RestoreId, RestorePhase.Completed, "Restore concluído."), ct: default);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Restore job cancelled: {RestoreId}", job.RestoreId);
            await restoreRepo.SetStatusAsync(
                job.RestoreId, JobStatus.Failed, "Restore cancelado.", DateTime.UtcNow, ct: default);
            await audit.LogAsync("Restore.Failed", "Restore", entityId, job.UserId,
                details: "Restore cancelado.", ct: default);

            await _hub.BroadcastAsync(new RestoreProgressEvent(
                job.RestoreId, RestorePhase.Failed, "Restore cancelado."), ct: default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Restore job failed: {RestoreId}", job.RestoreId);

            var errorMsg = $"{ex.GetType().Name}: {ex.Message}";
            try
            {
                await restoreRepo.SetStatusAsync(
                    job.RestoreId, JobStatus.Failed, errorMsg, DateTime.UtcNow, ct: default);
                await audit.LogAsync("Restore.Failed", "Restore", entityId, job.UserId,
                    details: errorMsg, ct: default);
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Failed to persist error status for restore {RestoreId}", job.RestoreId);
            }

            await _hub.BroadcastAsync(new RestoreProgressEvent(
                job.RestoreId, RestorePhase.Failed, errorMsg), ct: default);
        }
        finally
        {
            _maintenance.Exit(job.RestoreId);

            if (job.UploadedFilePath is not null)
            {
                try { File.Delete(job.UploadedFilePath); }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temp upload {Path}", job.UploadedFilePath);
                }
            }
        }
    }

    [LoggerMessage(LogLevel.Information, Message = "Processing restore job: restoreId={RestoreId}, userId={UserId}")]
    private partial void LogProcessingJob(Guid RestoreId, string UserId);

    [LoggerMessage(LogLevel.Information, Message = "RestoreWorker {Status}")]
    private partial void LogWorkerStatus(string Status);

    private sealed class HubProgress(RestoreHub hub, CancellationToken ct) : IProgress<RestoreProgressEvent>
    {
        private readonly RestoreHub _hub = hub;
        private readonly CancellationToken _ct = ct;

        void IProgress<RestoreProgressEvent>.Report(RestoreProgressEvent value)
        {
            _ = _hub.BroadcastAsync(value, _ct);
        }
    }
}
