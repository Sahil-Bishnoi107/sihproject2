using BidGuard.Application.Common.Dtos;
using Mediator;

namespace BidGuard.Application.Features.Queries.Bids.GetMyBids;

/// <summary>The caller organization's bids and simulations, newest first.</summary>
public sealed record GetMyBidsQuery : IQuery<IReadOnlyList<BidDto>>;
