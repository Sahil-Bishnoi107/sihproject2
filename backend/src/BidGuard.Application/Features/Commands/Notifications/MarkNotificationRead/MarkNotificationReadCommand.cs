using Mediator;

namespace BidGuard.Application.Features.Commands.Notifications.MarkNotificationRead;

public sealed record MarkNotificationReadCommand(Guid NotificationId) : ICommand;
