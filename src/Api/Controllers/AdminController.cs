using System.Security.Claims;
using GuildManagerApi.Application.Auth;
using GuildManagerApi.Application.DTOs;
using GuildManagerApi.Domain.Exceptions;
using GuildManagerApi.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GuildManagerApi.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
[Authorize(Policy = "AdminOnly")]
[Produces("application/json")]
public class AdminController(
    IWclCredentialService credentialService,
    IBattleNetCredentialService bnetCredentialService,
    IRaiderIoCredentialService raiderIoCredentialService,
    IScoringSettingsRepository scoringSettingsRepository,
    IAuditService audit,
    IAuthService authService) : ControllerBase
{
    private readonly IWclCredentialService _credentialsService = credentialService;
    private readonly IBattleNetCredentialService _bnetCredentialsService = bnetCredentialService;
    private readonly IRaiderIoCredentialService _raiderIoCredentialsService = raiderIoCredentialService;
    private readonly IScoringSettingsRepository _scoringSettingsRepository = scoringSettingsRepository;
    private readonly IAuditService _audit = audit;
    private readonly IAuthService _authService = authService;

    [HttpPut("wcl-credentials")]
    [ProducesResponseType(typeof(WclCredentialStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpsertWclCCredentials(
        [FromBody] WclCredentialRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId))
            return BadRequest(new { error = "ClientId is required." });

        if (string.IsNullOrWhiteSpace(request.ClientSecret))
            return BadRequest(new { error = "ClientSecret is required." });

        await _credentialsService.SaveAsync(
                   request.ClientId.Trim(),
                   request.ClientSecret.Trim(),
                   request.Label?.Trim(),
                   ct);

        await _audit.LogAsync("WclCredentials.Updated", "WclCredential",
            actorId: GetActorId(), actorUsername: GetActorUsername(), ct: ct);

        return Ok(new WclCredentialStatusResponse(
            Configured: true,
            Label: request.Label?.Trim(),
            UpdatedAt: DateTime.UtcNow,
            Message: "WarcraftLogs credentials saved successfully. " +
                        "ClientId and ClientSecret are stored encrypted (AES-256-GCM)."
        ));
    }

    [HttpGet("wcl-credentials/status")]
    [ProducesResponseType(typeof(WclCredentialStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWclCredentialStatus(CancellationToken ct)
    {
        var configured = await _credentialsService.AreConfiguredAsync(ct);

        return Ok(new WclCredentialStatusResponse(
            Configured: configured,
            Label: null,
            UpdatedAt: null,
            Message: configured
                ? "WarcraftLogs credentials are configured."
                : "WarcraftLogs credentials have not been set. Call PUT /api/admin/wcl-credentials."
        ));
    }

    [HttpPut("bnet-credentials")]
    [ProducesResponseType(typeof(BNetCredentialStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpsertBNetCredentials(
        [FromBody] BNetCredentialRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId))
            return BadRequest(new { error = "ClientId is required." });

        if (string.IsNullOrWhiteSpace(request.ClientSecret))
            return BadRequest(new { error = "ClientSecret is required." });

        await _bnetCredentialsService.SaveAsync(
            request.ClientId.Trim(),
            request.ClientSecret.Trim(),
            request.Label?.Trim(),
            ct);

        await _audit.LogAsync("BNetCredentials.Updated", "BattleNetCredential",
            actorId: GetActorId(), actorUsername: GetActorUsername(), ct: ct);

        return Ok(new BNetCredentialStatusResponse(
            Configured: true,
            Label: request.Label?.Trim(),
            UpdatedAt: DateTime.UtcNow,
            Message: "Battle.net credentials saved successfully. " +
                     "ClientId and ClientSecret are stored encrypted (AES-256-GCM)."
        ));
    }

    [HttpGet("bnet-credentials/status")]
    [ProducesResponseType(typeof(BNetCredentialStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBNetCredentialStatus(CancellationToken ct)
    {
        var configured = await _bnetCredentialsService.AreConfiguredAsync(ct);

        return Ok(new BNetCredentialStatusResponse(
            Configured: configured,
            Label: null,
            UpdatedAt: null,
            Message: configured
                ? "Battle.net credentials are configured."
                : "Battle.net credentials have not been set. Call PUT /api/admin/bnet-credentials."
        ));
    }

    [HttpPut("raider-io-key")]
    [ProducesResponseType(typeof(RaiderIoKeyStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertRaiderIoKey(
        [FromBody] RaiderIoKeyRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
            return BadRequest(new { error = "ApiKey is required." });

        await _raiderIoCredentialsService.SaveAsync(
            request.ApiKey.Trim(),
            request.Label?.Trim(),
            ct);

        await _audit.LogAsync("RaiderIoKey.Updated", "RaiderIoCredential",
            actorId: GetActorId(), actorUsername: GetActorUsername(), ct: ct);

        return Ok(new RaiderIoKeyStatusResponse(
            Configured: true,
            Label: request.Label?.Trim(),
            UpdatedAt: DateTime.UtcNow,
            Message: "Raider.IO API key saved successfully. Key is stored encrypted (AES-256-GCM)."
        ));
    }

    [HttpGet("raider-io-key/status")]
    [ProducesResponseType(typeof(RaiderIoKeyStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRaiderIoKeyStatus(CancellationToken ct)
    {
        var configured = await _raiderIoCredentialsService.IsConfiguredAsync(ct);

        return Ok(new RaiderIoKeyStatusResponse(
            Configured: configured,
            Label: null,
            UpdatedAt: null,
            Message: configured
                ? "Raider.IO API key is configured."
                : "Raider.IO API key has not been set. Requests will use the public rate limit (200 req/min). Call PUT /api/admin/raider-io-key to configure."
        ));
    }

    [HttpDelete("raider-io-key")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteRaiderIoKey(CancellationToken ct)
    {
        await _raiderIoCredentialsService.DeleteAsync(ct);

        await _audit.LogAsync("RaiderIoKey.Deleted", "RaiderIoCredential",
            actorId: GetActorId(), actorUsername: GetActorUsername(), ct: ct);

        return NoContent();
    }


    [HttpGet("scoring-settings")]
    [ProducesResponseType(typeof(ScoringSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetScoringSettings(CancellationToken ct)
    {
        var settings = await _scoringSettingsRepository.GetAsync(ct);
        if (settings is null)
            return NotFound(new { message = "Scoring settings have not been configured yet." });

        return Ok(ToDto(settings));
    }


    [HttpPut("scoring-settings")]
    [ProducesResponseType(typeof(ScoringSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertScoringSettings(
            [FromBody] ScoringSettingsRequest request,
            CancellationToken ct)
    {
        if (request.Tiers is null || request.Tiers.Count == 0)
            return BadRequest(new { error = "At least one tier is required." });

        try
        {
            var inputs = request.Tiers.Select(t => new ScoringTierInput(
                t.MinPercent,
                t.MaxPercent,
                t.Points,
                t.Label
            ));

            var settings = await _scoringSettingsRepository.SaveAsync(inputs, ct);
            await _audit.LogAsync("ScoringSettings.Updated", "ScoringSettings",
                actorId: GetActorId(), actorUsername: GetActorUsername(), ct: ct);
            return Ok(ToDto(settings));
        }
        catch (ScoringValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }


    [HttpDelete("scoring-settings")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteScoringSettings(CancellationToken ct)
    {
        var deleted = await _scoringSettingsRepository.DeleteAsync(ct);
        if (!deleted) return NotFound(new { message = "No scoring settings to delete." });

        await _audit.LogAsync("ScoringSettings.Deleted", "ScoringSettings",
            actorId: GetActorId(), actorUsername: GetActorUsername(), ct: ct);
        return NoContent();
    }

    [HttpGet("scoring-settings/calculate")]
    [ProducesResponseType(typeof(ScoreCalculationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CalculateScore(
            [FromQuery] float rankPercent,
            CancellationToken ct)
    {
        if (rankPercent < 0 || rankPercent > 100)
            return BadRequest(new { error = "rankPercent must be between 0 and 100." });

        var settings = await _scoringSettingsRepository.GetAsync(ct);
        if (settings is null)
            return NotFound(new { message = "Scoring settings have not been configured yet." });

        var points = await _scoringSettingsRepository.CalculateScoreAsync(rankPercent, ct);

        // Encontrar o tier correspondente para incluir o label
        var matchedTier = settings.Tiers
            .OrderBy(t => t.MinPercent)
            .FirstOrDefault(t =>
                rankPercent >= t.MinPercent &&
                (rankPercent < t.MaxPercent || t.MaxPercent >= 100f));

        return Ok(new ScoreCalculationResult(
            RankPercent: rankPercent,
            Points: points,
            TierLabel: matchedTier?.Label,
            Message: points.HasValue
                ? $"RankPercent {rankPercent}% falls in tier [{matchedTier!.MinPercent}–{matchedTier.MaxPercent}%] → {points} points."
                : $"RankPercent {rankPercent}% does not match any configured tier."
        ));
    }


    [HttpGet("audit-log")]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? entityType = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);

        var (items, total) = await _audit.QueryAsync(page, pageSize, entityType, action, from, to, ct);

        var dtos = items.Select(a => new AuditLogDto(
            a.Id, a.Action, a.EntityType, a.EntityId,
            a.ActorId, a.ActorUsername, a.Details, a.OccurredAt
        )).ToList();

        return Ok(new PagedResult<AuditLogDto>(dtos, page, pageSize, total));
    }

    /// <summary>
    /// Redefine a senha de um usuário. Se NewPassword for null, gera uma senha temporária e a retorna.
    /// Revoga todas as sessões ativas do usuário.
    /// </summary>
    [HttpPost("users/reset-password")]
    [ProducesResponseType(typeof(AdminResetPasswordResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminResetPassword(
        [FromBody] AdminResetPasswordRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _authService.AdminResetPasswordAsync(request, ct);

            await _audit.LogAsync(
                "User.AdminPasswordReset",
                "AppUser",
                entityId: request.UserId.ToString(),
                actorId: GetActorId(),
                actorUsername: GetActorUsername(),
                ct: ct);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    private Guid? GetActorId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private string? GetActorUsername() =>
        User.FindFirstValue(ClaimTypes.Name)
        ?? User.Identity?.Name;

    private static ScoringSettingsDto ToDto(Domain.Entities.ScoringSettings s) =>
           new(
               s.UpdatedAt,
               [..s.Tiers
                .OrderBy(t => t.MinPercent)
                .Select(t => new ScoringTierDto(t.Id, t.MinPercent, t.MaxPercent, t.Points, t.Label))]
           );
}
