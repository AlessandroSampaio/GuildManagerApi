using System.ComponentModel.DataAnnotations;
using GuildManagerApi.Domain.Enums;

namespace GuildManagerApi.Application.DTOs;

// ── Requests ──────────────────────────────────────────────────────────────────

public record RegisterRequest(
    [Required, MinLength(3), MaxLength(32)] string Username,
    [Required, EmailAddress] string Email,
    [Required, MinLength(8), MaxLength(64)] string Password
);

public record LoginRequest(
    [Required] string Username,
    [Required] string Password
);

public record RefreshTokenRequest(
    [Required] string RefreshToken
);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(8), MaxLength(64)] string NewPassword
);

// ── Responses ─────────────────────────────────────────────────────────────────

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserInfoDto User
);

public record UserInfoDto(
    Guid Id,
    string Username,
    string Email,
    string Role,
    DateTime CreatedAt,
    DateTime? LastLoginAt
);
