using System.Text.Json;
using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Common.Interfaces.Verification;
using BidGuard.Application.Services.Audit;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BidGuard.Application.Jobs;

/// <summary>
/// Pipeline step 2: cross-match identifiers found in the documents with the bidder's profile, then check them
/// against the government registries (cached 24 h). A document/profile disagreement is recorded as a Mismatch.
/// </summary>
public sealed class VerifyBidCredentialsJob(
    IBidRepository bids,
    IExtractedFieldRepository extractedFields,
    IVerificationResultRepository verifications,
    IEnumerable<IRegistryVerificationService> registries,
    ICacheService cache,
    IAuditService audit,
    IUnitOfWork unitOfWork,
    TimeProvider time,
    ILogger<VerifyBidCredentialsJob> logger) : IHandlesPermanentFailure
{
    private static readonly TimeSpan RegistryCacheTtl = TimeSpan.FromHours(24);

    public async Task RunAsync(Guid bidId, CancellationToken ct)
    {
        var bid = await bids.GetByIdAsync(bidId, ct);
        if (bid is null || bid.Status != BidStatus.Verifying)
        {
            logger.LogWarning("Skipping verification for bid {BidId}: not in Verifying state", bidId);
            return;
        }

        var org = bid.Organization;
        var fields = await extractedFields.GetByBidAsync(bid.Id, ct);
        verifications.RemoveRange(await verifications.GetByBidAsync(bid.Id, ct));

        var now = time.GetUtcNow().UtcDateTime;
        var results = new List<VerificationResult>();
        foreach (var registry in registries)
        {
            var (fieldKey, profileValue) = registry.Source switch
            {
                VerificationSource.GST => ("gstin", org.Gstin),
                VerificationSource.MCA => ("cin", org.Cin),
                VerificationSource.Udyam => ("udyam", org.UdyamNumber),
                VerificationSource.EPFO => ("epfo", org.EpfoCode),
                _ => (string.Empty, null),
            };
            var documentValue = fields.Where(f => f.Key == fieldKey).OrderByDescending(f => f.Confidence).FirstOrDefault()?.Value;
            var identifier = Normalize(documentValue ?? profileValue);
            if (identifier is null) continue;

            if (documentValue is not null && profileValue is not null && Normalize(documentValue) != Normalize(profileValue))
            {
                results.Add(new VerificationResult(bid.Id, registry.Source, identifier, VerificationStatus.Mismatch, null,
                    JsonSerializer.Serialize(new { reason = "Identifier in submitted documents differs from the registered profile",
                        document = Normalize(documentValue), profile = Normalize(profileValue) }), now));
                continue;
            }

            var lookup = await cache.GetOrSetAsync(CacheKeys.Verification(registry.Source.ToString(), identifier),
                () => registry.VerifyAsync(identifier, ct), RegistryCacheTtl, ct);
            results.Add(new VerificationResult(bid.Id, registry.Source, identifier, lookup.Status, lookup.RegisteredName, lookup.DetailsJson, now));
        }

        await verifications.AddRangeAsync(results, ct);
        bid.StartEvaluation();
        await audit.LogAsync("BidCredentialsVerified", nameof(Bid), bid.Id.ToString(),
            new { Results = results.Select(r => new { Source = r.Source.ToString(), r.Identifier, Status = r.Status.ToString() }) }, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public Task OnPermanentFailureAsync(IReadOnlyList<object?> jobArguments, string reason, CancellationToken ct) =>
        BidJobFailure.MarkFailedAsync(jobArguments, "registry verification", reason, bids, audit, unitOfWork, null, null, ct);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Replace(" ", "").Trim().ToUpperInvariant();
}
