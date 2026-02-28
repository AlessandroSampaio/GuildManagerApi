using GuildManagerApi.Domain.Entities;
using GuildManagerApi.Domain.Interfaces;
using GuildManagerApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GuildManagerApi.Infrastructure.Repositories;

public class UserRepository(AppDbContext context) : IUserRepository
{

    private readonly AppDbContext _context = context;

    public async Task AddAsync(AppUser user, CancellationToken ct = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AddRefreshTokenAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        _context.RefreshTokens.Add(token);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(string username, string email, CancellationToken cancellationToken = default)
        => _context.Users.AnyAsync(u => u.Username.Equals(username) || u.Email.Equals(email), cancellationToken);


    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => _context.Users.FirstOrDefaultAsync(u => u.Email.Equals(email), cancellationToken);

    public Task<AppUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Users.FirstOrDefaultAsync(u => u.Id.Equals(id), cancellationToken);

    public Task<AppUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
        => _context.Users.FirstOrDefaultAsync(u => u.Username.Equals(username), cancellationToken);

    public Task<RefreshToken?> GetRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
        => _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token.Equals(token), cancellationToken);

    public Task RevokeAllUserTokensAsync(int userId, CancellationToken cancellationToken = default)
        => _context.RefreshTokens
            .Where(r => r.UserId.Equals(userId) && !r.IsRevoked)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsRevoked, true), cancellationToken);


    public async Task RevokeRefreshTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        var stored = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token, cancellationToken);
        if (stored is null) return;
        stored.IsRevoked = true;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(AppUser user, CancellationToken ct = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(ct);
    }
}
