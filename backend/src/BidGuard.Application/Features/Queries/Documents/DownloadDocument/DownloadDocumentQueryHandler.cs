using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Features.Queries.Tenders;
using BidGuard.Application.Services.Bids;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Queries.Documents.DownloadDocument;

public sealed class DownloadDocumentQueryHandler(
    IDocumentRepository documents,
    ITenderRepository tenders,
    IBidRepository bids,
    IFileStorageService storage,
    ICurrentUserService currentUser) : IQueryHandler<DownloadDocumentQuery, DocumentContent>
{
    public async ValueTask<DocumentContent> Handle(DownloadDocumentQuery query, CancellationToken ct)
    {
        var document = await documents.GetByIdAsync(query.DocumentId, ct) ?? throw new NotFoundException(nameof(Document), query.DocumentId);

        // A document is visible exactly when its owner is.
        switch (document.OwnerType)
        {
            case DocumentOwnerType.Tender:
                var tender = await tenders.GetByIdAsync(document.OwnerId, ct) ?? throw new NotFoundException(nameof(Document), document.Id);
                TenderAccess.EnsureVisible(tender, currentUser);
                break;
            case DocumentOwnerType.Bid:
                var bid = await bids.GetByIdAsync(document.OwnerId, ct) ?? throw new NotFoundException(nameof(Document), document.Id);
                BidAccess.EnsureCanRead(bid, currentUser);
                break;
            default:
                if (currentUser.Role is not (UserRole.Officer or UserRole.Admin))
                    throw new NotFoundException(nameof(Document), document.Id);
                break;
        }

        return new DocumentContent(await storage.DownloadAsync(document.StorageKey, ct), document.FileName, document.ContentType);
    }
}
