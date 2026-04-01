namespace GuildManagerApi.Domain.Entities;

public class RaiderIoCredential
{
    public int Id { get; set; } = 1;
    public byte[] ApiKeyEncrypted { get; set; } = [];
    public string? Label { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
