namespace GuildManagerApi.Domain.Interfaces;

public interface IRaiderIoCredentialService
{
    /// <summary>Returns the stored API key, or null if not configured.</summary>
    Task<string?> GetApiKeyAsync(CancellationToken ct = default);
    Task SaveAsync(string apiKey, string? label = null, CancellationToken ct = default);
    Task<bool> IsConfiguredAsync(CancellationToken ct = default);
    Task DeleteAsync(CancellationToken ct = default);
}
