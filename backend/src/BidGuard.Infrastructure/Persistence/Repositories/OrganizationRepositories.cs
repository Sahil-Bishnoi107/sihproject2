using BidGuard.Domain.Entities;
using BidGuard.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BidGuard.Infrastructure.Persistence.Repositories;

public sealed class OrganizationRepository(BidGuardDbContext db) : Repository<Organization>(db), IOrganizationRepository
{
    public Task<Organization?> GetWithCredentialsAsync(Guid id, CancellationToken ct = default) =>
        Set.Include(o => o.Credentials).FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<bool> PanExistsAsync(string pan, CancellationToken ct = default)
    {
        var normalized = pan.Trim().ToUpperInvariant();
        return Set.AnyAsync(o => o.Pan == normalized, ct);
    }
}

public sealed class CredentialRepository(BidGuardDbContext db) : Repository<Credential>(db), ICredentialRepository
{
    public async Task<IReadOnlyList<Credential>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default) =>
        await Set.Where(c => c.OrganizationId == organizationId).OrderBy(c => c.ExpiresOn).ToListAsync(ct);

    public async Task<IReadOnlyList<Credential>> GetExpiringAsync(DateOnly expiringOnOrBefore, DateTime notAlertedSince, CancellationToken ct = default) =>
        await Set.Where(c => c.ExpiresOn != null && c.ExpiresOn <= expiringOnOrBefore
                             && (c.LastExpiryAlertAt == null || c.LastExpiryAlertAt <= notAlertedSince))
            .ToListAsync(ct);
}
