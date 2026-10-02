using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Features.Commands.Bids.OverrideEvaluation;
using BidGuard.Application.Features.Commands.Bids.RecordDecision;
using BidGuard.Application.Features.Queries.Bids.GetBidReport;
using BidGuard.Application.Jobs;
using BidGuard.Application.Services.Bids;
using BidGuard.Application.Services.Evaluation;
using BidGuard.Application.Tests.TestSupport;
using BidGuard.Domain.Common;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BidGuard.Application.Tests.Review;

public class ReviewFeatureTests : IDisposable
{
    private readonly TestHarness _h = new();
    private readonly Organization _org = new("Acme", "AAACA1111A", OrganizationCategory.Medium);
    private readonly User _officer = new("officer@x.in", "h", "Officer", UserRole.Officer);
    private readonly User _bidder;
    private readonly Tender _tender;
    private readonly ComplianceRule _mandatory;
    private readonly ComplianceRule _optional;

    public ReviewFeatureTests()
    {
        _bidder = new User("b@acme.in", "h", "Bidder", UserRole.Bidder, _org.Id);
        _tender = new Tender("GEM/2026/B/9", "T", "C", 1_000_000m, 0m, DateTime.UtcNow.AddDays(5), _officer.Id);
        _mandatory = ComplianceRule.CreateApproved(_tender.Id, "M1", "ISO 9001 certificate", RuleCategory.Technical, true, 3m, """{"field":"iso9001.number","op":"exists"}""");
        _optional = ComplianceRule.CreateApproved(_tender.Id, "O1", "Past experience 3 years", RuleCategory.Eligibility, false, 1m, """{"field":"experience.years","op":">=","value":3}""");
        _h.Db.AddRange(_org, _officer, _bidder, _tender, _mandatory, _optional);
        _h.Db.SaveChanges();
    }

    private Bid UnderReviewBid(bool simulation, RuleOutcome mandatory, RuleOutcome optional)
    {
        var bid = new Bid(_tender.Id, _org.Id, simulation);
        bid.StartExtraction();
        bid.StartVerification();
        bid.StartEvaluation();
        var score = new ScoringService().Score([(_mandatory, mandatory), (_optional, optional)]);
        bid.MarkUnderReview(score.Score, score.Decision);
        _h.Db.Bids.Add(bid);
        _h.Db.RuleEvaluations.AddRange(
            new RuleEvaluation(bid.Id, _mandatory.Id, mandatory, "iso9001.number not found in submitted documents", "[]"),
            new RuleEvaluation(bid.Id, _optional.Id, optional, "experience.years is 2; required >= 3", "[]"));
        _h.Db.SaveChanges();
        return bid;
    }

    private void ActAsOfficer()
    {
        _h.CurrentUser.UserId = _officer.Id;
        _h.CurrentUser.Role = UserRole.Officer;
        _h.CurrentUser.OrganizationId = null;
    }

    private void ActAsBidder(Guid? orgId = null)
    {
        _h.CurrentUser.UserId = _bidder.Id;
        _h.CurrentUser.Role = UserRole.Bidder;
        _h.CurrentUser.OrganizationId = orgId ?? _org.Id;
    }

    private GetBidReportQueryHandler ReportHandler() =>
        new(_h.Bids, _h.Rules, _h.Evaluations, _h.Verifications, _h.Documents, _h.CurrentUser);

    [Fact]
    public async Task Officer_override_rescored_bid_and_is_audited()
    {
        var bid = UnderReviewBid(false, RuleOutcome.Missing, RuleOutcome.Pass);
        ActAsOfficer();
        var evaluation = await _h.Db.RuleEvaluations.SingleAsync(e => e.BidId == bid.Id && e.RuleId == _mandatory.Id);

        await new OverrideEvaluationCommandHandler(_h.Bids, _h.Rules, _h.Evaluations, new ScoringService(), _h.CurrentUser, _h.Audit, _h.Db)
            .Handle(new OverrideEvaluationCommand(bid.Id, evaluation.Id, RuleOutcome.Pass, "ISO certificate seen on page 7 of annexure"), CancellationToken.None);

        var reloaded = await _h.Db.Bids.SingleAsync(b => b.Id == bid.Id);
        reloaded.OverallScore.Should().Be(100m);
        reloaded.SuggestedDecision.Should().Be(BidDecision.Compliant);
        (await _h.Db.AuditLogs.AnyAsync(a => a.Action == "EvaluationOverridden")).Should().BeTrue();
    }

    [Fact]
    public async Task Decision_is_recorded_and_bidder_notified()
    {
        var bid = UnderReviewBid(false, RuleOutcome.Pass, RuleOutcome.Pass);
        ActAsOfficer();

        await new RecordDecisionCommandHandler(_h.Bids, _h.Users, _h.Notifications, _h.CurrentUser, _h.Audit, _h.Db)
            .Handle(new RecordDecisionCommand(bid.Id, BidDecision.Compliant, "All criteria met"), CancellationToken.None);

        (await _h.Db.Bids.SingleAsync(b => b.Id == bid.Id)).Status.Should().Be(BidStatus.Decided);
        (await _h.Db.Notifications.AnyAsync(n => n.UserId == _bidder.Id && n.Type == NotificationType.BidDecided)).Should().BeTrue();
    }

    [Fact]
    public async Task Officers_cannot_see_simulations()
    {
        var bid = UnderReviewBid(true, RuleOutcome.Pass, RuleOutcome.Pass);
        ActAsOfficer();

        var act = async () => await ReportHandler().Handle(new GetBidReportQuery(bid.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Other_organizations_cannot_see_a_bid()
    {
        var bid = UnderReviewBid(false, RuleOutcome.Pass, RuleOutcome.Pass);
        ActAsBidder(Guid.NewGuid());

        var act = async () => await ReportHandler().Handle(new GetBidReportQuery(bid.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Report_lists_fixes_ranked_by_score_gain()
    {
        var bid = UnderReviewBid(true, RuleOutcome.Missing, RuleOutcome.Fail);
        ActAsBidder();

        var report = await ReportHandler().Handle(new GetBidReportQuery(bid.Id), CancellationToken.None);

        report.Evaluations.Should().HaveCount(2);
        report.Fixes.Select(f => f.RuleCode).Should().Equal("M1", "O1");
        report.Fixes[0].ScoreGain.Should().Be(75m);
        report.Fixes[0].Action.Should().Contain("ISO 9001");
        report.Fixes[0].IsMandatory.Should().BeTrue();
    }

    [Fact]
    public void Simulation_cannot_receive_a_decision()
    {
        var bid = UnderReviewBid(true, RuleOutcome.Pass, RuleOutcome.Pass);

        FluentActions.Invoking(() => bid.Decide(BidDecision.Compliant)).Should().Throw<DomainException>();
    }

    [Fact]
    public void Access_rules_for_each_role()
    {
        var official = new Bid(_tender.Id, _org.Id, false);
        var simulation = new Bid(_tender.Id, _org.Id, true);
        ActAsOfficer();
        FluentActions.Invoking(() => BidAccess.EnsureCanRead(official, _h.CurrentUser)).Should().NotThrow();
        FluentActions.Invoking(() => BidAccess.EnsureCanRead(simulation, _h.CurrentUser)).Should().Throw<NotFoundException>();
        ActAsBidder();
        FluentActions.Invoking(() => BidAccess.EnsureCanRead(simulation, _h.CurrentUser)).Should().NotThrow();
        FluentActions.Invoking(() => BidAccess.EnsureOwner(official, _h.CurrentUser)).Should().NotThrow();
    }

    public void Dispose() => _h.Dispose();
}

public class CredentialExpiryAlertJobTests : IDisposable
{
    private readonly TestHarness _h = new();

    [Fact]
    public async Task Alerts_once_for_credentials_expiring_within_30_days()
    {
        var org = new Organization("Acme", "AAACA1111A", OrganizationCategory.MSE);
        var bidder = new User("b@acme.in", "h", "B", UserRole.Bidder, org.Id);
        var today = DateOnly.FromDateTime(_h.Time.GetUtcNow().UtcDateTime);
        org.AddCredential(CredentialType.ISO9001, "ISO-1", today.AddYears(-2), today.AddDays(10));
        org.AddCredential(CredentialType.GST, "GST-1", today.AddYears(-2), today.AddDays(90));
        _h.Db.AddRange(org, bidder);
        _h.Db.SaveChanges();
        var job = new CredentialExpiryAlertJob(_h.Credentials, _h.Users, _h.Notifications, _h.Db, _h.Time);

        await job.RunAsync(CancellationToken.None);
        await job.RunAsync(CancellationToken.None);

        var alerts = await _h.Db.Notifications.Where(n => n.Type == NotificationType.CredentialExpiring).ToListAsync();
        alerts.Should().ContainSingle().Which.Message.Should().Contain("ISO-1");
    }

    [Fact]
    public async Task Alerts_again_after_seven_days()
    {
        var org = new Organization("Acme", "AAACA1111A", OrganizationCategory.MSE);
        var bidder = new User("b@acme.in", "h", "B", UserRole.Bidder, org.Id);
        var today = DateOnly.FromDateTime(_h.Time.GetUtcNow().UtcDateTime);
        org.AddCredential(CredentialType.ISO9001, "ISO-1", null, today.AddDays(20));
        _h.Db.AddRange(org, bidder);
        _h.Db.SaveChanges();
        var job = new CredentialExpiryAlertJob(_h.Credentials, _h.Users, _h.Notifications, _h.Db, _h.Time);

        await job.RunAsync(CancellationToken.None);
        _h.Time.Advance(TimeSpan.FromDays(8));
        await job.RunAsync(CancellationToken.None);

        (await _h.Db.Notifications.CountAsync(n => n.Type == NotificationType.CredentialExpiring)).Should().Be(2);
    }

    public void Dispose() => _h.Dispose();
}
