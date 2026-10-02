using BidGuard.Application.Common.Dtos;
using BidGuard.Application.Common.Exceptions;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Jobs;
using BidGuard.Application.Services.Audit;
using BidGuard.Application.Services.Documents;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;
using Mediator;

namespace BidGuard.Application.Features.Commands.Tenders.UploadTenderDocument;

public sealed class UploadTenderDocumentCommandHandler(
    ITenderRepository tenders,
    DocumentUploader uploader,
    IBackgroundJobScheduler jobs,
    ICurrentUserService currentUser,
    IAuditService audit,
    IUnitOfWork unitOfWork) : ICommandHandler<UploadTenderDocumentCommand, TenderDto>
{
    public async ValueTask<TenderDto> Handle(UploadTenderDocumentCommand command, CancellationToken ct)
    {
        var tender = await tenders.GetByIdAsync(command.TenderId, ct) ?? throw new NotFoundException(nameof(Tender), command.TenderId);
        tender.EnsureEditable();

        var document = await uploader.UploadAsync(DocumentOwnerType.Tender, tender.Id, command.FileName, command.ContentType,
            command.Content, "tender", currentUser.UserId ?? Guid.Empty, ct);

        tender.AttachDocument(document.Id);
        tender.StartExtraction();
        await audit.LogAsync("TenderDocumentUploaded", nameof(Tender), tender.Id.ToString(),
            new { document.FileName, document.Sha256 }, ct);
        await unitOfWork.SaveChangesAsync(ct);

        // Enqueue only after commit so the job sees the saved state.
        jobs.Enqueue<ExtractTenderRulesJob>(job => job.RunAsync(tender.Id, CancellationToken.None));
        return TenderDto.From(tender);
    }
}
