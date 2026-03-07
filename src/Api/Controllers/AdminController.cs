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
    IScoringSettingsRepository scoringSettingsRepository) : ControllerBase
{
    private readonly IWclCredentialService _credentialsService = credentialService;
    private readonly IScoringSettingsRepository _scoringSettingsRepository = scoringSettingsRepository;

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
        return deleted ? NoContent() : NotFound(new { message = "No scoring settings to delete." });
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


    private static ScoringSettingsDto ToDto(Domain.Entities.ScoringSettings s) =>
           new(
               s.UpdatedAt,
               [..s.Tiers
                .OrderBy(t => t.MinPercent)
                .Select(t => new ScoringTierDto(t.Id, t.MinPercent, t.MaxPercent, t.Points, t.Label))]
           );
}
