using BidGuard.Domain.Common;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using FluentAssertions;

namespace BidGuard.Application.Tests.Domain;

public class BidStateTests
{
    [Fact]
    public void StartExtraction_moves_Uploaded_bid_to_Extracting()
    {
        var bid = new Bid(Guid.NewGuid(), Guid.NewGuid(), isSimulation: false);

        bid.StartExtraction();

        bid.Status.Should().Be(BidStatus.Extracting);
    }

    [Fact]
    public void Decide_before_review_throws()
    {
        var bid = new Bid(Guid.NewGuid(), Guid.NewGuid(), isSimulation: false);
        bid.StartExtraction();

        FluentActions.Invoking(() => bid.Decide(BidDecision.Compliant))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void Full_pipeline_reaches_Decided()
    {
        var bid = new Bid(Guid.NewGuid(), Guid.NewGuid(), isSimulation: false);
        bid.StartExtraction();
        bid.StartVerification();
        bid.StartEvaluation();
        bid.MarkUnderReview(80m, BidDecision.Compliant);
        bid.Decide(BidDecision.NonCompliant);

        bid.Status.Should().Be(BidStatus.Decided);
        bid.Decision.Should().Be(BidDecision.NonCompliant);
        bid.OverallScore.Should().Be(80m);
    }

    [Fact]
    public void Fail_records_reason_from_any_processing_state()
    {
        var bid = new Bid(Guid.NewGuid(), Guid.NewGuid(), isSimulation: false);
        bid.StartExtraction();

        bid.Fail("sidecar unreachable");

        bid.Status.Should().Be(BidStatus.Failed);
        bid.FailureReason.Should().Be("sidecar unreachable");
    }

    [Fact]
    public void Failed_bid_can_be_resubmitted()
    {
        var bid = new Bid(Guid.NewGuid(), Guid.NewGuid(), isSimulation: false);
        bid.StartExtraction();
        bid.Fail("x");

        bid.StartExtraction();

        bid.Status.Should().Be(BidStatus.Extracting);
        bid.FailureReason.Should().BeNull();
    }
}

public class TenderStateTests
{
    private static Tender NewTender() =>
        new("GEM/2026/B/1", "Laptops", "IT Hardware", 1_000_000m, 20_000m, DateTime.UtcNow.AddDays(10), Guid.NewGuid());

    [Fact]
    public void Publish_with_unapproved_rule_throws()
    {
        var tender = NewTender();
        tender.StartExtraction();
        tender.MarkRulesExtracted();
        var rule = ComplianceRule.CreateDraft(tender.Id, "R1", "GST registration", RuleCategory.Eligibility, true, 1m, "{\"field\":\"gstin\",\"op\":\"exists\"}");

        FluentActions.Invoking(() => tender.Publish(new[] { rule }))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void Publish_with_no_rules_throws()
    {
        var tender = NewTender();
        tender.StartExtraction();
        tender.MarkRulesExtracted();

        FluentActions.Invoking(() => tender.Publish(Array.Empty<ComplianceRule>()))
            .Should().Throw<DomainException>();
    }

    [Fact]
    public void Publish_with_approved_rules_succeeds()
    {
        var tender = NewTender();
        tender.StartExtraction();
        tender.MarkRulesExtracted();
        var rule = ComplianceRule.CreateDraft(tender.Id, "R1", "GST registration", RuleCategory.Eligibility, true, 1m, "{\"field\":\"gstin\",\"op\":\"exists\"}");
        rule.Approve();

        tender.Publish(new[] { rule });

        tender.Status.Should().Be(TenderStatus.Published);
    }
}
