using BidGuard.Domain.Common;
using BidGuard.Domain.Enums;

namespace BidGuard.Domain.Entities;

/// <summary>
/// A bidder's submission against a tender. When <see cref="IsSimulation"/> is true it is a private
/// pre-bid dry run that only the bidder can see.
/// </summary>
public class Bid : BaseEntity
{
    private Bid() { }

    public Bid(Guid tenderId, Guid organizationId, bool isSimulation)
    {
        TenderId = tenderId;
        OrganizationId = organizationId;
        IsSimulation = isSimulation;
        Status = BidStatus.Uploaded;
    }

    public Guid TenderId { get; private set; }
    public Tender Tender { get; private set; } = default!;
    public Guid OrganizationId { get; private set; }
    public Organization Organization { get; private set; } = default!;
    public bool IsSimulation { get; private set; }
    public BidStatus Status { get; private set; }
    public DateTime? SubmittedAt { get; private set; }
    public decimal? OverallScore { get; private set; }
    public BidDecision? SuggestedDecision { get; private set; }
    public BidDecision? Decision { get; private set; }
    public string? DecisionRemarks { get; private set; }
    public Guid? DecidedById { get; private set; }
    public DateTime? DecidedAt { get; private set; }
    public string? FailureReason { get; private set; }

    public bool CanAcceptDocuments => Status is BidStatus.Uploaded or BidStatus.Failed;

    public void StartExtraction()
    {
        if (Status is not (BidStatus.Uploaded or BidStatus.Failed))
            throw new DomainException($"Bid cannot be submitted while {Status}.");
        Status = BidStatus.Extracting;
        SubmittedAt = DateTime.UtcNow;
        FailureReason = null;
        Touch();
    }

    public void StartVerification() => Transition(BidStatus.Extracting, BidStatus.Verifying);

    public void StartEvaluation() => Transition(BidStatus.Verifying, BidStatus.Evaluating);

    public void MarkUnderReview(decimal score, BidDecision suggested)
    {
        Transition(BidStatus.Evaluating, BidStatus.UnderReview);
        OverallScore = score;
        SuggestedDecision = suggested;
    }

    /// <summary>Recompute score and suggestion after an officer overrides a rule outcome.</summary>
    public void Rescore(decimal score, BidDecision suggested)
    {
        if (Status != BidStatus.UnderReview)
            throw new DomainException("Only a bid under review can be rescored.");
        OverallScore = score;
        SuggestedDecision = suggested;
        Touch();
    }

    public void Decide(BidDecision decision, Guid? decidedById = null, string? remarks = null)
    {
        if (IsSimulation)
            throw new DomainException("A simulation cannot receive an official decision.");
        Transition(BidStatus.UnderReview, BidStatus.Decided);
        Decision = decision;
        DecidedById = decidedById;
        DecisionRemarks = remarks;
        DecidedAt = DateTime.UtcNow;
    }

    public void Fail(string reason)
    {
        if (Status is not (BidStatus.Extracting or BidStatus.Verifying or BidStatus.Evaluating))
            return;
        Status = BidStatus.Failed;
        FailureReason = reason;
        Touch();
    }

    public void EnsureCanAcceptDocuments()
    {
        if (!CanAcceptDocuments)
            throw new DomainException($"Documents cannot be added while bid is {Status}.");
    }

    private void Transition(BidStatus from, BidStatus to)
    {
        if (Status != from)
            throw new DomainException($"Invalid bid transition {Status} -> {to}.");
        Status = to;
        Touch();
    }
}
