using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Common.Models;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Queries.Tenders.GetTenders;

public sealed class GetTendersQueryHandler(ITenderRepository tenders, ICurrentUserService currentUser)
    : IQueryHandler<GetTendersQuery, PagedResult<TenderDto>>
{
    public async ValueTask<PagedResult<TenderDto>> Handle(GetTendersQuery query, CancellationToken ct)
    {
        // Bidders only ever see tenders that are open for bidding.
        var status = currentUser.Role == UserRole.Bidder ? TenderStatus.Published : query.Status;

        var (items, total) = await tenders.ListAsync(status, query.Search, query.Page, query.PageSize, ct);
        return new PagedResult<TenderDto>(items.Select(TenderDto.From).ToList(), query.Page, query.PageSize, total);
    }
}
