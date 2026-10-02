using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Commands.Notifications.MarkNotificationRead;

public sealed class MarkNotificationReadCommandHandler(
    INotificationRepository notifications,
    ICurrentUserService currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<MarkNotificationReadCommand>
{
    public async ValueTask<Unit> Handle(MarkNotificationReadCommand command, CancellationToken ct)
    {
        var notification = await notifications.GetByIdAsync(command.NotificationId, ct);
        if (notification is null || notification.UserId != currentUser.UserId)
            throw new NotFoundException(nameof(Notification), command.NotificationId);

        notification.MarkRead();
        await unitOfWork.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
