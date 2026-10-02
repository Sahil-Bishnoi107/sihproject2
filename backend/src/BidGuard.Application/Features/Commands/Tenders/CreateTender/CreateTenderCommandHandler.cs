using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Audit;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Commands.Tenders.CreateTender;

public sealed class CreateTenderCommandHandler(
    ITenderRepository tenders,
    ICurrentUserService currentUser,
    IAuditService audit,
    IUnitOfWork unitOfWork) : ICommandHandler<CreateTenderCommand, TenderDto>
{
    public async ValueTask<TenderDto> Handle(CreateTenderCommand command, CancellationToken ct)
    {
        if (await tenders.GemBidNumberExistsAsync(command.GemBidNumber, ct))
            throw new ConflictException($"Tender {command.GemBidNumber} already exists.");

        var tender = new Tender(command.GemBidNumber, command.Title, command.Category, command.EstimatedValue,
            command.EmdAmount, command.BidEndDate.ToUniversalTime(),
            currentUser.UserId ?? throw new UnauthorizedException("Not authenticated."));

        await tenders.AddAsync(tender, ct);
        await audit.LogAsync("TenderCreated", nameof(Tender), tender.Id.ToString(), new { tender.GemBidNumber, tender.Title }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return TenderDto.From(tender);
    }
}
