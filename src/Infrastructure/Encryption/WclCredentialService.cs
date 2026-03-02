using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GuildManagerApi.Infrastructure.Encryption;

public class WclCredentialService(AppDbContext db, IFieldEncryptionService enc) : IWclCredentialService
{
    private readonly AppDbContext _db = db;
    private readonly IFieldEncryptionService _enc = enc;

    public Task<bool> AreConfiguredAsync(CancellationToken ct = default)
        => _db.WclCredentials.AnyAsync(c => c.Id == 1, ct);

    public async Task<string> GetClientIdAsync(CancellationToken ct = default)
    {
        var cred = await GetRequiredAsync(ct);
        return _enc.Decrypt(cred.ClientIdEncrypted);
    }

    public async Task<string> GetClientSecretAsync(CancellationToken ct = default)
    {
        var cred = await GetRequiredAsync(ct);
        return _enc.Decrypt(cred.ClientSecretEncrypted);
    }

    public async Task SaveAsync(string clientId, string clientSecret, string? label = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new ArgumentException("ClientId cannot be empty.", nameof(clientId));
        if (string.IsNullOrWhiteSpace(clientSecret))
            throw new ArgumentException("ClientSecret cannot be empty.", nameof(clientSecret));
        var existing = await _db.WclCredentials.FirstOrDefaultAsync(c => c.Id == 1, ct);

        if (existing is null)
        {
            existing = new WclCredential { Id = 1 };
            _db.WclCredentials.Add(existing);
        }

        existing.ClientIdEncrypted = _enc.Encrypt(clientId);
        existing.ClientSecretEncrypted = _enc.Encrypt(clientSecret);
        existing.Label = label;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    private async Task<WclCredential> GetRequiredAsync(CancellationToken ct)
    {
        var cred = await _db.WclCredentials.FirstOrDefaultAsync(c => c.Id == 1, ct);
        return cred ?? throw new InvalidOperationException(
            "WarcraftLogs credentials have not been configured. " +
            "Call PUT /api/admin/wcl-credentials to set ClientId and ClientSecret.");
    }
}
