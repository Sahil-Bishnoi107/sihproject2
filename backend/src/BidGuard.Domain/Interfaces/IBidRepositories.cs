using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;

namespace BidGuard.Domain.Interfaces;

public interface IBidRepository : IRepository<Bid>
{
    Task<IReadOnlyList<Bid>> GetOfficialByTenderAsync(Guid tenderId, CancellationToken ct = default);
    Task<IReadOnlyList<Bid>> GetByOrganizationAsync(Guid organizationId, CancellationToken ct = default);
    Task<bool> OfficialBidExistsAsync(Guid tenderId, Guid organizationId, CancellationToken ct = default);
}

public interface IDocumentRepository : IRepository<Document>
{
    Task<IReadOnlyList<Document>> GetByOwnerAsync(DocumentOwnerType ownerType, Guid ownerId, CancellationToken ct = default);
}

public interface IExtractedFieldRepository : IRepository<ExtractedField>
{
    Task<IReadOnlyList<ExtractedField>> GetByBidAsync(Guid bidId, CancellationToken ct = default);
}

public interface IVerificationResultRepository : IRepository<VerificationResult>
{
    Task<IReadOnlyList<VerificationResult>> GetByBidAsync(Guid bidId, CancellationToken ct = default);
}

public interface IRuleEvaluationRepository : IRepository<RuleEvaluation>
{
    Task<IReadOnlyList<RuleEvaluation>> GetByBidAsync(Guid bidId, CancellationToken ct = default);
}
