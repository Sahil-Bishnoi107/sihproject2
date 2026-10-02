using BidGuard.Application.Common.Dtos;
using BidGuard.Domain.Enums;
using Mediator;

namespace BidGuard.Application.Features.Commands.Bids.OverrideEvaluation;

/// <summary>Officer replaces one rule's machine outcome, with a mandatory reason; the bid is rescored.</summary>
public sealed record OverrideEvaluationCommand(Guid BidId, Guid EvaluationId, RuleOutcome Outcome, string Reason) : ICommand<BidDto>;
