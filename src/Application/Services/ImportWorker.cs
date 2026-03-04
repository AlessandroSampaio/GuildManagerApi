using GuildManagerApi.Domain.Enums;
using GuildManagerApi.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GuildManagerApi.Application.Services;

public sealed partial class ImportWorker(
        IImportQueue queue,
        ImportProgressHub hub,
        IServiceScopeFactory scopeFactory,
        ILogger<ImportWorker> logger) : BackgroundService
{
    private readonly IImportQueue _queue = queue;
    private readonly ImportProgressHub _hub = hub;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<ImportWorker> _logger = logger;


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogWorkerStatus("started");

        await foreach (var job in _queue.ReadAllAsync(stoppingToken))
        {
            LogProcessingJob(
                job.ReportCode, job.UserId?.ToString() ?? "unknown");

            await ProcessJobAsync(job, stoppingToken);
        }

        LogWorkerStatus("stopped");
    }

    private async Task ProcessJobAsync(ImportJob job, CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var importService = scope.ServiceProvider.GetRequiredService<IImportReportService>();
        var reportRepo = scope.ServiceProvider.GetRequiredService<IReportRepository>();

        var progress = new HubProgress(_hub, ct);

        try
        {
            await importService.ImportAsync(
                job.ReportCode,
                job.UserId,
                progress: progress,
                ct: ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Import job cancelled: {Code}", job.ReportCode);
            await reportRepo.SetImportStatusAsync(
                job.ReportCode, ImportStatus.Failed, "Importação cancelada.", ct: default);

            await _hub.BroadcastAsync(new ImportProgressEvent(
                job.ReportCode, ImportPhase.Failed, "Importação cancelada."), ct: default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import job failed: {Code}", job.ReportCode);

            var errorMsg = $"{ex.GetType().Name}: {ex.Message}";
            try
            {
                await reportRepo.SetImportStatusAsync(
                    job.ReportCode, ImportStatus.Failed, errorMsg, ct: default);
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Failed to persist error status for {Code}", job.ReportCode);
            }

            await _hub.BroadcastAsync(new ImportProgressEvent(
                job.ReportCode, ImportPhase.Failed, errorMsg), ct: default);
        }

    }

    [LoggerMessage(LogLevel.Information, Message = "Processing import job: report={Code}, userId={UserId}")]
    private partial void LogProcessingJob(string Code, string UserId);


    [LoggerMessage(LogLevel.Information, Message = "Worker status: {Status}")]
    private partial void LogWorkerStatus(string status);


    private sealed class HubProgress(ImportProgressHub hub, CancellationToken ct) : IProgress<ImportProgressEvent>
    {
        private readonly ImportProgressHub _hub = hub;
        private readonly CancellationToken _ct = ct;

        void IProgress<ImportProgressEvent>.Report(ImportProgressEvent value)
        {
            // IProgress.Report() é síncrono por contrato — inicia broadcast async
            // sem await (fire-and-forget) para não bloquear o worker.
            _ = _hub.BroadcastAsync(value, _ct);
        }
    }
}
