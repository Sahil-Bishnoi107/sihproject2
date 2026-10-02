using BidGuard.Application.Common.Dtos;
using Mediator;

namespace BidGuard.Application.Features.Queries.Bids.GetBidById;

public sealed record GetBidByIdQuery(Guid BidId) : IQuery<BidDto>;
