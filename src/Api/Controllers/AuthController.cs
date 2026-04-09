using System.Security.Claims;
using GuildManagerApi.Application.Auth;
using GuildManagerApi.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GuildManagerApi.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(IAuthService auth) : ControllerBase
{
    private readonly IAuthService _auth = auth;

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="request">The registration model.</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The registered user.</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _auth.RegisterAsync(request, ct);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }



    /// <summary>
    /// Logs in a user.
    /// </summary>
    /// <param name="request">The login model.</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The logged in user tokens</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _auth.LoginAsync(request, ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }


    /// <summary>
    /// Refreshes a user's access token.
    /// </summary>
    /// <param name="request">The refresh token model.</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>The refreshed user tokens</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _auth.RefreshAsync(request.RefreshToken, ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Revoga o refresh token (logout da sessão atual).
    /// </summary>
    /// <param name="request">The refresh token model.</param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>No content</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        await _auth.RevokeAsync(request.RefreshToken, ct);
        return NoContent();
    }

    /// <summary>
    /// Revoga todos os refresh tokens do usuário (logout de todos os dispositivos).
    /// </summary>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>No content</returns>
    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        await _auth.RevokeAllAsync(userId.Value, ct);
        return NoContent();
    }

    /// <summary>
    /// Altera a senha do usuário autenticado e revoga todas as sessões.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="ct">Cancellation Token</param>
    /// <returns>No content</returns>
    [HttpPatch("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        try
        {
            await _auth.ChangePasswordAsync(userId.Value, request, ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retorna informações do usuário autenticado.
    /// </summary>
    /// <returns>No content</returns>

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserInfoDto), StatusCodes.Status200OK)]
    public IActionResult Me()
    {
        var claims = User.Claims.ToDictionary(c => c.Type, c => c.Value);
        return Ok(new
        {
            id = claims.GetValueOrDefault(ClaimTypes.NameIdentifier),
            username = claims.GetValueOrDefault(ClaimTypes.Name),
            email = claims.GetValueOrDefault(ClaimTypes.Email),
            role = claims.GetValueOrDefault(ClaimTypes.Role)
        });
    }

    /// <summary>
    /// Redefine a senha confirmando username e e-mail cadastrados.
    /// Revoga todas as sessões ativas do usuário.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        try
        {
            await _auth.ResetPasswordAsync(request, ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    private Guid? GetUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
        return sub is null ? null : Guid.Parse(sub);
    }

}
