using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using GuildManagerApi.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GuildManagerApi.Application.Services;

public sealed partial class ImportProgressHub(ILogger<ImportProgressHub> logger) : IImportProgressHub
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<WebSocket>> _sockets = new();
    private readonly ILogger<ImportProgressHub> _logger = logger;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void Register(string reportCode, WebSocket socket)
    {
        var bag = _sockets.GetOrAdd(reportCode, _ => []);
        bag.Add(socket);
        LogRegisterWS(reportCode, bag.Count);
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
            _logger.LogDebug(ex, "WS closed unexpectedly");
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

    public async Task BroadcastAsync(ImportProgressEvent evt, CancellationToken ct = default)
    {
        if (!_sockets.TryGetValue(evt.ReportCode, out var bag)) return;

        var json = JsonSerializer.Serialize(new
        {
            reportCode = evt.ReportCode,
            phase = evt.Phase.ToString(),
            phaseCode = (int)evt.Phase,
            message = evt.Message,
            data = evt.Data,
            timestamp = DateTime.UtcNow
        }, _jsonOpts);

        var bytes = Encoding.UTF8.GetBytes(json);
        var segment = new ArraySegment<byte>(bytes);

        var live = new List<WebSocket>();
        var dead = new List<WebSocket>();

        foreach (var ws in bag)
        {
            if (ws.State == WebSocketState.Open)
                live.Add(ws);
            else
                dead.Add(ws);
        }

        var sends = live.Select(ws => SendSafeAsync(ws, segment, ct));
        await Task.WhenAll(sends);

        if (dead.Count > 0)
        {
            var fresh = new ConcurrentBag<WebSocket>(live.Where(ws => ws.State == WebSocketState.Open));
            _sockets.TryUpdate(evt.ReportCode, fresh, bag);
            LogRemoveWS(evt.ReportCode, dead.Count);
        }

        // Limpar entrada do dicionário quando todos saíram
        if (_sockets.TryGetValue(evt.ReportCode, out var current) && current.IsEmpty)
            _sockets.TryRemove(evt.ReportCode, out _);
    }

    private async Task SendSafeAsync(WebSocket ws, ArraySegment<byte> data, CancellationToken ct)
    {
        try
        {
            await ws.SendAsync(data, WebSocketMessageType.Text, endOfMessage: true, ct);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to send WS message — client disconnected");
        }
    }
    [LoggerMessage(LogLevel.Information, Message = "WS registered for {ReportCode}. Total: {Counter}")]
    private partial void LogRegisterWS(string reportCode, int Counter);

    [LoggerMessage(LogLevel.Information, Message = "Removed {Count} dead WS(s) for {ReportCode}")]
    private partial void LogRemoveWS(string reportCode, int Count);
}
