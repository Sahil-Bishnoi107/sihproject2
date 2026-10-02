using BidGuard.Domain.Common;
using BidGuard.Domain.Enums;

namespace BidGuard.Domain.Entities;

public class Notification : BaseEntity
{
    private Notification() { }

    public Notification(Guid userId, NotificationType type, string message, string? referenceId = null)
    {
        UserId = userId;
        Type = type;
        Message = message;
        ReferenceId = referenceId;
    }

    public Guid UserId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Message { get; private set; } = default!;
    public string? ReferenceId { get; private set; }
    public bool IsRead { get; private set; }

    public void MarkRead()
    {
        IsRead = true;
        Touch();
    }
}
