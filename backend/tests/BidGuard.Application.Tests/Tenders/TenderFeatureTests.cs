using System.Text.Json;
using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Features.Commands.Rules.CreateRule;
using BidGuard.Application.Features.Commands.Tenders.CreateTender;
using BidGuard.Application.Features.Commands.Tenders.PublishTender;
using BidGuard.Application.Features.Commands.Tenders.UploadTenderDocument;
using BidGuard.Application.Features.Queries.Tenders.GetTenders;
using BidGuard.Application.Jobs;
using BidGuard.Application.Services.Documents;
using BidGuard.Application.Tests.TestSupport;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BidGuard.Application.Tests.Tenders;

public class TenderFeatureTests : IDisposable
{
    private readonly TestHarness _h = new();
    private readonly User _officer;

    public TenderFeatureTests()
    {
        _officer = new User("officer@x.in", "h", "Officer", UserRole.Officer);
        _h.Db.Users.Add(_officer);
        _h.Db.SaveChanges();
        _h.CurrentUser.UserId = _officer.Id;
        _h.CurrentUser.Role = UserRole.Officer;
    }

    private DocumentUploader Uploader => new(_h.Storage, _h.Documents);

    private async Task<Guid> CreateTenderAsync(string gem = "GEM/2026/B/5551234")
    {
        var dto = await new CreateTenderCommandHandler(_h.Tenders, _h.CurrentUser, _h.Audit, _h.Db)
            .Handle(new CreateTenderCommand(gem, "Supply of 200 laptops", "IT Hardware", 12_000_000m, 240_000m,
                DateTime.UtcNow.AddDays(20)), CancellationToken.None);
        return dto.Id;
    }

    private async Task UploadAsync(Guid tenderId) =>
        await new UploadTenderDocumentCommandHandler(_h.Tenders, Uploader, _h.Scheduler, _h.CurrentUser, _h.Audit, _h.Db)
            .Handle(new UploadTenderDocumentCommand(tenderId, "tender.pdf", "application/pdf", new MemoryStream([1, 2, 3]), 3),
                CancellationToken.None);

    private ExtractTenderRulesJob Job() =>
        new(_h.Tenders, _h.Documents, _h.Rules, _h.Notifications, _h.Storage, _h.Ai, _h.Audit, _h.Db, NullLogger<ExtractTenderRulesJob>.Instance);

    [Fact]
    public async Task Duplicate_GeM_bid_number_conflicts()
    {
        await CreateTenderAsync();

        var act = () => CreateTenderAsync();

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Uploading_document_stores_file_and_queues_extraction()
    {
        var id = await CreateTenderAsync();

        await UploadAsync(id);

        var tender = await _h.Db.Tenders.SingleAsync(t => t.Id == id);
        tender.Status.Should().Be(TenderStatus.Extracting);
        tender.TenderDocumentId.Should().NotBeNull();
        _h.Storage.Objects.Should().ContainSingle();
        _h.Scheduler.HasJob<ExtractTenderRulesJob>().Should().BeTrue();
    }

    [Fact]
    public async Task Extraction_with_no_rules_still_reaches_RulesExtracted()
    {
        var id = await CreateTenderAsync();
        await UploadAsync(id);
        _h.Ai.Rules = [];

        await Job().RunAsync(id, CancellationToken.None);

        (await _h.Db.Tenders.SingleAsync(t => t.Id == id)).Status.Should().Be(TenderStatus.RulesExtracted);
        (await _h.Db.ComplianceRules.CountAsync()).Should().Be(0);
        (await _h.Db.Notifications.AnyAsync(n => n.UserId == _officer.Id && n.Type == NotificationType.RulesReady)).Should().BeTrue();
    }

    [Fact]
    public async Task Extraction_creates_AI_draft_rules_with_unique_codes()
    {
        var id = await CreateTenderAsync();
        await UploadAsync(id);
        _h.Ai.Rules =
        [
            new("ELG-GST", "GST registration", RuleCategory.Eligibility, true, 1, """{"field":"gstin","op":"exists"}""", "GSTIN required", 2, null),
            new("ELG-GST", "GST verified", RuleCategory.Eligibility, true, 1, """{"field":"gstin","op":"verified","source":"GST"}""", null, 2, null),
            new("BAD", "Broken", RuleCategory.Document, false, 1, """{"field":"x","op":"nope"}""", null, null, null),
        ];

        await Job().RunAsync(id, CancellationToken.None);

        var rules = await _h.Db.ComplianceRules.Where(r => r.TenderId == id).ToListAsync();
        rules.Should().HaveCount(2, "invalid AI expressions are dropped");
        rules.Select(r => r.Code).Should().OnlyHaveUniqueItems();
        rules.Should().OnlyContain(r => r.Status == RuleStatus.AiDraft);
    }

    [Fact]
    public async Task Permanent_failure_returns_tender_to_Draft_with_reason()
    {
        var id = await CreateTenderAsync();
        await UploadAsync(id);

        await Job().OnPermanentFailureAsync([id, CancellationToken.None], "sidecar unreachable", CancellationToken.None);

        var tender = await _h.Db.Tenders.SingleAsync(t => t.Id == id);
        tender.Status.Should().Be(TenderStatus.Draft);
        tender.FailureReason.Should().Contain("sidecar unreachable");
    }

    [Fact]
    public async Task Publish_requires_all_rules_approved()
    {
        var id = await CreateTenderAsync();
        await UploadAsync(id);
        _h.Ai.Rules = [new("R1", "GST", RuleCategory.Eligibility, true, 1, """{"field":"gstin","op":"exists"}""", null, null, null)];
        await Job().RunAsync(id, CancellationToken.None);
        var publish = new PublishTenderCommandHandler(_h.Tenders, _h.Rules, _h.Cache, _h.Audit, _h.Db);

        var act = async () => await publish.Handle(new PublishTenderCommand(id), CancellationToken.None);

        await act.Should().ThrowAsync<BidGuard.Domain.Common.DomainException>();
    }

    [Fact]
    public void Rule_validator_rejects_malformed_expression()
    {
        var validator = new CreateRuleCommandValidator();
        using var doc = JsonDocument.Parse("""{"field":"a","op":"frobnicate"}""");

        var result = validator.TestValidate(new CreateRuleCommand(Guid.NewGuid(), "R1", "t", RuleCategory.Eligibility, true, 1m,
            doc.RootElement.Clone(), null, null, null));

        result.ShouldHaveValidationErrorFor(x => x.Expression);
    }

    [Fact]
    public void Upload_validator_rejects_non_pdf_and_oversized_files()
    {
        var validator = new UploadTenderDocumentCommandValidator();

        validator.TestValidate(new UploadTenderDocumentCommand(Guid.NewGuid(), "a.exe", "application/octet-stream", Stream.Null, 10))
            .ShouldHaveValidationErrorFor(x => x.FileName);
        validator.TestValidate(new UploadTenderDocumentCommand(Guid.NewGuid(), "a.pdf", "application/pdf", Stream.Null, UploadLimits.MaxBytes + 1))
            .ShouldHaveValidationErrorFor(x => x.Length);
    }

    [Fact]
    public async Task Bidders_only_see_published_tenders()
    {
        await CreateTenderAsync();
        _h.CurrentUser.Role = UserRole.Bidder;

        var result = await new GetTendersQueryHandler(_h.Tenders, _h.CurrentUser)
            .Handle(new GetTendersQuery(null, null), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }

    public void Dispose() => _h.Dispose();
}
