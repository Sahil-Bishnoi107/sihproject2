using BidGuard.Application.Common.Dtos;
using Mediator;

namespace BidGuard.Application.Features.Queries.Bids.GetTenderBids;

/// <summary>Officer view: every official bid on a tender, highest score first. Simulations are never listed.</summary>
public sealed record GetTenderBidsQuery(Guid TenderId) : IQuery<IReadOnlyList<BidDto>>;
