using BidGuard.Domain.Entities;

namespace BidGuard.Domain.Interfaces;

public interface IOrganizationRepository : IRepository<Organization>
{
    Task<Organization?> GetWithCredentialsAsync(Guid id, CancellationToken ct = default);
    Task<bool> PanExistsAsync(string pan, CancellationToken ct = default);
}

public interface ICredentialRepository : IRepository<Credential>
{
    Task<IReadOnlyList<Credential>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);

    /// <summary>Credentials expiring on/before <paramref name="expiringOnOrBefore"/> not alerted since <paramref name="notAlertedSince"/>.</summary>
    Task<IReadOnlyList<Credential>> GetExpiringAsync(DateOnly expiringOnOrBefore, DateTime notAlertedSince, CancellationToken ct = default);
}
