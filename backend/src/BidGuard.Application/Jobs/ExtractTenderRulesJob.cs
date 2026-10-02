using BidGuard.Application.Common.Interfaces;
using BidGuard.Application.Services.Audit;
using BidGuard.Application.Services.Rules;
using BidGuard.Domain.Entities;
using BidGuard.Domain.Enums;
using BidGuard.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BidGuard.Application.Jobs;

/// <summary>Compliance-as-Code step 1: the AI sidecar drafts machine-readable rules from the tender PDF.</summary>
public sealed class ExtractTenderRulesJob(
    ITenderRepository tenders,
    IDocumentRepository documents,
    IComplianceRuleRepository rules,
    INotificationRepository notifications,
    IFileStorageService storage,
    IDocumentIntelligenceService ai,
    IAuditService audit,
    IUnitOfWork unitOfWork,
    ILogger<ExtractTenderRulesJob> logger) : IHandlesPermanentFailure
{
    public async Task RunAsync(Guid tenderId, CancellationToken ct)
    {
        var tender = await tenders.GetByIdAsync(tenderId, ct);
        if (tender is null || tender.Status != TenderStatus.Extracting || tender.TenderDocumentId is null)
        {
            logger.LogWarning("Skipping rule extraction for tender {TenderId}: not in Extracting state", tenderId);
            return;
        }

        var document = await documents.GetByIdAsync(tender.TenderDocumentId.Value, ct)
                       ?? throw new InvalidOperationException($"Tender document {tender.TenderDocumentId} missing.");

        await using var stream = await storage.DownloadAsync(document.StorageKey, ct);
        var extracted = await ai.ExtractRulesAsync(stream, document.FileName, ct);

        // Re-extraction replaces earlier AI drafts but keeps anything an officer authored or approved.
        var existing = await rules.GetByTenderAsync(tender.Id, ct);
        rules.RemoveRange(existing.Where(r => r.Status == RuleStatus.AiDraft));
        var usedCodes = existing.Where(r => r.Status != RuleStatus.AiDraft).Select(r => r.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var drafts = new List<ComplianceRule>();
        foreach (var dto in extracted)
        {
            if (RuleExpression.Validate(dto.ExpressionJson).Count > 0)
            {
                logger.LogWarning("Dropping AI rule {Code} for tender {TenderId}: invalid expression", dto.Code, tender.Id);
                continue;
            }
            var code = UniqueCode(dto.Code, usedCodes);
            drafts.Add(ComplianceRule.CreateDraft(tender.Id, code, dto.Title, dto.Category, dto.IsMandatory, dto.Weight,
                dto.ExpressionJson, dto.SourceClause, dto.SourcePage, dto.ExemptionKind));
        }

        await rules.AddRangeAsync(drafts, ct);
        tender.MarkRulesExtracted();
        document.MarkExtracted(ocrUsed: false);

        await notifications.AddAsync(new Notification(tender.CreatedById, NotificationType.RulesReady,
            $"{drafts.Count} draft rule(s) extracted for tender {tender.GemBidNumber}. Review and approve before publishing.",
            tender.Id.ToString()), ct);
        await audit.LogAsync("TenderRulesExtracted", nameof(Tender), tender.Id.ToString(), new { RuleCount = drafts.Count }, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task OnPermanentFailureAsync(IReadOnlyList<object?> jobArguments, string reason, CancellationToken ct)
    {
        if (jobArguments.Count == 0 || jobArguments[0] is not Guid tenderId) return;
        var tender = await tenders.GetByIdAsync(tenderId, ct);
        if (tender is null) return;

        tender.MarkExtractionFailed($"Rule extraction failed: {reason}");
        await notifications.AddAsync(new Notification(tender.CreatedById, NotificationType.ProcessingFailed,
            $"Rule extraction failed for tender {tender.GemBidNumber}. You can retry or add rules manually.", tender.Id.ToString()), ct);
        await audit.LogAsync("TenderRuleExtractionFailed", nameof(Tender), tender.Id.ToString(), new { reason }, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private static string UniqueCode(string code, HashSet<string> used)
    {
        var baseCode = string.IsNullOrWhiteSpace(code) ? "RULE" : code.Trim().ToUpperInvariant();
        baseCode = baseCode.Length > 28 ? baseCode[..28] : baseCode;
        var candidate = baseCode;
        for (var i = 2; !used.Add(candidate); i++)
            candidate = $"{baseCode}-{i}";
        return candidate;
    }
}
