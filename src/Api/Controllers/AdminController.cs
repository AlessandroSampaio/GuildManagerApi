using GuildManagerApi.Application.DTOs;
using GuildManagerApi.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GuildManagerApi.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = "AdminOnly")]
[Produces("application/json")]
public class AdminController(IWclCredentialService credentialService) : ControllerBase
{
    private readonly IWclCredentialService _credentialsService = credentialService;

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
}
