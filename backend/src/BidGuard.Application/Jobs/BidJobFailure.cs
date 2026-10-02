using BidGuard.Application.Services.Audit;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;

namespace BidGuard.Application.Jobs;

/// <summary>Shared clean-up for bid pipeline jobs once retries are exhausted: the bid moves to Failed with a reason.</summary>
internal static class BidJobFailure
{
    public static async Task MarkFailedAsync(IReadOnlyList<object?> args, string stage, string reason, IBidRepository bids,
        IAuditService audit, IUnitOfWork unitOfWork, INotificationRepository? notifications, IUserRepository? users, CancellationToken ct)
    {
        if (args.Count == 0 || args[0] is not Guid bidId) return;
        var bid = await bids.GetByIdAsync(bidId, ct);
        if (bid is null) return;

        bid.Fail($"{stage} failed: {reason}");
        await audit.LogAsync("BidProcessingFailed", nameof(Bid), bid.Id.ToString(), new { stage, reason }, ct);

        if (notifications is not null && users is not null)
            foreach (var user in await users.GetByOrganizationAsync(bid.OrganizationId, ct))
                await notifications.AddAsync(new Notification(user.Id, NotificationType.ProcessingFailed,
                    $"Processing of your {(bid.IsSimulation ? "simulation" : "bid")} failed during {stage}. You can resubmit it.",
                    bid.Id.ToString()), ct);

        await unitOfWork.SaveChangesAsync(ct);
    }
}
