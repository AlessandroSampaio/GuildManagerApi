using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GuildManagerApi.Application.Services;

public class RaiderIoSyncOptions
{
    public const string Section = "RaiderIoSync";
    public int SyncIntervalHours { get; set; } = 6;
    public int ThrottleDelayMs { get; set; } = 300;
}

public interface IRaiderIoSyncService
{
    Task SyncCoreAsync(
        int coreId,
        Guid? userId,
        IProgress<RaiderIoSyncProgressEvent>? progress = null,
        CancellationToken ct = default);
}

public class RaiderIoSyncService(
    ICoreRepository cores,
    IRaiderIoService raiderIoService,
    IOptions<RaiderIoSyncOptions> options,
    ILogger<RaiderIoSyncService> logger) : IRaiderIoSyncService
{
    private readonly ICoreRepository _cores = cores;
    private readonly IRaiderIoService _raiderIoService = raiderIoService;
    private readonly RaiderIoSyncOptions _options = options.Value;
    private readonly ILogger<RaiderIoSyncService> _logger = logger;

    public async Task SyncCoreAsync(
        int coreId,
        Guid? userId,
        IProgress<RaiderIoSyncProgressEvent>? progress = null,
        CancellationToken ct = default)
    {
        var core = await _cores.GetByIdWithCharactersAsync(coreId, ct)
            ?? throw new KeyNotFoundException($"Core {coreId} not found.");

        var characters = core.Players
            .SelectMany(cp => cp.Player.Characters)
            .Where(ch => !string.IsNullOrWhiteSpace(ch.Name)
                      && !string.IsNullOrWhiteSpace(ch.Server)
                      && !string.IsNullOrWhiteSpace(ch.Region))
            .ToList();

        progress?.Report(new RaiderIoSyncProgressEvent(
            coreId, RaiderIoSyncPhase.Started,
            $"Iniciando sync Raider.IO para o core \"{core.Name}\" — {characters.Count} personagem(ns).",
            new { total = characters.Count }));

        int synced = 0;
        int failed = 0;

        foreach (var ch in characters)
        {
            try
            {
                var (statusCode, body, safeUrl) = await _raiderIoService.GetCharacterProfileAsync(
                    ch.Region!, ch.Server!, ch.Name!, ch.Id, ct);

                if (statusCode is >= 200 and < 300)
                    synced++;
                else
                {
                    failed++;
                    _logger.LogError(
                        "Raider.IO sync failed for {Name}-{Server} ({Region}): HTTP {StatusCode} url={SafeUrl} response={Response}",
                        ch.Name, ch.Server, ch.Region, statusCode, safeUrl, body);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex,
                    "Raider.IO sync exception for {Name}-{Server} ({Region})",
                    ch.Name, ch.Server, ch.Region);
            }
            finally
            {
                if (!ct.IsCancellationRequested)
                    await Task.Delay(_options.ThrottleDelayMs, ct).ConfigureAwait(false);
            }

            progress?.Report(new RaiderIoSyncProgressEvent(
                coreId, RaiderIoSyncPhase.Syncing,
                $"[{synced + failed}/{characters.Count}] {ch.Name}-{ch.Server}",
                new { synced, failed, total = characters.Count }));
        }

        progress?.Report(new RaiderIoSyncProgressEvent(
            coreId, RaiderIoSyncPhase.Completed,
            $"Sync concluído: {synced} ok, {failed} erro(s).",
            new { synced, failed, total = characters.Count }));
    }
}
