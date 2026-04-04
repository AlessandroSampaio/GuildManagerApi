namespace GuildManagerApi.Domain.Entities;

public class BattleNetUserToken
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastRefreshedAt { get; set; }
    /// <summary>BattleTag do usuário (ex: Player#1234). Obtido via /userinfo após o exchange.</summary>
    public string? BattleTag { get; set; }
    /// <summary>Battle.net account ID numérico (campo "sub" do userinfo).</summary>
    public string? Sub { get; set; }
    /// <summary>Battle.net não emite refresh tokens — token expirado exige re-autorização.</summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt.AddMinutes(-2);

    public AppUser User { get; set; } = null!;
}

public class BattleNetCredential
{
    public int Id { get; set; } = 1;
    public byte[] ClientIdEncrypted { get; set; } = [];
    public byte[] ClientSecretEncrypted { get; set; } = [];
    public string? Label { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
