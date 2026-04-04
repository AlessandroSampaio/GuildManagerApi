using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using GuildManagerApi.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GuildManagerApi.Application.Services;

/// <summary>
/// Hub WebSocket para eventos de sincronização Raider.IO de core.
/// Segue o mesmo padrão de <see cref="GuildSyncHub"/>, usando coreId como chave.
/// </summary>
public sealed partial class RaiderIoSyncHub(ILogger<RaiderIoSyncHub> logger)
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<WebSocket>> _sockets = new();
    private readonly ILogger<RaiderIoSyncHub> _logger = logger;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Register(int coreId, WebSocket socket)
    {
        var key = coreId.ToString();
        var bag  = _sockets.GetOrAdd(key, _ => []);
        bag.Add(socket);
        LogRegisterWS(coreId, bag.Count);
    }

    public async Task WaitForCloseAsync(WebSocket socket, CancellationToken ct = default)
    {
        var buffer = new byte[64];

        try
        {
            while (socket.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;
            }
        }
        catch (OperationCanceledException) { }
        catch (WebSocketException ex)
        {
            _logger.LogDebug(ex, "WS closed unexpectedly for Raider.IO sync");
        }
        finally
        {
            if (socket.State == WebSocketState.Open ||
                socket.State == WebSocketState.CloseReceived)
            {
                try
                {
                    await socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                }
                catch { /* already gone */ }
            }
        }
    }

    public async Task BroadcastAsync(RaiderIoSyncProgressEvent evt, CancellationToken ct = default)
    {
        var key = evt.CoreId.ToString();
        if (!_sockets.TryGetValue(key, out var bag)) return;

        var json = JsonSerializer.Serialize(new
        {
            coreId    = evt.CoreId,
            phase     = evt.Phase.ToString(),
            phaseCode = (int)evt.Phase,
            message   = evt.Message,
            data      = evt.Data,
            timestamp = DateTime.UtcNow
        }, _jsonOpts);

        var bytes   = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        var live = new List<WebSocket>();
        var dead = new List<WebSocket>();

        foreach (var ws in bag)
        {
            if (ws.State == WebSocketState.Open) live.Add(ws);
            else                                  dead.Add(ws);
        }

        await Task.WhenAll(live.Select(ws => SendSafeAsync(ws, segment, ct)));

        if (dead.Count > 0)
        {
            var fresh = new ConcurrentBag<WebSocket>(
                live.Where(ws => ws.State == WebSocketState.Open));
            _sockets.TryUpdate(key, fresh, bag);
            LogRemoveWS(evt.CoreId, dead.Count);
        }

        if (_sockets.TryGetValue(key, out var current) && current.IsEmpty)
            _sockets.TryRemove(key, out _);
    }

    private async Task SendSafeAsync(WebSocket ws, ArraySegment<byte> data, CancellationToken ct)
    {
        try
        {
            await ws.SendAsync(data, WebSocketMessageType.Text, endOfMessage: true, ct);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to send Raider.IO sync WS message — client disconnected");
        }
    }

    [LoggerMessage(LogLevel.Information,
        Message = "Raider.IO sync WS registered for core {CoreId}. Total connections: {Count}")]
    private partial void LogRegisterWS(int CoreId, int Count);

    [LoggerMessage(LogLevel.Information,
        Message = "Removed {Count} dead WS connection(s) for core {CoreId}")]
    private partial void LogRemoveWS(int CoreId, int Count);
}
