using GuildManagerApi.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GuildManagerApi.Api.Controllers;

[ApiController]
[Route("api/raider-io")]
[Authorize]
[Produces("application/json")]
public class RaiderIoController(IRaiderIoService raiderIoService) : ControllerBase
{
    private readonly IRaiderIoService _raiderIoService = raiderIoService;

    /// <summary>
    /// Proxy para GET /api/v1/characters/profile do Raider.IO.
    /// Retorna mythic_plus_best_runs:all e raid_progression do personagem.
    /// </summary>
    [HttpGet("characters/profile")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCharacterProfile(
        [FromQuery] string region,
        [FromQuery] string realm,
        [FromQuery] string name,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(region))
            return BadRequest(new { error = "region is required." });
        if (string.IsNullOrWhiteSpace(realm))
            return BadRequest(new { error = "realm is required." });
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { error = "name is required." });

        var (statusCode, body) = await _raiderIoService.GetCharacterProfileAsync(region, realm, name, ct);

        return new ContentResult
        {
            Content = body,
            ContentType = "application/json",
            StatusCode = statusCode,
        };
    }
}
