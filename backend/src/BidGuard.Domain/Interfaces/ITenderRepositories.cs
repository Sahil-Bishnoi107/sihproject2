using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;

namespace BidGuard.Domain.Interfaces;

public interface ITenderRepository : IRepository<Tender>
{
    Task<bool> GemBidNumberExistsAsync(string gemBidNumber, CancellationToken ct = default);
    Task<(IReadOnlyList<Tender> Items, int Total)> ListAsync(TenderStatus? status, string? search, int page, int pageSize, CancellationToken ct = default);
}

public interface IComplianceRuleRepository : IRepository<ComplianceRule>
{
    Task<IReadOnlyList<ComplianceRule>> GetByTenderAsync(Guid tenderId, CancellationToken ct = default);
}
