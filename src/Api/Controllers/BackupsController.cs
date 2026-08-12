using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using GuildManagerApi.Application.Auth;
using GuildManagerApi.Application.DTOs;
using GuildManagerApi.Application.Services;
using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Enums;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Backup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GuildManagerApi.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
[Authorize(Policy = "AdminOnly")]
[Produces("application/json")]
public class BackupsController(
        IBackupJobRepository backupRepo,
        IRestoreJobRepository restoreRepo,
        IBackupQueue backupQueue,
        IRestoreQueue restoreQueue,
        BackupHub backupHub,
        RestoreHub restoreHub,
        IDatabaseBackupService backupService,
        IJwtService jwtService,
        IOptions<BackupSettings> backupSettings,
        IAuditService audit,
        ILogger<BackupsController> logger) : ControllerBase
{
    private readonly IBackupJobRepository _backupRepo = backupRepo;
    private readonly IRestoreJobRepository _restoreRepo = restoreRepo;
    private readonly IBackupQueue _backupQueue = backupQueue;
    private readonly IRestoreQueue _restoreQueue = restoreQueue;
    private readonly BackupHub _backupHub = backupHub;
    private readonly RestoreHub _restoreHub = restoreHub;
    private readonly IDatabaseBackupService _backupService = backupService;
    private readonly IJwtService _jwtService = jwtService;
    private readonly BackupSettings _settings = backupSettings.Value;
    private readonly IAuditService _audit = audit;
    private readonly ILogger<BackupsController> _logger = logger;

    private static readonly JsonSerializerOptions _jsonOpts =
            new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    // ── Backups ──────────────────────────────────────────────────────────────

    [HttpPost("backups")]
    [ProducesResponseType(typeof(BackupAcceptedDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateBackup(CancellationToken ct)
    {
        if (await _backupRepo.HasActiveJobAsync(ct))
        {
            return Conflict(new { error = "A backup is already in progress." });
        }

        var userId = GetUserId();
        var id = Guid.NewGuid();
        var fileName = $"backup_{DateTime.UtcNow:yyyyMMddHHmmss}_{id:N}.dump";

        await _backupRepo.CreateAsync(new BackupJob
        {
            Id = id,
            FileName = fileName,
            Status = JobStatus.Queued,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        }, ct);

        await _backupQueue.EnqueueAsync(new BackupJobMessage(id, userId), ct);

        return AcceptedAtAction(
            nameof(GetBackup),
            new { id },
            new BackupAcceptedDto(
                BackupId: id,
                Status: "Queued",
                WsUrl: $"/api/admin/backups/{id}/ws",
                Message: "Backup enfileirado. Conecte-se ao WebSocket para acompanhar o progresso."
            ));
    }

    [HttpGet("backups")]
    [ProducesResponseType(typeof(PagedResult<BackupDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListBackups(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);
        var (items, total) = await _backupRepo.GetPagedAsync(page, pageSize, ct);
        var dtos = items.Select(ToDto).ToList();
        return Ok(new PagedResult<BackupDto>(dtos, page, pageSize, total));
    }

    [HttpGet("backups/{id:guid}")]
    [ProducesResponseType(typeof(BackupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBackup([FromRoute] Guid id, CancellationToken ct)
    {
        var job = await _backupRepo.GetByIdAsync(id, ct);
        if (job is null) return NotFound();
        return Ok(ToDto(job));
    }

    [HttpGet("backups/{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DownloadBackup([FromRoute] Guid id, CancellationToken ct)
    {
        var job = await _backupRepo.GetByIdAsync(id, ct);
        if (job is null) return NotFound();

        if (job.Status != JobStatus.Completed)
            return Conflict(new { error = "Backup is not completed yet.", status = job.Status.ToString() });

        var path = Path.Combine(_settings.ResolvedStorageDirectory, job.FileName);
        if (!System.IO.File.Exists(path))
            return NotFound(new { error = "Backup file is missing on disk." });

        return PhysicalFile(path, "application/octet-stream", job.FileName, enableRangeProcessing: true);
    }

    [HttpDelete("backups/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteBackup([FromRoute] Guid id, CancellationToken ct)
    {
        var job = await _backupRepo.GetByIdAsync(id, ct);
        if (job is null) return NotFound();

        if (job.Status == JobStatus.Running)
            return Conflict(new { error = "Cannot delete a backup that is currently running." });

        await _backupRepo.DeleteAsync(id, ct);

        var path = Path.Combine(_settings.ResolvedStorageDirectory, job.FileName);
        try
        {
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete backup file {Path}", path);
        }

        await _audit.LogAsync("Backup.Deleted", "Backup", id.ToString(),
            GetUserId(), GetUsername(), ct: ct);

        return NoContent();
    }

    [HttpGet("backups/{id:guid}/ws")]
    [AllowAnonymous]
    public async Task StreamBackupProgress(
        [FromRoute] Guid id, [FromQuery] string? access_token, CancellationToken ct)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await HttpContext.Response.WriteAsync("Requisição WebSocket esperada. Use ws:// ou wss://.", ct);
            return;
        }

        var principal = string.IsNullOrWhiteSpace(access_token) ? null : _jwtService.ValidateToken(access_token);
        if (principal is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!principal.IsInRole("Admin"))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();

        var job = await _backupRepo.GetByIdAsync(id, ct);
        if (job is null)
        {
            await SendAndCloseAsync(ws, new
            {
                backupId = id,
                phase = "Failed",
                phaseCode = (int)BackupPhase.Failed,
                message = $"Backup '{id}' não encontrado.",
                timestamp = DateTime.UtcNow
            }, WebSocketCloseStatus.InvalidPayloadData, ct);
            return;
        }

        if (job.Status == JobStatus.Completed)
        {
            await SendAndCloseAsync(ws, new
            {
                backupId = id,
                phase = "Completed",
                phaseCode = (int)BackupPhase.Completed,
                message = "Backup já concluído.",
                timestamp = DateTime.UtcNow
            }, WebSocketCloseStatus.NormalClosure, ct);
            return;
        }

        if (job.Status == JobStatus.Failed)
        {
            await SendAndCloseAsync(ws, new
            {
                backupId = id,
                phase = "Failed",
                phaseCode = (int)BackupPhase.Failed,
                message = job.ErrorMessage ?? "Backup falhou.",
                timestamp = DateTime.UtcNow
            }, WebSocketCloseStatus.NormalClosure, ct);
            return;
        }

        _backupHub.Register(id, ws);
        await _backupHub.WaitForCloseAsync(ws, ct);
    }

    // ── Restore ──────────────────────────────────────────────────────────────

    [HttpPost("backups/{id:guid}/restore")]
    [ProducesResponseType(typeof(RestoreAcceptedDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RestoreFromCatalog(
        [FromRoute] Guid id, [FromBody] RestoreConfirmRequest request, CancellationToken ct)
    {
        if (!request.Confirm)
            return BadRequest(new { error = "confirm must be true to restore the database." });

        var backup = await _backupRepo.GetByIdAsync(id, ct);
        if (backup is null) return NotFound();

        if (backup.Status != JobStatus.Completed)
            return Conflict(new { error = "Only a completed backup can be restored.", status = backup.Status.ToString() });

        if (await _restoreRepo.HasActiveJobAsync(ct))
            return Conflict(new { error = "A restore is already in progress." });

        var userId = GetUserId();
        var restoreId = Guid.NewGuid();

        await _restoreRepo.CreateAsync(new RestoreJob
        {
            Id = restoreId,
            SourceBackupId = id,
            SourceFileName = backup.FileName,
            IsUpload = false,
            Status = JobStatus.Queued,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        }, ct);

        await _restoreQueue.EnqueueAsync(new RestoreJobMessage(restoreId, id, null, userId), ct);

        return Accepted(new RestoreAcceptedDto(
            RestoreId: restoreId,
            Status: "Queued",
            WsUrl: $"/api/admin/restores/{restoreId}/ws",
            Message: "Restore enfileirado. Conecte-se ao WebSocket para acompanhar o progresso."
        ));
    }

    [HttpPost("backups/restore/upload")]
    [Consumes("multipart/form-data")]
    [DisableRequestSizeLimit]
    [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
    [ProducesResponseType(typeof(RestoreAcceptedDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RestoreFromUpload(
        [FromForm] RestoreUploadRequest request, CancellationToken ct)
    {
        if (!request.Confirm)
            return BadRequest(new { error = "confirm must be true to restore the database." });

        if (request.File is null || request.File.Length == 0)
            return BadRequest(new { error = "A file is required." });

        if (request.File.Length > _settings.MaxUploadSizeBytes)
            return BadRequest(new { error = $"File exceeds the maximum allowed size of {_settings.MaxUploadSizeBytes} bytes." });

        if (await _restoreRepo.HasActiveJobAsync(ct))
            return Conflict(new { error = "A restore is already in progress." });

        var uploadsDir = Path.Combine(_settings.ResolvedStorageDirectory, "uploads");
        Directory.CreateDirectory(uploadsDir);
        var tempPath = Path.Combine(uploadsDir, $"upload_{Guid.NewGuid():N}.dump");

        await using (var fs = System.IO.File.Create(tempPath))
        {
            await request.File.CopyToAsync(fs, ct);
        }

        if (!await _backupService.IsValidCustomFormatDumpAsync(tempPath, ct))
        {
            try { System.IO.File.Delete(tempPath); } catch { /* best-effort cleanup */ }
            return BadRequest(new { error = "File is not a valid pg_dump custom-format archive (PGDMP header missing)." });
        }

        var userId = GetUserId();
        var restoreId = Guid.NewGuid();

        await _restoreRepo.CreateAsync(new RestoreJob
        {
            Id = restoreId,
            SourceBackupId = null,
            SourceFileName = request.File.FileName,
            IsUpload = true,
            Status = JobStatus.Queued,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        }, ct);

        await _restoreQueue.EnqueueAsync(new RestoreJobMessage(restoreId, null, tempPath, userId), ct);

        return Accepted(new RestoreAcceptedDto(
            RestoreId: restoreId,
            Status: "Queued",
            WsUrl: $"/api/admin/restores/{restoreId}/ws",
            Message: "Restore enfileirado. Conecte-se ao WebSocket para acompanhar o progresso."
        ));
    }

    [HttpGet("restores/{restoreId:guid}/ws")]
    [AllowAnonymous]
    public async Task StreamRestoreProgress(
        [FromRoute] Guid restoreId, [FromQuery] string? access_token, CancellationToken ct)
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await HttpContext.Response.WriteAsync("Requisição WebSocket esperada. Use ws:// ou wss://.", ct);
            return;
        }

        var principal = string.IsNullOrWhiteSpace(access_token) ? null : _jwtService.ValidateToken(access_token);
        if (principal is null)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!principal.IsInRole("Admin"))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();

        var job = await _restoreRepo.GetByIdAsync(restoreId, ct);
        if (job is null)
        {
            await SendAndCloseAsync(ws, new
            {
                restoreId,
                phase = "Failed",
                phaseCode = (int)RestorePhase.Failed,
                message = $"Restore '{restoreId}' não encontrado.",
                timestamp = DateTime.UtcNow
            }, WebSocketCloseStatus.InvalidPayloadData, ct);
            return;
        }

        if (job.Status == JobStatus.Completed)
        {
            await SendAndCloseAsync(ws, new
            {
                restoreId,
                phase = "Completed",
                phaseCode = (int)RestorePhase.Completed,
                message = "Restore já concluído.",
                timestamp = DateTime.UtcNow
            }, WebSocketCloseStatus.NormalClosure, ct);
            return;
        }

        if (job.Status == JobStatus.Failed)
        {
            await SendAndCloseAsync(ws, new
            {
                restoreId,
                phase = "Failed",
                phaseCode = (int)RestorePhase.Failed,
                message = job.ErrorMessage ?? "Restore falhou.",
                timestamp = DateTime.UtcNow
            }, WebSocketCloseStatus.NormalClosure, ct);
            return;
        }

        _restoreHub.Register(restoreId, ws);
        await _restoreHub.WaitForCloseAsync(ws, ct);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static BackupDto ToDto(BackupJob job) => new(
        job.Id, job.FileName, job.SizeBytes, job.Status.ToString(),
        job.CreatedAt, job.CreatedByUserId, job.CompletedAt, job.ErrorMessage);

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
        return sub is null ? null : Guid.Parse(sub);
    }

    private string? GetUsername() =>
        User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;

    private static async Task SendAndCloseAsync(
           WebSocket ws, object payload, WebSocketCloseStatus closeStatus, CancellationToken ct)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload, _jsonOpts);
            var bytes = Encoding.UTF8.GetBytes(json);
            await ws.SendAsync(bytes, WebSocketMessageType.Text, true, ct);
            await ws.CloseAsync(closeStatus, "done", ct);
        }
        catch { /* client already gone */ }
    }
}
