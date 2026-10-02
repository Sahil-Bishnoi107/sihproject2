using BidGuard.Domain.Common;
using BidGuard.Domain.Enums;

namespace BidGuard.Domain.Entities;

/// <summary>The deterministic verdict for one rule on one bid, optionally overridden by an officer.</summary>
public class RuleEvaluation : BaseEntity
{
    private RuleEvaluation() { }

    public RuleEvaluation(Guid bidId, Guid ruleId, RuleOutcome outcome, string explanation, string evidenceJson)
    {
        BidId = bidId;
        RuleId = ruleId;
        Outcome = outcome;
        Explanation = explanation;
        EvidenceJson = evidenceJson;
    }

    public Guid BidId { get; private set; }
    public Guid RuleId { get; private set; }
    public ComplianceRule Rule { get; private set; } = default!;
    public RuleOutcome Outcome { get; private set; }
    public string Explanation { get; private set; } = default!;
    /// <summary>JSON array of evidence items (field key, value, document, page, bbox, verification).</summary>
    public string EvidenceJson { get; private set; } = default!;
    public RuleOutcome? OfficerOverride { get; private set; }
    public string? OverrideReason { get; private set; }
    public Guid? ReviewedById { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    public RuleOutcome EffectiveOutcome => OfficerOverride ?? Outcome;

    public void Override(RuleOutcome outcome, string reason, Guid officerId)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("An override requires a reason.");
        OfficerOverride = outcome;
        OverrideReason = reason.Trim();
        ReviewedById = officerId;
        ReviewedAt = DateTime.UtcNow;
        Touch();
    }
}
