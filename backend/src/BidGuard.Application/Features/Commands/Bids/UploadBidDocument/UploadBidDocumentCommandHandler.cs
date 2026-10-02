using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Audit;
using BidGuard.Application.Services.Bids;
using BidGuard.Application.Services.Documents;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Commands.Bids.UploadBidDocument;

public sealed class UploadBidDocumentCommandHandler(
    IBidRepository bids,
    DocumentUploader uploader,
    ICurrentUserService currentUser,
    IAuditService audit,
    IUnitOfWork unitOfWork) : ICommandHandler<UploadBidDocumentCommand, DocumentDto>
{
    public async ValueTask<DocumentDto> Handle(UploadBidDocumentCommand command, CancellationToken ct)
    {
        var bid = await bids.GetByIdAsync(command.BidId, ct) ?? throw new NotFoundException(nameof(Bid), command.BidId);
        BidAccess.EnsureOwner(bid, currentUser);
        bid.EnsureCanAcceptDocuments();

        var document = await uploader.UploadAsync(DocumentOwnerType.Bid, bid.Id, command.FileName, command.ContentType,
            command.Content, command.DocType, currentUser.UserId!.Value, ct);

        await audit.LogAsync("BidDocumentUploaded", nameof(Bid), bid.Id.ToString(),
            new { document.FileName, document.Sha256, document.DocType }, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return DocumentDto.From(document);
    }
}
