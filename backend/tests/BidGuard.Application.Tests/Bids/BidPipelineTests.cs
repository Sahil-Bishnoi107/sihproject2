using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Common.Interfaces.Verification;
using BidGuard.Application.Features.Commands.Bids.CreateBid;
using BidGuard.Application.Features.Commands.Bids.SubmitBid;
using BidGuard.Application.Features.Commands.Bids.UploadBidDocument;
using BidGuard.Application.Jobs;
using BidGuard.Application.Services.Bids;
using BidGuard.Application.Services.Documents;
using BidGuard.Application.Services.Evaluation;
using BidGuard.Application.Services.Rules;
using BidGuard.Application.Tests.TestSupport;
using BidGuard.Domain.Common;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Infrastructure.ExternalServices.MockGov;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BidGuard.Application.Tests.Bids;

public class BidPipelineTests : IDisposable
{
    private readonly TestHarness _h = new();
    private readonly Organization _org;
    private readonly User _bidder;
    private readonly User _officer;
    private readonly Tender _tender;

    public BidPipelineTests()
    {
        _org = new Organization("Shakti Enterprises Pvt Ltd", "AAECS1234K", OrganizationCategory.MSE);
        _org.UpdateProfile("Shakti Enterprises Pvt Ltd", OrganizationCategory.MSE, "27AAECS1234K1Z5", "UDYAM-MH-26-0012345",
            null, null, isStartup: false, dpiitNumber: null);
        _bidder = new User("bidder@shakti.in", "h", "Bidder", UserRole.Bidder, _org.Id);
        _officer = new User("officer@x.in", "h", "Officer", UserRole.Officer);
        _tender = new Tender("GEM/2026/B/7001", "Laptops", "IT", 10_000_000m, 200_000m, DateTime.UtcNow.AddDays(10), _officer.Id);
        _tender.StartExtraction();
        _tender.MarkRulesExtracted();

        var rules = new[]
        {
            ComplianceRule.CreateApproved(_tender.Id, "FIN-EMD", "EMD paid", RuleCategory.Financial, true, 1m,
                """{"field":"emd.paid","op":"exists"}""", exemptionKind: ExemptionKind.Emd),
            ComplianceRule.CreateApproved(_tender.Id, "FIN-TURNOVER", "Average turnover >= 50 lakh", RuleCategory.Financial, true, 2m,
                """{"field":"turnover.avg3y","op":">=","value":5000000}"""),
            ComplianceRule.CreateApproved(_tender.Id, "ELG-GST", "GST registration verified", RuleCategory.Eligibility, true, 1m,
                """{"field":"gstin","op":"verified","source":"GST"}"""),
        };
        _tender.Publish(rules);

        _h.Db.AddRange(_org, _bidder, _officer, _tender);
        _h.Db.ComplianceRules.AddRange(rules);
        _h.Db.SaveChanges();

        _h.CurrentUser.UserId = _bidder.Id;
        _h.CurrentUser.Role = UserRole.Bidder;
        _h.CurrentUser.OrganizationId = _org.Id;
    }

    private DocumentUploader Uploader => new(_h.Storage, _h.Documents);
    private BidPipeline Pipeline => new(_h.Scheduler);

    private IRegistryVerificationService[] Registries
    {
        get
        {
            var o = Options.Create(new MockGovSettings { LatencyMs = 0 });
            return [new MockGstVerificationService(o), new MockMcaVerificationService(o), new MockUdyamVerificationService(o), new MockEpfoVerificationService(o)];
        }
    }

    private async Task<Guid> CreateAndSubmitAsync(params ExtractedFieldDto[] fields)
    {
        var bid = await new CreateBidCommandHandler(_h.Tenders, _h.Bids, _h.Organizations, _h.CurrentUser, _h.Audit, _h.Db, _h.Time)
            .Handle(new CreateBidCommand(_tender.Id, IsSimulation: false), CancellationToken.None);
        await new UploadBidDocumentCommandHandler(_h.Bids, Uploader, _h.CurrentUser, _h.Audit, _h.Db)
            .Handle(new UploadBidDocumentCommand(bid.Id, "bid.pdf", "application/pdf", new MemoryStream([1, 2, 3]), 3, "financials"),
                CancellationToken.None);
        _h.Ai.EntitiesByFile["bid.pdf"] = new EntityExtractionResult(false, fields);
        await new SubmitBidCommandHandler(_h.Bids, _h.Documents, Pipeline, _h.CurrentUser, _h.Audit, _h.Db)
            .Handle(new SubmitBidCommand(bid.Id), CancellationToken.None);
        return bid.Id;
    }

    private async Task RunPipelineAsync(Guid bidId)
    {
        await new ExtractBidEntitiesJob(_h.Bids, _h.Documents, _h.ExtractedFields, _h.Notifications, _h.Users, _h.Storage, _h.Ai, _h.Audit, _h.Db,
            NullLogger<ExtractBidEntitiesJob>.Instance).RunAsync(bidId, CancellationToken.None);
        await new VerifyBidCredentialsJob(_h.Bids, _h.ExtractedFields, _h.Verifications, Registries, _h.Cache, _h.Audit, _h.Db, _h.Time,
            NullLogger<VerifyBidCredentialsJob>.Instance).RunAsync(bidId, CancellationToken.None);
        await new EvaluateBidJob(_h.Bids, _h.Rules, _h.ExtractedFields, _h.Verifications, _h.Evaluations, _h.Documents, _h.Notifications,
            _h.Users, new RuleEngine(), new ExemptionService(), new ScoringService(), _h.Audit, _h.Db,
            NullLogger<EvaluateBidJob>.Instance).RunAsync(bidId, CancellationToken.None);
    }

    private static ExtractedFieldDto F(string key, string value, decimal conf = 0.95m) => new(key, value, conf, 1, [10, 20, 110, 40]);

    [Fact]
    public async Task Submit_queues_the_three_job_chain()
    {
        await CreateAndSubmitAsync(F("gstin", "27AAECS1234K1Z5"));

        _h.Scheduler.HasJob<ExtractBidEntitiesJob>().Should().BeTrue();
        _h.Scheduler.HasJob<VerifyBidCredentialsJob>().Should().BeTrue();
        _h.Scheduler.HasJob<EvaluateBidJob>().Should().BeTrue();
    }

    [Fact]
    public async Task Verified_MSE_bid_is_compliant_with_EMD_exempted()
    {
        var bidId = await CreateAndSubmitAsync(F("gstin", "27AAECS1234K1Z5"), F("udyam", "UDYAM-MH-26-0012345"), F("turnover.avg3y", "60,00,000"));

        await RunPipelineAsync(bidId);

        var bid = await _h.Db.Bids.SingleAsync(b => b.Id == bidId);
        bid.Status.Should().Be(BidStatus.UnderReview);
        bid.SuggestedDecision.Should().Be(BidDecision.Compliant);
        bid.OverallScore.Should().Be(100m);
        var evals = await _h.Db.RuleEvaluations.Include(e => e.Rule).Where(e => e.BidId == bidId).ToListAsync();
        evals.Should().HaveCount(3);
        evals.Single(e => e.Rule.Code == "FIN-EMD").Outcome.Should().Be(RuleOutcome.Exempted);
        evals.Single(e => e.Rule.Code == "FIN-TURNOVER").EvidenceJson.Should().Contain("turnover.avg3y").And.Contain("\"page\":1");
        (await _h.Db.Notifications.AnyAsync(n => n.UserId == _officer.Id && n.Type == NotificationType.BidEvaluated)).Should().BeTrue();
    }

    [Fact]
    public async Task Low_turnover_is_non_compliant()
    {
        var bidId = await CreateAndSubmitAsync(F("gstin", "27AAECS1234K1Z5"), F("udyam", "UDYAM-MH-26-0012345"), F("turnover.avg3y", "3000000"));

        await RunPipelineAsync(bidId);

        (await _h.Db.Bids.SingleAsync(b => b.Id == bidId)).SuggestedDecision.Should().Be(BidDecision.NonCompliant);
    }

    [Fact]
    public async Task GSTIN_in_documents_differing_from_profile_is_a_mismatch()
    {
        var bidId = await CreateAndSubmitAsync(F("gstin", "29AABCN5678P1Z2"), F("udyam", "UDYAM-MH-26-0012345"), F("turnover.avg3y", "6000000"));

        await RunPipelineAsync(bidId);

        var gst = await _h.Db.VerificationResults.SingleAsync(v => v.BidId == bidId && v.Source == VerificationSource.GST);
        gst.Status.Should().Be(VerificationStatus.Mismatch);
        (await _h.Db.Bids.SingleAsync(b => b.Id == bidId)).SuggestedDecision.Should().Be(BidDecision.NonCompliant);
    }

    [Fact]
    public async Task Missing_turnover_needs_clarification()
    {
        var bidId = await CreateAndSubmitAsync(F("gstin", "27AAECS1234K1Z5"), F("udyam", "UDYAM-MH-26-0012345"));

        await RunPipelineAsync(bidId);

        (await _h.Db.Bids.SingleAsync(b => b.Id == bidId)).SuggestedDecision.Should().Be(BidDecision.NeedsClarification);
    }

    [Fact]
    public async Task Permanent_extraction_failure_marks_bid_failed()
    {
        var bidId = await CreateAndSubmitAsync(F("gstin", "27AAECS1234K1Z5"));
        var job = new ExtractBidEntitiesJob(_h.Bids, _h.Documents, _h.ExtractedFields, _h.Notifications, _h.Users, _h.Storage, _h.Ai,
            _h.Audit, _h.Db, NullLogger<ExtractBidEntitiesJob>.Instance);

        await job.OnPermanentFailureAsync([bidId, CancellationToken.None], "sidecar unreachable", CancellationToken.None);

        var bid = await _h.Db.Bids.SingleAsync(b => b.Id == bidId);
        bid.Status.Should().Be(BidStatus.Failed);
        bid.FailureReason.Should().Contain("sidecar unreachable");
    }

    [Fact]
    public async Task Submitting_without_documents_is_rejected()
    {
        var bid = await new CreateBidCommandHandler(_h.Tenders, _h.Bids, _h.Organizations, _h.CurrentUser, _h.Audit, _h.Db, _h.Time)
            .Handle(new CreateBidCommand(_tender.Id, false), CancellationToken.None);

        var act = async () => await new SubmitBidCommandHandler(_h.Bids, _h.Documents, Pipeline, _h.CurrentUser, _h.Audit, _h.Db)
            .Handle(new SubmitBidCommand(bid.Id), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Second_official_bid_from_same_organization_conflicts()
    {
        var handler = new CreateBidCommandHandler(_h.Tenders, _h.Bids, _h.Organizations, _h.CurrentUser, _h.Audit, _h.Db, _h.Time);
        await handler.Handle(new CreateBidCommand(_tender.Id, false), CancellationToken.None);

        var act = async () => await handler.Handle(new CreateBidCommand(_tender.Id, false), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Another_organization_cannot_upload_to_the_bid()
    {
        var bid = await new CreateBidCommandHandler(_h.Tenders, _h.Bids, _h.Organizations, _h.CurrentUser, _h.Audit, _h.Db, _h.Time)
            .Handle(new CreateBidCommand(_tender.Id, false), CancellationToken.None);
        _h.CurrentUser.OrganizationId = Guid.NewGuid();

        var act = async () => await new UploadBidDocumentCommandHandler(_h.Bids, Uploader, _h.CurrentUser, _h.Audit, _h.Db)
            .Handle(new UploadBidDocumentCommand(bid.Id, "x.pdf", "application/pdf", new MemoryStream([1]), 1, null), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task My_bids_include_tender_title_and_GeM_number()
    {
        await CreateAndSubmitAsync(F("gstin", "27AAECS1234K1Z5"));

        var mine = await new BidGuard.Application.Features.Queries.Bids.GetMyBids.GetMyBidsQueryHandler(_h.Bids, _h.CurrentUser)
            .Handle(new BidGuard.Application.Features.Queries.Bids.GetMyBids.GetMyBidsQuery(), CancellationToken.None);

        mine.Should().ContainSingle();
        mine[0].GemBidNumber.Should().Be("GEM/2026/B/7001");
        mine[0].TenderTitle.Should().Be("Laptops");
    }

    public void Dispose() => _h.Dispose();
}

public class FactSetBuilderTests
{
    private static readonly Tender Tender = new("GEM/2026/B/1", "T", "C", 1_000_000m, 20_000m, new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc), Guid.NewGuid());

    private static ExtractedField Field(string key, string value, decimal conf) => new(Guid.Empty, Guid.Empty, key, value, conf, 1, null);

    [Fact]
    public void Highest_confidence_value_wins()
    {
        var org = new Organization("A", "AAAAA1111A", OrganizationCategory.MSE);

        var facts = FactSetBuilder.Build(Tender, org, [Field("gstin", "LOW", 0.4m), Field("gstin", "HIGH", 0.9m)], []);

        facts.TryGetValue("gstin", out var v).Should().BeTrue();
        v.Should().Be("HIGH");
    }

    [Fact]
    public void Three_year_average_is_derived_from_fiscal_years()
    {
        var org = new Organization("A", "AAAAA1111A", OrganizationCategory.MSE);

        var facts = FactSetBuilder.Build(Tender, org,
            [Field("turnover.fy2022", "40,00,000", 0.9m), Field("turnover.fy2023", "50,00,000", 0.9m), Field("turnover.fy2024", "60,00,000", 0.9m),
             Field("turnover.fy2021", "1", 0.9m)], []);

        facts.TryGetValue("turnover.avg3y", out var avg).Should().BeTrue();
        decimal.Parse(avg).Should().Be(5_000_000m);
    }

    [Fact]
    public void Profile_and_tender_variables_are_exposed()
    {
        var org = new Organization("A", "AAAAA1111A", OrganizationCategory.MSE);
        org.UpdateProfile("A", OrganizationCategory.MSE, null, null, null, null, true, "DIPP1");

        var facts = FactSetBuilder.Build(Tender, org, [],
            [new VerificationResult(Guid.Empty, VerificationSource.Udyam, "U", VerificationStatus.Verified, null, null, DateTime.UtcNow)]);

        facts.TryGetValue("org.category", out var cat).Should().BeTrue();
        cat.Should().Be("MSE");
        facts.TryGetValue("org.isStartup", out var s).Should().BeTrue();
        s.Should().Be("true");
        facts.TryGetVariable("bidEndDate", out var end).Should().BeTrue();
        end.Should().Be("2026-10-15");
        facts.TryGetVerification("Udyam", out var st).Should().BeTrue();
        st.Should().Be(VerificationStatus.Verified);
    }
}
