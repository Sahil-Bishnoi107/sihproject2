using BidGuard.Domain.Enums;

namespace BidGuard.Application.Services.Rules;

public sealed record RuleResult(RuleOutcome Outcome, string Explanation, IReadOnlyList<string> UsedKeys);
