using BidGuard.Application.Common.Dtos;
using Mediator;

namespace BidGuard.Application.Features.Queries.Notifications.GetNotifications;

public sealed record GetNotificationsQuery(bool UnreadOnly = false) : IQuery<IReadOnlyList<NotificationDto>>;
