using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;

namespace BidGuard.Application.Services.Evaluation;

public sealed record ScoreResult(decimal Score, BidDecision Decision);

public interface IScoringService
{
    ScoreResult Score(IReadOnlyCollection<(ComplianceRule Rule, RuleOutcome Outcome)> outcomes);
}

/// <summary>
/// Score = weighted share of rules satisfied (Pass or Exempted), 0–100.
/// Decision: any mandatory Fail ⇒ NonCompliant; else any mandatory Missing ⇒ NeedsClarification; else Compliant.
/// </summary>
public sealed class ScoringService : IScoringService
{
    public ScoreResult Score(IReadOnlyCollection<(ComplianceRule Rule, RuleOutcome Outcome)> outcomes)
    {
        var totalWeight = outcomes.Sum(o => o.Rule.Weight);
        var satisfiedWeight = outcomes
            .Where(o => o.Outcome is RuleOutcome.Pass or RuleOutcome.Exempted)
            .Sum(o => o.Rule.Weight);

        var score = totalWeight == 0 ? 100m : Math.Round(satisfiedWeight / totalWeight * 100m, 2, MidpointRounding.AwayFromZero);

        var decision =
            outcomes.Any(o => o.Rule.IsMandatory && o.Outcome == RuleOutcome.Fail) ? BidDecision.NonCompliant
            : outcomes.Any(o => o.Rule.IsMandatory && o.Outcome == RuleOutcome.Missing) ? BidDecision.NeedsClarification
            : BidDecision.Compliant;

        return new ScoreResult(score, decision);
    }
}
