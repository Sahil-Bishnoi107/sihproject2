using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Bids;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Queries.Bids.GetBidById;

public sealed class GetBidByIdQueryHandler(IBidRepository bids, ICurrentUserService currentUser) : IQueryHandler<GetBidByIdQuery, BidDto>
{
    public async ValueTask<BidDto> Handle(GetBidByIdQuery query, CancellationToken ct)
    {
        var bid = await bids.GetByIdAsync(query.BidId, ct) ?? throw new NotFoundException(nameof(Bid), query.BidId);
        BidAccess.EnsureCanRead(bid, currentUser);
        return BidDto.From(bid);
    }
}
