using Mediator;

namespace BidGuard.Application.Features.Commands.Rules.DeleteRule;

public sealed record DeleteRuleCommand(Guid TenderId, Guid RuleId) : ICommand;
