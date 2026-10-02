using BidGuard.Domain.Common;
using BidGuard.Domain.Enums;

namespace BidGuard.Domain.Entities;

public class Tender : BaseEntity
{
    private readonly List<ComplianceRule> _rules = [];

    private Tender() { }

    public Tender(string gemBidNumber, string title, string category, decimal estimatedValue, decimal emdAmount,
        DateTime bidEndDate, Guid createdById)
    {
        GemBidNumber = gemBidNumber.Trim().ToUpperInvariant();
        Title = title.Trim();
        Category = category.Trim();
        EstimatedValue = estimatedValue;
        EmdAmount = emdAmount;
        BidEndDate = bidEndDate;
        CreatedById = createdById;
        Status = TenderStatus.Draft;
    }

    public string GemBidNumber { get; private set; } = default!;
    public string Title { get; private set; } = default!;
    public string Category { get; private set; } = default!;
    public decimal EstimatedValue { get; private set; }
    public decimal EmdAmount { get; private set; }
    public DateTime BidEndDate { get; private set; }
    public TenderStatus Status { get; private set; }
    public Guid CreatedById { get; private set; }
    public Guid? TenderDocumentId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime? PublishedAt { get; private set; }

    public IReadOnlyCollection<ComplianceRule> Rules => _rules.AsReadOnly();

    public bool RulesEditable => Status is TenderStatus.Draft or TenderStatus.RulesExtracted;

    public void AttachDocument(Guid documentId)
    {
        EnsureEditable();
        TenderDocumentId = documentId;
        Touch();
    }

    public void StartExtraction()
    {
        if (Status is not (TenderStatus.Draft or TenderStatus.RulesExtracted))
            throw new DomainException($"Cannot extract rules while tender is {Status}.");
        Status = TenderStatus.Extracting;
        FailureReason = null;
        Touch();
    }

    public void MarkRulesExtracted()
    {
        if (Status != TenderStatus.Extracting)
            throw new DomainException($"Tender is not extracting (status {Status}).");
        Status = TenderStatus.RulesExtracted;
        Touch();
    }

    /// <summary>Extraction failed: return to Draft so the officer can retry or author rules manually.</summary>
    public void MarkExtractionFailed(string reason)
    {
        if (Status != TenderStatus.Extracting) return;
        Status = TenderStatus.Draft;
        FailureReason = reason;
        Touch();
    }

    public void Publish(IEnumerable<ComplianceRule> rules)
    {
        if (!RulesEditable)
            throw new DomainException($"Cannot publish a tender in status {Status}.");
        var list = rules.ToList();
        if (list.Count == 0)
            throw new DomainException("A tender needs at least one compliance rule before publishing.");
        if (list.Any(r => r.Status != RuleStatus.Approved))
            throw new DomainException("All compliance rules must be approved before publishing.");
        Status = TenderStatus.Published;
        PublishedAt = DateTime.UtcNow;
        Touch();
    }

    public void Close()
    {
        if (Status != TenderStatus.Published)
            throw new DomainException("Only a published tender can be closed.");
        Status = TenderStatus.Closed;
        Touch();
    }

    public void EnsureEditable()
    {
        if (!RulesEditable)
            throw new DomainException($"Tender cannot be modified in status {Status}.");
    }
}
