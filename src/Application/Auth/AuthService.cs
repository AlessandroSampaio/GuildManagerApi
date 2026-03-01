using GuildManagerApi.Application.DTOs;
using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Enums;
using GuildManagerApi.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace GuildManagerApi.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeAllAsync(Guid userId, CancellationToken ct = default);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
}

public class AuthService(IUserRepository users, IJwtService jwt, IOptions<JwtOptions> jwtOpts) : IAuthService
{
    private readonly IUserRepository _users = users;
    private readonly IJwtService _jwt = jwt;
    private readonly JwtOptions _jwtOpts = jwtOpts.Value;

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(userId, ct) ?? throw new InvalidOperationException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _users.UpdateAsync(user, ct);

        // Revoke all sessions after password change
        await _users.RevokeAllUserTokensAsync(userId, ct);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _users.GetByUsernameAsync(request.Username.Trim(), ct)
            ?? throw new UnauthorizedAccessException("Invalid username or password.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account disabled");

        if (BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invai usernme or password.");

        user.LastLoginAt = DateTime.UtcNow;
        await _users.UpdateAsync(user, ct);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var stored = await _users.GetRefreshTokenAsync(refreshToken, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token");

        if (stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
        {
            // Revoke all tokens if a revoked token is reused, for token rotation security
            if (stored.IsRevoked)
                await _users.RevokeAllUserTokensAsync(stored.UserId, ct);

            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        var user = await _users.GetByIdAsync(stored.UserId, ct)
            ?? throw new UnauthorizedAccessException("User not found");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account disabled");

        //Rotate: revoke old tokens
        await _users.RevokeRefreshTokenAsync(refreshToken, ct);

        return await IssueTokensAsync(user, ct);

    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        if (await _users.ExistsAsync(request.Username, request.Email, ct))
            throw new InvalidOperationException("Username or Email already in user");

        var user = new AppUser
        {
            Username = request.Username.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = AppUserRole.Member,
        };


        await _users.AddAsync(user, ct);
        return await IssueTokensAsync(user, ct);
    }

    public Task RevokeAllAsync(Guid userId, CancellationToken ct = default)
        => _users.RevokeAllUserTokensAsync(userId, ct);

    public Task RevokeAsync(string refreshToken, CancellationToken ct = default)
        => _users.RevokeRefreshTokenAsync(refreshToken, ct);


    private async Task<AuthResponse> IssueTokensAsync(AppUser user, CancellationToken ct)
    {
        var accessToken = _jwt.GenerateAccessToken(user);
        var rawRefresh = _jwt.GenerateRefreshToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtOpts.AccessTokenExpiryMinutes);

        var refreshEntity = new RefreshToken
        {
            UserId = user.Id,
            Token = rawRefresh,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOpts.RefreshTokenExpiryDays)
        };

        await _users.AddRefreshTokenAsync(refreshEntity, ct);

        return new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: rawRefresh,
            ExpiresAt: expiresAt,
            User: ToDto(user)
        );
    }

    private static UserInfoDto ToDto(AppUser u) => new(
           u.Id, u.Username, u.Email, u.Role, u.CreatedAt, u.LastLoginAt);

}
