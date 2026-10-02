using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BidGuard.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(BidGuardDbContext db) : Repository<User>(db), IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return Set.FirstOrDefaultAsync(u => u.Email == normalized, ct);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return Set.AnyAsync(u => u.Email == normalized, ct);
    }

    public async Task<IReadOnlyList<User>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default) =>
        await Set.Where(u => u.OrganizationId == organizationId && u.IsActive).ToListAsync(ct);

    public async Task<IReadOnlyList<User>> GetByRoleAsync(UserRole role, CancellationToken ct = default) =>
        await Set.Where(u => u.Role == role && u.IsActive).ToListAsync(ct);
}

public sealed class RefreshTokenRepository(BidGuardDbContext db) : Repository<RefreshToken>(db), IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default) =>
        Set.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<IReadOnlyList<RefreshToken>> GetActiveForUserAsync(Guid userId, DateTime now, CancellationToken ct = default) =>
        await Set.Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now).ToListAsync(ct);
}
