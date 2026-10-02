using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;

namespace BidGuard.Application.Features.Queries.Tenders;

internal static class TenderAccess
{
    /// <summary>Bidders may only see published or closed tenders; unpublished ones look like they do not exist.</summary>
    public static void EnsureVisible(Tender tender, ICurrentUserService currentUser)
    {
        if (currentUser.Role == UserRole.Bidder && tender.Status is not (TenderStatus.Published or TenderStatus.Closed))
            throw new NotFoundException(nameof(Tender), tender.Id);
    }
}
