namespace BidGuard.Domain.Enums;

public enum RuleCategory { Eligibility, Technical, Financial, Document }

public enum RuleStatus { AiDraft, Approved }

/// <summary>Which statutory relaxation a rule is eligible for (GFR 2017 / MSE Procurement Policy / DPIIT startup policy).</summary>
public enum ExemptionKind { Emd, PriorExperienceTurnover }

public enum RuleOutcome { Pass, Fail, Exempted, Missing }
