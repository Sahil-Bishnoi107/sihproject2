using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Audit;
using BidGuard.Application.Services.Bids;
using BidGuard.Domain.Common;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Commands.Bids.SubmitBid;

public sealed class SubmitBidCommandHandler(
    IBidRepository bids,
    IDocumentRepository documents,
    BidPipeline pipeline,
    ICurrentUserService currentUser,
    IAuditService audit,
    IUnitOfWork unitOfWork) : ICommandHandler<SubmitBidCommand, BidDto>
{
    public async ValueTask<BidDto> Handle(SubmitBidCommand command, CancellationToken ct)
    {
        var bid = await bids.GetByIdAsync(command.BidId, ct) ?? throw new NotFoundException(nameof(Bid), command.BidId);
        BidAccess.EnsureOwner(bid, currentUser);

        var files = await documents.GetByOwnerAsync(DocumentOwnerType.Bid, bid.Id, ct);
        if (files.Count == 0)
            throw new DomainException("Upload at least one document before submitting.");

        bid.StartExtraction();
        await audit.LogAsync(bid.IsSimulation ? "SimulationSubmitted" : "BidSubmitted", nameof(Bid), bid.Id.ToString(),
            new { Documents = files.Select(f => new { f.FileName, f.Sha256 }) }, ct);
        await unitOfWork.SaveChangesAsync(ct);

        pipeline.Start(bid.Id);
        return BidDto.From(bid);
    }
}
