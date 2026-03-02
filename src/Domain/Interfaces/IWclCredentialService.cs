namespace GuildManagerApi.Domain.Interfaces;

public interface IWclCredentialService
{
    Task<string> GetClientIdAsync(CancellationToken ct = default);
    Task<string> GetClientSecretAsync(CancellationToken ct = default);
    Task SaveAsync(string clientId, string clientSecret, string? label = null, CancellationToken ct = default);
    Task<bool> AreConfiguredAsync(CancellationToken ct = default);
}
