using BidGuard.Domain.Common;
using BidGuard.Domain.Enums;

namespace BidGuard.Domain.Entities;

/// <summary>Outcome of checking a credential against a government registry.</summary>
public class VerificationResult : BaseEntity
{
    private VerificationResult() { }

    public VerificationResult(Guid bidId, VerificationSource source, string identifier, VerificationStatus status,
        string? registeredName, string? details, DateTime checkedAt)
    {
        BidId = bidId;
        Source = source;
        Identifier = identifier;
        Status = status;
        RegisteredName = registeredName;
        Details = details;
        CheckedAt = checkedAt;
    }

    public Guid BidId { get; private set; }
    public VerificationSource Source { get; private set; }
    public string Identifier { get; private set; } = default!;
    public VerificationStatus Status { get; private set; }
    public string? RegisteredName { get; private set; }
    /// <summary>Raw registry payload (JSON).</summary>
    public string? Details { get; private set; }
    public DateTime CheckedAt { get; private set; }
}
