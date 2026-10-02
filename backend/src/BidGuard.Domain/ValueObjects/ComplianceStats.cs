namespace BidGuard.Domain.ValueObjects;

public sealed record ComplianceStats(
    int TotalTenders,
    int PublishedTenders,
    int TotalBids,
    IReadOnlyDictionary<string, int> BidsByDecision,
    IReadOnlyList<RuleFailureStat> TopFailingRules);

public sealed record RuleFailureStat(string RuleCode, string RuleTitle, int Failures);
