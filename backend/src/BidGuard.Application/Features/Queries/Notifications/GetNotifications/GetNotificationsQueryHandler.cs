using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Queries.Notifications.GetNotifications;

public sealed class GetNotificationsQueryHandler(INotificationRepository notifications, ICurrentUserService currentUser)
    : IQueryHandler<GetNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    public async ValueTask<IReadOnlyList<NotificationDto>> Handle(GetNotificationsQuery query, CancellationToken ct)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedException("Not authenticated.");
        return (await notifications.GetForUserAsync(userId, query.UnreadOnly, ct)).Select(NotificationDto.From).ToList();
    }
}
