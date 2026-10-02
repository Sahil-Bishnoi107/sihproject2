using BidGuard.Domain.Common;
using BidGuard.Domain.Enums;

namespace BidGuard.Domain.Entities;

/// <summary>
/// Compliance-as-Code unit: one tender clause expressed as a machine-evaluable JSON expression.
/// </summary>
public class ComplianceRule : BaseEntity
{
    private ComplianceRule() { }

    private ComplianceRule(Guid tenderId, string code, string title, RuleCategory category, bool isMandatory,
        decimal weight, string expression, string? sourceClause, int? sourcePage, ExemptionKind? exemptionKind, RuleStatus status)
    {
        TenderId = tenderId;
        Status = status;
        Apply(code, title, category, isMandatory, weight, expression, sourceClause, sourcePage, exemptionKind);
    }

    public Guid TenderId { get; private set; }
    public string Code { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public RuleCategory Category { get; private set; }
    public bool IsMandatory { get; private set; }
    public decimal Weight { get; private set; }
    public string Expression { get; private set; } = default!;
    public string? SourceClause { get; private set; }
    public int? SourcePage { get; private set; }
    public ExemptionKind? ExemptionKind { get; private set; }
    public RuleStatus Status { get; private set; }

    public bool IsExemptionEligible => ExemptionKind.HasValue;

    public static ComplianceRule CreateDraft(Guid tenderId, string code, string title, RuleCategory category, bool isMandatory,
        decimal weight, string expression, string? sourceClause = null, int? sourcePage = null, ExemptionKind? exemptionKind = null) =>
        new(tenderId, code, title, category, isMandatory, weight, expression, sourceClause, sourcePage, exemptionKind, RuleStatus.AiDraft);

    /// <summary>Rules authored by an officer are approved on creation.</summary>
    public static ComplianceRule CreateApproved(Guid tenderId, string code, string title, RuleCategory category, bool isMandatory,
        decimal weight, string expression, string? sourceClause = null, int? sourcePage = null, ExemptionKind? exemptionKind = null) =>
        new(tenderId, code, title, category, isMandatory, weight, expression, sourceClause, sourcePage, exemptionKind, RuleStatus.Approved);

    public void Update(string code, string title, RuleCategory category, bool isMandatory, decimal weight, string expression,
        string? sourceClause, int? sourcePage, ExemptionKind? exemptionKind)
    {
        Apply(code, title, category, isMandatory, weight, expression, sourceClause, sourcePage, exemptionKind);
        Touch();
    }

    public void Approve()
    {
        Status = RuleStatus.Approved;
        Touch();
    }

    private void Apply(string code, string title, RuleCategory category, bool isMandatory, decimal weight, string expression,
        string? sourceClause, int? sourcePage, ExemptionKind? exemptionKind)
    {
        if (weight <= 0) throw new DomainException("Rule weight must be positive.");
        Code = code.Trim().ToUpperInvariant();
        Title = title.Trim();
        Category = category;
        IsMandatory = isMandatory;
        Weight = weight;
        Expression = expression;
        SourceClause = sourceClause;
        SourcePage = sourcePage;
        ExemptionKind = exemptionKind;
    }
}
