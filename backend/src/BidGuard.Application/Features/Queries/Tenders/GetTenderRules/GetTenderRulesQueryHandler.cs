using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Queries.Tenders.GetTenderRules;

public sealed class GetTenderRulesQueryHandler(
    ITenderRepository tenders,
    IComplianceRuleRepository rules,
    ICacheService cache,
    ICurrentUserService currentUser) : IQueryHandler<GetTenderRulesQuery, IReadOnlyList<RuleDto>>
{
    private static readonly TimeSpan PublishedTtl = TimeSpan.FromHours(1);

    public async ValueTask<IReadOnlyList<RuleDto>> Handle(GetTenderRulesQuery query, CancellationToken ct)
    {
        var tender = await tenders.GetByIdAsync(query.TenderId, ct) ?? throw new NotFoundException(nameof(Tender), query.TenderId);
        TenderAccess.EnsureVisible(tender, currentUser);

        async Task<List<RuleDto>> Load() => (await rules.GetByTenderAsync(tender.Id, ct)).Select(RuleDto.From).ToList();

        // Published rule sets are immutable, so they are safe to cache; drafts change while officers edit.
        return tender.Status == TenderStatus.Published
            ? await cache.GetOrSetAsync(CacheKeys.TenderRules(tender.Id), Load, PublishedTtl, ct)
            : await Load();
    }
}
