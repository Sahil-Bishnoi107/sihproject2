using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;

namespace BidGuard.Application.Services.Bids;

/// <summary>
/// Bid visibility rules. A bid the caller may not see is reported as not found, so its existence is not leaked.
/// </summary>
public static class BidAccess
{
    /// <summary>Bidders see their own organization's bids; officers and admins see official bids, never simulations.</summary>
    public static void EnsureCanRead(Bid bid, ICurrentUserService user)
    {
        var allowed = user.Role switch
        {
            UserRole.Bidder => user.OrganizationId == bid.OrganizationId,
            UserRole.Officer or UserRole.Admin => !bid.IsSimulation,
            _ => false,
        };
        if (!allowed) throw new NotFoundException(nameof(Bid), bid.Id);
    }

    /// <summary>Only the owning bidder organization may change a bid.</summary>
    public static void EnsureOwner(Bid bid, ICurrentUserService user)
    {
        if (user.Role != UserRole.Bidder || user.OrganizationId != bid.OrganizationId)
            throw new NotFoundException(nameof(Bid), bid.Id);
    }
}
