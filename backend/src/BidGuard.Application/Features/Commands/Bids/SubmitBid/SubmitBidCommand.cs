using BidGuard.Application.Common.Dtos;
using Mediator;

namespace BidGuard.Application.Features.Commands.Bids.SubmitBid;

/// <summary>Lock the bid's documents and start extraction → verification → evaluation.</summary>
public sealed record SubmitBidCommand(Guid BidId) : ICommand<BidDto>;
