using BidGuard.Application.Common.Dtos;
using Mediator;

namespace BidGuard.Application.Features.Commands.Rules.ApproveRules;

/// <summary>Approve one AI-drafted rule (<paramref name="RuleId"/> set) or every draft on the tender (null).</summary>
public sealed record ApproveRulesCommand(Guid TenderId, Guid? RuleId) : ICommand<IReadOnlyList<RuleDto>>;
