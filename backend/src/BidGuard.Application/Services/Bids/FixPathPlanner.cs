using BidGuard.Application.Common.Dtos;
using BidGuard.Domain.Enums;

namespace BidGuard.Application.Services.Bids;

/// <summary>
/// Turns unmet rules into an ordered fix list: mandatory gaps first (they decide compliance),
/// then by how many score points each fix recovers.
/// </summary>
public static class FixPathPlanner
{
    public static IReadOnlyList<FixDto> Plan(IReadOnlyList<EvaluationDto> evaluations)
    {
        var totalWeight = evaluations.Sum(e => e.Weight);
        if (totalWeight == 0) return [];

        return evaluations
            .Where(e => e.EffectiveOutcome is RuleOutcome.Fail or RuleOutcome.Missing)
            .Select(e => new FixDto(
                e.RuleCode,
                e.RuleTitle,
                e.IsMandatory,
                e.EffectiveOutcome,
                Math.Round(e.Weight / totalWeight * 100m, 2, MidpointRounding.AwayFromZero),
                e.EffectiveOutcome == RuleOutcome.Missing
                    ? $"Provide a document that shows: {e.RuleTitle}."
                    : $"Meet the requirement \"{e.RuleTitle}\" ({e.Explanation})."))
            .OrderByDescending(f => f.IsMandatory)
            .ThenByDescending(f => f.ScoreGain)
            .ToList();
    }
}
