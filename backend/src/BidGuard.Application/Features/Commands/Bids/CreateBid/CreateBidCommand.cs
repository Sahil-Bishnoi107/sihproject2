using BidGuard.Application.Common.Dtos;
using Mediator;

namespace BidGuard.Application.Features.Commands.Bids.CreateBid;

/// <summary>Start an official bid, or a private pre-bid simulation when <paramref name="IsSimulation"/> is true.</summary>
public sealed record CreateBidCommand(Guid TenderId, bool IsSimulation) : ICommand<BidDto>;
