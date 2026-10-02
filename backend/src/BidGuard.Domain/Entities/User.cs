using BidGuard.Domain.Common;
using BidGuard.Domain.Enums;

namespace BidGuard.Domain.Entities;

public class User : BaseEntity
{
    private User() { }

    public User(string email, string passwordHash, string fullName, UserRole role, Guid? organizationId = null)
    {
        if (role == UserRole.Bidder && organizationId is null)
            throw new DomainException("A bidder must belong to an organization.");

        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        FullName = fullName.Trim();
        Role = role;
        OrganizationId = organizationId;
        IsActive = true;
    }

    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public Organization? Organization { get; private set; }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }
}
