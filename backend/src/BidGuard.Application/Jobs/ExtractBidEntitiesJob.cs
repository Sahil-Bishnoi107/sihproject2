using System.Text.Json;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Audit;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BidGuard.Application.Jobs;

/// <summary>Pipeline step 1: the AI sidecar extracts entities (with page + bounding box) from every bid document.</summary>
public sealed class ExtractBidEntitiesJob(
    IBidRepository bids,
    IDocumentRepository documents,
    IExtractedFieldRepository extractedFields,
    INotificationRepository notifications,
    IUserRepository users,
    IFileStorageService storage,
    IDocumentIntelligenceService ai,
    IAuditService audit,
    IUnitOfWork unitOfWork,
    ILogger<ExtractBidEntitiesJob> logger) : IHandlesPermanentFailure
{
    public async Task RunAsync(Guid bidId, CancellationToken ct)
    {
        var bid = await bids.GetByIdAsync(bidId, ct);
        if (bid is null || bid.Status != BidStatus.Extracting)
        {
            logger.LogWarning("Skipping entity extraction for bid {BidId}: not in Extracting state", bidId);
            return;
        }

        // A resubmitted bid starts from a clean slate.
        extractedFields.RemoveRange(await extractedFields.GetByBidAsync(bid.Id, ct));

        var files = await documents.GetByOwnerAsync(DocumentOwnerType.Bid, bid.Id, ct);
        var total = 0;
        foreach (var document in files)
        {
            await using var stream = await storage.DownloadAsync(document.StorageKey, ct);
            var result = await ai.ExtractEntitiesAsync(stream, document.FileName, document.DocType, ct);

            await extractedFields.AddRangeAsync(result.Fields.Select(f => new ExtractedField(bid.Id, document.Id, f.Key, f.Value,
                f.Confidence, f.Page, f.BoundingBox is null ? null : JsonSerializer.Serialize(f.BoundingBox))), ct);
            document.MarkExtracted(result.OcrUsed);
            total += result.Fields.Count;
        }

        bid.StartVerification();
        await audit.LogAsync("BidEntitiesExtracted", nameof(Bid), bid.Id.ToString(), new { Documents = files.Count, Fields = total }, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public Task OnPermanentFailureAsync(IReadOnlyList<object?> jobArguments, string reason, CancellationToken ct) =>
        BidJobFailure.MarkFailedAsync(jobArguments, "document extraction", reason, bids, audit, unitOfWork, notifications, users, ct);
}
