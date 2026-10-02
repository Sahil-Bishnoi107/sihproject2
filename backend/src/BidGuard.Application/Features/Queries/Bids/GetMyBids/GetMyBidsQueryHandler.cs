using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Queries.Bids.GetMyBids;

public sealed class GetMyBidsQueryHandler(IBidRepository bids, ICurrentUserService currentUser)
    : IQueryHandler<GetMyBidsQuery, IReadOnlyList<BidDto>>
{
    public async ValueTask<IReadOnlyList<BidDto>> Handle(GetMyBidsQuery query, CancellationToken ct)
    {
        var orgId = currentUser.OrganizationId ?? throw new ForbiddenException("Only bidder accounts have bids.");
        return (await bids.GetByOrganizationAsync(orgId, ct)).Select(BidDto.From).ToList();
    }
}
