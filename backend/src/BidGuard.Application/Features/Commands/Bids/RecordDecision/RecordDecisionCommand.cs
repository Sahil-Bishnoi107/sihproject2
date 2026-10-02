using BidGuard.Application.Common.Dtos;
using BidGuard.Domain.Enums;
using Mediator;

namespace BidGuard.Application.Features.Commands.Bids.RecordDecision;

/// <summary>The officer's final, human decision on an official bid.</summary>
public sealed record RecordDecisionCommand(Guid BidId, BidDecision Decision, string? Remarks) : ICommand<BidDto>;
