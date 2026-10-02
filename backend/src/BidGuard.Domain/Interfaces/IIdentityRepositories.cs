using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;

namespace BidGuard.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetByRoleAsync(UserRole role, CancellationToken ct = default);
}

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    Task<IReadOnlyList<RefreshToken>> GetActiveForUserAsync(Guid userId, DateTime now, CancellationToken ct = default);
}
