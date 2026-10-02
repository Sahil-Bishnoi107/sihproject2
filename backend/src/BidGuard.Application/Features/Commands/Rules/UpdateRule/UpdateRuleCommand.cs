using System.Text.Json;
using BidGuard.Application.Common.Dtos;
using BidGuard.Domain.Enums;
using Mediator;

namespace BidGuard.Application.Features.Commands.Rules.UpdateRule;

public sealed record UpdateRuleCommand(
    Guid TenderId,
    Guid RuleId,
    string Code,
    string Title,
    RuleCategory Category,
    bool IsMandatory,
    decimal Weight,
    JsonElement Expression,
    string? SourceClause,
    int? SourcePage,
    ExemptionKind? ExemptionKind) : ICommand<RuleDto>;
