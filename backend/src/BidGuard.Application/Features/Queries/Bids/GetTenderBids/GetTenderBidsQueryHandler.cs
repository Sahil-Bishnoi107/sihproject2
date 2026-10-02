using BidGuard.Application.Common.Dtos;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Queries.Bids.GetTenderBids;

public sealed class GetTenderBidsQueryHandler(IBidRepository bids) : IQueryHandler<GetTenderBidsQuery, IReadOnlyList<BidDto>>
{
    public async ValueTask<IReadOnlyList<BidDto>> Handle(GetTenderBidsQuery query, CancellationToken ct) =>
        (await bids.GetOfficialByTenderAsync(query.TenderId, ct)).Select(BidDto.From).ToList();
}
