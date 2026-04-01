using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GuildManagerApi.Infrastructure.Encryption;

public class RaiderIoCredentialService(AppDbContext db, IFieldEncryptionService enc) : IRaiderIoCredentialService
{
    private readonly AppDbContext _db = db;
    private readonly IFieldEncryptionService _enc = enc;

    public Task<bool> IsConfiguredAsync(CancellationToken ct = default)
        => _db.RaiderIoCredentials.AnyAsync(c => c.Id == 1, ct);

    public async Task<string?> GetApiKeyAsync(CancellationToken ct = default)
    {
        var cred = await _db.RaiderIoCredentials.FirstOrDefaultAsync(c => c.Id == 1, ct);
        return cred is null ? null : _enc.Decrypt(cred.ApiKeyEncrypted);
    }

    public async Task SaveAsync(string apiKey, string? label = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("ApiKey cannot be empty.", nameof(apiKey));

        var existing = await _db.RaiderIoCredentials.FirstOrDefaultAsync(c => c.Id == 1, ct);
        if (existing is null)
        {
            existing = new RaiderIoCredential { Id = 1 };
            _db.RaiderIoCredentials.Add(existing);
        }

        existing.ApiKeyEncrypted = _enc.Encrypt(apiKey);
        existing.Label = label;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(CancellationToken ct = default)
    {
        var existing = await _db.RaiderIoCredentials.FirstOrDefaultAsync(c => c.Id == 1, ct);
        if (existing is null) return;
        _db.RaiderIoCredentials.Remove(existing);
        await _db.SaveChangesAsync(ct);
    }
}
