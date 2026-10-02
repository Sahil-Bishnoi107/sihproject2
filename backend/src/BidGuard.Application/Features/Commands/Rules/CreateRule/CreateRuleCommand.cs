using System.Text.Json;
using BidGuard.Application.Common.Dtos;
using BidGuard.Domain.Enums;
using Mediator;

namespace BidGuard.Application.Features.Commands.Rules.CreateRule;

/// <summary>Officer-authored rule; approved on creation.</summary>
public sealed record CreateRuleCommand(
    Guid TenderId,
    string Code,
    string Title,
    RuleCategory Category,
    bool IsMandatory,
    decimal Weight,
    JsonElement Expression,
    string? SourceClause,
    int? SourcePage,
    ExemptionKind? ExemptionKind) : ICommand<RuleDto>;
