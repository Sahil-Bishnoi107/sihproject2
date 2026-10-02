using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Audit;
using BidGuard.Domain.Common;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Commands.Bids.CreateBid;

public sealed class CreateBidCommandHandler(
    ITenderRepository tenders,
    IBidRepository bids,
    IOrganizationRepository organizations,
    ICurrentUserService currentUser,
    IAuditService audit,
    IUnitOfWork unitOfWork,
    TimeProvider time) : ICommandHandler<CreateBidCommand, BidDto>
{
    public async ValueTask<BidDto> Handle(CreateBidCommand command, CancellationToken ct)
    {
        if (currentUser.Role != UserRole.Bidder || currentUser.OrganizationId is not { } orgId)
            throw new ForbiddenException("Only bidder accounts can create bids.");

        var tender = await tenders.GetByIdAsync(command.TenderId, ct);
        if (tender is null || tender.Status != TenderStatus.Published)
            throw new NotFoundException(nameof(Tender), command.TenderId);

        if (!command.IsSimulation)
        {
            if (tender.BidEndDate <= time.GetUtcNow().UtcDateTime)
                throw new DomainException("Bidding for this tender has closed.");
            if (await bids.OfficialBidExistsAsync(tender.Id, orgId, ct))
                throw new ConflictException("Your organization already has a bid on this tender.");
        }

        var organization = await organizations.GetByIdAsync(orgId, ct) ?? throw new NotFoundException(nameof(Organization), orgId);
        var bid = new Bid(tender.Id, organization.Id, command.IsSimulation);

        await bids.AddAsync(bid, ct);
        await audit.LogAsync(command.IsSimulation ? "SimulationCreated" : "BidCreated", nameof(Bid), bid.Id.ToString(),
            new { TenderId = tender.Id, OrganizationId = organization.Id }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return BidDto.From(bid);
    }
}
