using BidGuard.Application.Common.Interfaces;
using BidGuard.Domain.Enums;
using BidGuard.Infrastructure.Identity;

namespace BidGuard.API.Services;

/// <summary>Reads the caller's identity from the validated JWT (claims user_id, email, role, org_id).</summary>
public sealed class CurrentUserService(IHttpContextAccessor accessor) : ICurrentUserService
{
    private HttpContext? Context => accessor.HttpContext;

    public Guid? UserId => Guid.TryParse(Claim(BidGuardClaims.UserId), out var id) ? id : null;
    public string? Email => Claim(BidGuardClaims.Email);
    public UserRole? Role => Enum.TryParse<UserRole>(Claim(BidGuardClaims.Role), out var role) ? role : null;
    public Guid? OrganizationId => Guid.TryParse(Claim(BidGuardClaims.OrganizationId), out var id) ? id : null;
    public string? IpAddress => Context?.Connection.RemoteIpAddress?.ToString();

    private string? Claim(string type) => Context?.User.FindFirst(type)?.Value;
}
