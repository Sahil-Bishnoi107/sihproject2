using BidGuard.Domain.Enums;

namespace BidGuard.Application.Common.Interfaces;

/// <summary>The authenticated caller, resolved from JWT claims (user_id, email, role).</summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    UserRole? Role { get; }
    Guid? OrganizationId { get; }
    string? IpAddress { get; }
}
