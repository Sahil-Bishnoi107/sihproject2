using BidGuard.Application.Services.Audit;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Queries.Audit.VerifyAuditChain;

public sealed class VerifyAuditChainQueryHandler(IAuditLogRepository repository)
    : IQueryHandler<VerifyAuditChainQuery, AuditChainStatus>
{
    public async ValueTask<AuditChainStatus> Handle(VerifyAuditChainQuery query, CancellationToken ct)
    {
        var entries = await repository.GetAllOrderedAsync(ct);
        var brokenId = AuditHasher.VerifyChain(entries);
        var brokenSeq = brokenId is null ? (long?)null : entries.First(e => e.Id == brokenId).Sequence;
        return new AuditChainStatus(brokenId is null, entries.Count, brokenId, brokenSeq);
    }
}
