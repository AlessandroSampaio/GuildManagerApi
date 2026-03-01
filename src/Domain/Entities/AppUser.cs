
using GuildManagerApi.Domain.Enums;

namespace GuildManagerApi.Domain.Entities;

public class AppUser
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public AppUserRole Role { get; set; } = AppUserRole.Member; // User | Admin
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;
    // Refresh tokens
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    // WarcraftLogs OAuth token (Authorization Code Flow)
    public WclUserToken? WclToken { get; set; }
}

public class RefreshToken
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; }

    public AppUser User { get; set; } = null!;
}

public class WclUserToken
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string WclRefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastRefreshedAt { get; set; }
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt.AddMinutes(-2);

    public AppUser User { get; set; } = null!;
}
