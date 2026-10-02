using BidGuard.Domain.Common;
using BidGuard.Domain.Enums;

namespace BidGuard.Domain.Entities;

/// <summary>A registration or certificate held by an organization (GST, Udyam, ISO...), tracked for expiry.</summary>
public class Credential : BaseEntity
{
    private Credential() { }

    internal Credential(Guid organizationId, CredentialType type, string number, DateOnly? issuedOn, DateOnly? expiresOn, Guid? documentId)
    {
        if (issuedOn.HasValue && expiresOn.HasValue && expiresOn < issuedOn)
            throw new DomainException("Credential expiry cannot precede its issue date.");

        OrganizationId = organizationId;
        Type = type;
        Number = number.Trim().ToUpperInvariant();
        IssuedOn = issuedOn;
        ExpiresOn = expiresOn;
        DocumentId = documentId;
    }

    public Guid OrganizationId { get; private set; }
    public CredentialType Type { get; private set; }
    public string Number { get; private set; } = default!;
    public DateOnly? IssuedOn { get; private set; }
    public DateOnly? ExpiresOn { get; private set; }
    public Guid? DocumentId { get; private set; }
    public DateTime? LastExpiryAlertAt { get; private set; }

    public void MarkExpiryAlerted(DateTime now) => LastExpiryAlertAt = now;
}
