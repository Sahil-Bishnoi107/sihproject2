using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Audit;
using BidGuard.Application.Services.Bids;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Commands.Bids.RecordDecision;

public sealed class RecordDecisionCommandHandler(
    IBidRepository bids,
    IUserRepository users,
    INotificationRepository notifications,
    ICurrentUserService currentUser,
    IAuditService audit,
    IUnitOfWork unitOfWork) : ICommandHandler<RecordDecisionCommand, BidDto>
{
    public async ValueTask<BidDto> Handle(RecordDecisionCommand command, CancellationToken ct)
    {
        var bid = await bids.GetByIdAsync(command.BidId, ct) ?? throw new NotFoundException(nameof(Bid), command.BidId);
        BidAccess.EnsureCanRead(bid, currentUser);

        var suggested = bid.SuggestedDecision;
        bid.Decide(command.Decision, currentUser.UserId, command.Remarks);

        foreach (var user in await users.GetByOrganizationAsync(bid.OrganizationId, ct))
            await notifications.AddAsync(new Notification(user.Id, NotificationType.BidDecided,
                $"Decision on your bid for {bid.Tender.GemBidNumber}: {command.Decision}.", bid.Id.ToString()), ct);

        await audit.LogAsync("BidDecided", nameof(Bid), bid.Id.ToString(), new
        {
            Decision = command.Decision.ToString(),
            Suggested = suggested?.ToString(),
            AgreesWithEngine = suggested == command.Decision,
            command.Remarks,
        }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return BidDto.From(bid);
    }
}
