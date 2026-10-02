using BidGuard.Application.Services.Evaluation;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using FluentAssertions;

namespace BidGuard.Application.Tests.Services;

public class ExemptionServiceTests
{
    private readonly ExemptionService _service = new();

    private static ComplianceRule Rule(ExemptionKind? kind, bool mandatory = true, decimal weight = 1m) =>
        ComplianceRule.CreateApproved(Guid.NewGuid(), "R", "rule", RuleCategory.Financial, mandatory, weight,
            """{"field":"emd.paid","op":"exists"}""", exemptionKind: kind);

    private static Organization Org(OrganizationCategory category, bool startup = false, string? dpiit = null)
    {
        var org = new Organization("Acme", "AAAPA1234A", category);
        org.UpdateProfile("Acme", category, null, "UDYAM-MH-01-0000001", null, null, startup, dpiit);
        return org;
    }

    [Fact]
    public void Verified_MSE_is_exempt_from_EMD()
    {
        _service.GetExemption(Rule(ExemptionKind.Emd), Org(OrganizationCategory.MSE), udyamVerified: true)
            .Should().NotBeNull().And.Contain("MSE");
    }

    [Fact]
    public void MSE_without_verified_Udyam_is_not_exempt()
    {
        _service.GetExemption(Rule(ExemptionKind.Emd), Org(OrganizationCategory.MSE), udyamVerified: false)
            .Should().BeNull();
    }

    [Fact]
    public void DPIIT_startup_is_exempt_from_EMD_and_prior_experience()
    {
        var org = Org(OrganizationCategory.Medium, startup: true, dpiit: "DIPP12345");

        _service.GetExemption(Rule(ExemptionKind.Emd), org, false).Should().Contain("DPIIT");
        _service.GetExemption(Rule(ExemptionKind.PriorExperienceTurnover), org, false).Should().Contain("DPIIT");
    }

    [Fact]
    public void Startup_without_DPIIT_number_is_not_exempt()
    {
        var org = Org(OrganizationCategory.Medium, startup: true, dpiit: null);

        _service.GetExemption(Rule(ExemptionKind.PriorExperienceTurnover), org, false).Should().BeNull();
    }

    [Fact]
    public void MSE_is_not_exempt_from_prior_experience()
    {
        _service.GetExemption(Rule(ExemptionKind.PriorExperienceTurnover), Org(OrganizationCategory.MSE), true)
            .Should().BeNull();
    }

    [Fact]
    public void Rule_without_exemption_kind_is_never_exempt()
    {
        _service.GetExemption(Rule(null), Org(OrganizationCategory.MSE, true, "DIPP1"), true).Should().BeNull();
    }
}

public class ScoringServiceTests
{
    private readonly ScoringService _service = new();

    private static ComplianceRule Rule(bool mandatory, decimal weight) =>
        ComplianceRule.CreateApproved(Guid.NewGuid(), "R", "rule", RuleCategory.Eligibility, mandatory, weight,
            """{"field":"a","op":"exists"}""");

    [Fact]
    public void Weighted_score_counts_pass_and_exempted()
    {
        var result = _service.Score([
            (Rule(true, 3m), RuleOutcome.Pass),
            (Rule(false, 1m), RuleOutcome.Fail),
        ]);

        result.Score.Should().Be(75m);
        result.Decision.Should().Be(BidDecision.Compliant);
    }

    [Fact]
    public void Exempted_counts_as_satisfied()
    {
        var result = _service.Score([(Rule(true, 1m), RuleOutcome.Exempted), (Rule(true, 1m), RuleOutcome.Pass)]);

        result.Score.Should().Be(100m);
        result.Decision.Should().Be(BidDecision.Compliant);
    }

    [Fact]
    public void Mandatory_fail_is_non_compliant_even_with_missing()
    {
        var result = _service.Score([(Rule(true, 1m), RuleOutcome.Fail), (Rule(true, 1m), RuleOutcome.Missing)]);

        result.Decision.Should().Be(BidDecision.NonCompliant);
    }

    [Fact]
    public void Mandatory_missing_needs_clarification()
    {
        var result = _service.Score([(Rule(true, 1m), RuleOutcome.Missing), (Rule(false, 1m), RuleOutcome.Fail)]);

        result.Decision.Should().Be(BidDecision.NeedsClarification);
    }

    [Fact]
    public void No_rules_scores_100()
    {
        _service.Score([]).Score.Should().Be(100m);
    }

    [Fact]
    public void Score_is_rounded_to_two_decimals()
    {
        var result = _service.Score([
            (Rule(false, 1m), RuleOutcome.Pass), (Rule(false, 1m), RuleOutcome.Fail), (Rule(false, 1m), RuleOutcome.Fail),
        ]);

        result.Score.Should().Be(33.33m);
    }
}
