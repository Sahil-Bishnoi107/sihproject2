using BidGuard.Domain.Common;
using BidGuard.Domain.Enums;

namespace BidGuard.Domain.Entities;

/// <summary>A bidding enterprise (seller on GeM).</summary>
public class Organization : BaseEntity
{
    private readonly List<Credential> _credentials = [];

    private Organization() { }

    public Organization(string legalName, string pan, OrganizationCategory category)
    {
        LegalName = legalName.Trim();
        Pan = pan.Trim().ToUpperInvariant();
        Category = category;
    }

    public string LegalName { get; private set; } = default!;
    public string Pan { get; private set; } = default!;
    public string? Gstin { get; private set; }
    public string? UdyamNumber { get; private set; }
    public string? Cin { get; private set; }
    public string? EpfoCode { get; private set; }
    public OrganizationCategory Category { get; private set; }
    public bool IsStartup { get; private set; }
    public string? DpiitNumber { get; private set; }

    public IReadOnlyCollection<Credential> Credentials => _credentials.AsReadOnly();

    public bool IsRecognisedStartup => IsStartup && !string.IsNullOrWhiteSpace(DpiitNumber);

    public void UpdateProfile(string legalName, OrganizationCategory category, string? gstin, string? udyamNumber,
        string? cin, string? epfoCode, bool isStartup, string? dpiitNumber)
    {
        LegalName = legalName.Trim();
        Category = category;
        Gstin = Normalize(gstin);
        UdyamNumber = Normalize(udyamNumber);
        Cin = Normalize(cin);
        EpfoCode = Normalize(epfoCode);
        IsStartup = isStartup;
        DpiitNumber = Normalize(dpiitNumber);
        Touch();
    }

    public Credential AddCredential(CredentialType type, string number, DateOnly? issuedOn, DateOnly? expiresOn, Guid? documentId = null)
    {
        var credential = new Credential(Id, type, number, issuedOn, expiresOn, documentId);
        _credentials.Add(credential);
        return credential;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}
