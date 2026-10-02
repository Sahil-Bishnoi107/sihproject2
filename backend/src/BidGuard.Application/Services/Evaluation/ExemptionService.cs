using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;

namespace BidGuard.Application.Services.Evaluation;

public interface IExemptionService
{
    /// <summary>Returns the exemption reason when the organization is relieved from the rule, otherwise null.</summary>
    string? GetExemption(ComplianceRule rule, Organization organization, bool udyamVerified);
}

/// <summary>
/// Statutory relaxations for GeM bids:
/// EMD — Micro &amp; Small Enterprises with a verified Udyam registration and DPIIT-recognised startups (GFR 2017 r.170);
/// prior experience/turnover — DPIIT-recognised startups (MoF OM, Public Procurement relaxation for startups).
/// </summary>
public sealed class ExemptionService : IExemptionService
{
    public string? GetExemption(ComplianceRule rule, Organization organization, bool udyamVerified) => rule.ExemptionKind switch
    {
        ExemptionKind.Emd when organization.Category == OrganizationCategory.MSE && udyamVerified =>
            $"EMD exempted: MSE with verified Udyam registration {organization.UdyamNumber}",
        ExemptionKind.Emd when organization.IsRecognisedStartup =>
            $"EMD exempted: DPIIT-recognised startup {organization.DpiitNumber}",
        ExemptionKind.PriorExperienceTurnover when organization.IsRecognisedStartup =>
            $"Prior experience/turnover relaxed: DPIIT-recognised startup {organization.DpiitNumber}",
        _ => null,
    };
}
