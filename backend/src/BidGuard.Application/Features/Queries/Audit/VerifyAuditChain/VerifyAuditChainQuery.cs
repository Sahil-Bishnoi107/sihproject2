using Mediator;

namespace BidGuard.Application.Features.Queries.Audit.VerifyAuditChain;

public sealed record AuditChainStatus(bool IsIntact, int EntriesChecked, Guid? FirstBrokenEntryId, long? FirstBrokenSequence);

public sealed record VerifyAuditChainQuery : IQuery<AuditChainStatus>;
